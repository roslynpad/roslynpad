using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Extensions.Logging;
using Mono.Cecil;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NuGet.Versioning;
using RoslynPad.Build.ILDecompiler;
using RoslynPad.NuGet;
using RoslynPad.Roslyn;
using RoslynPad.Runtime;
using RoslynPad.Utilities;

namespace RoslynPad.Build
{
    /// <summary>
    /// An <see cref="IExecutionHost"/> implementation that compiles to disk and executes in separated processes.
    /// </summary>
    internal class ExecutionHost : IExecutionHost
    {
        private readonly ExecutionHostParameters _parameters;
        private readonly IRoslynHost _roslynHost;
        private readonly ILogger _logger;
        private readonly IAnalyzerAssemblyLoader _analyzerAssemblyLoader;
        private readonly HashSet<LibraryRef> _libraries;
        private readonly ImmutableArray<string> _imports;
        private readonly SemaphoreSlim _lock;
        private readonly SyntaxTree _scriptInitSyntax;
        private readonly SyntaxTree _moduleInitAttributeSyntax;
        private readonly SyntaxTree _moduleInitSyntax;
        private readonly SyntaxTree _importsSyntax;
        private readonly LibraryRef _runtimeAssemblyLibraryRef;
        private CancellationTokenSource? _executeCts;
        private Task? _restoreTask;
        private CancellationTokenSource? _restoreCts;
        private ExecutionPlatform? _platform;
        private string? _assemblyPath;
        private string _name;
        private bool _running;
        private bool _initializeBuildPathAfterRun;
        private TextWriter? _processInputStream;
        private string? _dotNetExecutable;

        public ExecutionPlatform Platform
        {
            get => _platform ?? throw new InvalidOperationException("No platform selected");
            set
            {
                _platform = value;
                InitializeBuildPath(stop: true);
            }
        }

        public bool HasPlatform => _platform != null;

        public string DotNetExecutable
        {
            get => _dotNetExecutable ?? throw new InvalidOperationException("Missing dotnet");
            set => _dotNetExecutable = value;
        }

        private bool HasDotNetExecutable => _dotNetExecutable != null;

        public string Name
        {
            get => _name;
            set
            {
                if (!string.Equals(_name, value, StringComparison.Ordinal))
                {
                    _name = value;
                    InitializeBuildPath(stop: false);
                    _ = RestoreAsync();
                }
            }
        }

        private readonly JsonSerializer _jsonSerializer;

        private string BuildPath => _parameters.BuildPath;

        public ExecutionHost(ExecutionHostParameters parameters, IRoslynHost roslynHost, ILogger logger)
        {
            _jsonSerializer = new JsonSerializer
            {
                TypeNameHandling = TypeNameHandling.Auto,
                TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple
            };

            _name = "";
            _parameters = parameters;
            _roslynHost = roslynHost;
            _logger = logger;
            _analyzerAssemblyLoader = _roslynHost.GetService<IAnalyzerAssemblyLoader>();
            _libraries = new HashSet<LibraryRef>();
            _imports = parameters.Imports;

            _lock = new SemaphoreSlim(1, 1);

            _scriptInitSyntax = SyntaxFactory.ParseSyntaxTree(BuildCode.ScriptInit, roslynHost.ParseOptions.WithKind(SourceCodeKind.Script));
            var regularParseOptions = roslynHost.ParseOptions.WithKind(SourceCodeKind.Regular);
            _moduleInitAttributeSyntax = SyntaxFactory.ParseSyntaxTree(BuildCode.ModuleInitAttribute, regularParseOptions);
            _moduleInitSyntax = SyntaxFactory.ParseSyntaxTree(BuildCode.ModuleInit, regularParseOptions);
            _importsSyntax = SyntaxFactory.ParseSyntaxTree(GetGlobalUsings(), regularParseOptions);

            MetadataReferences = ImmutableArray<MetadataReference>.Empty;

            _runtimeAssemblyLibraryRef = LibraryRef.Reference(typeof(ObjectExtensions).Assembly.Location);
        }

        public event Action<IList<CompilationErrorResultObject>>? CompilationErrors;
        public event Action<string>? Disassembled;
        public event Action<ResultObject>? Dumped;
        public event Action<ExceptionResultObject>? Error;
        public event Action? ReadInput;
        public event Action? RestoreStarted;
        public event Action<RestoreResult>? RestoreCompleted;
        public event Action<RestoreResultObject>? RestoreMessage;
        public event Action<ProgressResultObject>? ProgressChanged;

        public void Dispose()
        {
        }

        private string GetGlobalUsings() => string.Join(" ", _imports.Select(i => $"global using {i};"));

        private void InitializeBuildPath(bool stop)
        {
            if (!HasPlatform)
            {
                return;
            }

            if (stop)
            {
                StopProcess();
            }
            else if (_running)
            {
                _initializeBuildPathAfterRun = true;
                return;
            }

            CleanupBuildPath();
        }

        private void CleanupBuildPath()
        {
            StopProcess();

            foreach (var file in IOUtilities.EnumerateFilesRecursive(BuildPath))
            {
                IOUtilities.PerformIO(() => File.Delete(file));
            }
        }

        public async Task ExecuteAsync(string code, bool disassemble, OptimizationLevel? optimizationLevel)
        {
            _logger.LogInformation("Start ExecuteAsync");

            await new NoContextYieldAwaitable();

            await RestoreTask.ConfigureAwait(false);

            using var _ = await _lock.DisposableWaitAsync();

            try
            {
                _running = true;

                using var executeCts = new CancellationTokenSource();
                var cancellationToken = executeCts.Token;

                var script = CreateCompiler(code, optimizationLevel);

                _assemblyPath = Path.Combine(BuildPath, "bin", $"{Name}.{ExecutableExtension}");

                var diagnostics = script.CompileAndSaveAssembly(_assemblyPath, cancellationToken);
                var hasErrors = diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error);
                _logger.LogInformation("Assembly saved at {assemblyPath}, has errors = {hasErrors}", _assemblyPath, hasErrors);

                SendDiagnostics(diagnostics);

                if (hasErrors)
                {
                    return;
                }

                if (disassemble)
                {
                    Disassemble();
                }

                _executeCts = executeCts;

                await RunProcess(_assemblyPath, cancellationToken);
            }
            finally
            {
                _executeCts = null;
                _running = false;

                if (_initializeBuildPathAfterRun)
                {
                    _initializeBuildPathAfterRun = false;
                    InitializeBuildPath(stop: false);
                }
            }
        }

        private void Disassemble()
        {
            using var assembly = AssemblyDefinition.ReadAssembly(_assemblyPath);
            var output = new PlainTextOutput();
            var disassembler = new ReflectionDisassembler(output, false, CancellationToken.None);
            disassembler.WriteModuleContents(assembly.MainModule);
            Disassembled?.Invoke(output.ToString());
        }

        private string ExecutableExtension => Platform.IsCore ? "dll" : "exe";

        public ImmutableArray<MetadataReference> MetadataReferences { get; private set; }
        public ImmutableArray<AnalyzerFileReference> Analyzers { get; private set; }

        private Compiler CreateCompiler(string code, OptimizationLevel? optimizationLevel)
        {
            Platform platform = Platform.Architecture == Architecture.X86
                ? Microsoft.CodeAnalysis.Platform.AnyCpu32BitPreferred
                : Microsoft.CodeAnalysis.Platform.AnyCpu;

            var optimization = optimizationLevel ?? OptimizationLevel.Release;

            _logger.LogInformation("Creating script runner, platform = {platform}, " +
                "references = {references}, imports = {imports}, directory = {directory}, " +
                "optimization = {optimization}",
                platform,
                MetadataReferences.Select(t => t.Display),
                _imports,
                _parameters.WorkingDirectory,
                optimizationLevel);

            var parseOptions = ((CSharpParseOptions)_roslynHost.ParseOptions).WithKind(_parameters.SourceCodeKind);

            var syntaxTrees = ImmutableList.Create(ParseCode(code, parseOptions));
            if (_parameters.SourceCodeKind == SourceCodeKind.Script)
            {
                syntaxTrees = syntaxTrees.Add(_scriptInitSyntax);
            }
            else
            {
                if (Platform.FrameworkVersion?.Major < 5)
                {
                    syntaxTrees = syntaxTrees.Add(_moduleInitAttributeSyntax);
                }

                syntaxTrees = syntaxTrees.Add(_moduleInitSyntax).Add(_importsSyntax);
            }

            return new Compiler(syntaxTrees,
                parseOptions,
                OutputKind.ConsoleApplication,
                platform,
                MetadataReferences,
                _imports,
                _parameters.WorkingDirectory,
                optimizationLevel: optimization,
                checkOverflow: _parameters.CheckOverflow,
                allowUnsafe: _parameters.AllowUnsafe);
        }

        private async Task RunProcess(string assemblyPath, CancellationToken cancellationToken)
        {
            using var process = new Process { StartInfo = GetProcessStartInfo(assemblyPath) };
            using var _ = cancellationToken.Register(() =>
            {
                try
                {
                    _processInputStream = null;
                    process.Kill();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error killing process");
                }
            });

            _logger.LogInformation("Starting process {executable}, arguments = {arguments}", process.StartInfo.FileName, process.StartInfo.Arguments);
            if (!process.Start())
            {
                _logger.LogWarning("Process.Start returned false");
                return;
            }

            _processInputStream = new StreamWriter(process.StandardInput.BaseStream, Encoding.UTF8);

            await Task.WhenAll(
                Task.Run(() => ReadObjectProcessStream(process.StandardOutput), cancellationToken),
                Task.Run(() => ReadProcessStream(process.StandardError), cancellationToken));

            ProcessStartInfo GetProcessStartInfo(string assemblyPath) => new()
            {
                FileName = Platform.IsCore ? DotNetExecutable : assemblyPath,
                Arguments = $"\"{assemblyPath}\" --pid {Environment.ProcessId}",
                WorkingDirectory = _parameters.WorkingDirectory,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
            };
        }

        public async Task SendInputAsync(string message)
        {
            var stream = _processInputStream;
            if (stream != null)
            {
                await stream.WriteLineAsync(message).ConfigureAwait(false);
                await stream.FlushAsync().ConfigureAwait(false);
            }
        }

        private async Task ReadProcessStream(StreamReader reader)
        {
            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync().ConfigureAwait(false);
                if (line != null)
                {
                    Dumped?.Invoke(ResultObject.Create(line, DumpQuotas.Default));
                }
            }
        }

        private void ReadObjectProcessStream(StreamReader reader)
        {
            using var jsonReader = new JsonTextReader(reader) { SupportMultipleContent = true };
            while (jsonReader.Read())
            {
                try
                {
                    var result = _jsonSerializer.Deserialize<ResultObject>(jsonReader);

                    switch (result)
                    {
                        case ExceptionResultObject exceptionResult:
                            Error?.Invoke(exceptionResult);
                            break;
                        case InputReadRequest:
                            ReadInput?.Invoke();
                            break;
                        case ProgressResultObject progress:
                            ProgressChanged?.Invoke(progress);
                            break;
                        default:
                            Dumped?.Invoke(result);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error deserializing result");
                    Dumped?.Invoke(ResultObject.Create("Error deserializing result: " + ex.Message, DumpQuotas.Default));
                }
            }
        }

        private static SyntaxTree ParseCode(string code, CSharpParseOptions parseOptions)
        {
            var tree = SyntaxFactory.ParseSyntaxTree(code, parseOptions);
            var root = tree.GetRoot();

            if (root is not CompilationUnitSyntax compilationUnit)
            {
                return tree;
            }

            // references directives are resolved by msbuild, so removing from compilation
            var nodesToRemove = compilationUnit.GetReferenceDirectives().AsEnumerable<SyntaxNode>();
            if (parseOptions.Kind == SourceCodeKind.Regular)
            {
                // load directives' files are added to the compilation separately
                nodesToRemove = nodesToRemove.Concat(compilationUnit.GetLoadDirectives());
            }

            compilationUnit = compilationUnit.RemoveNodes(nodesToRemove, SyntaxRemoveOptions.KeepExteriorTrivia) ?? compilationUnit;
            var members = compilationUnit.Members;

            // add .Dump() to the last bare expression
            var lastMissingSemicolon = compilationUnit.Members.OfType<GlobalStatementSyntax>()
                .LastOrDefault(m => m.Statement is ExpressionStatementSyntax expr && expr.SemicolonToken.IsMissing);
            if (lastMissingSemicolon != null)
            {
                var statement = (ExpressionStatementSyntax)lastMissingSemicolon.Statement;
                members = members.Replace(lastMissingSemicolon, BuildCode.GetDumpCall(statement));
            }

            root = compilationUnit.WithMembers(members);

            return tree.WithRootAndOptions(root, parseOptions);
        }

        private void SendDiagnostics(ImmutableArray<Diagnostic> diagnostics)
        {
            if (diagnostics.Length > 0)
            {
                CompilationErrors?.Invoke(diagnostics.Where(d => !_parameters.DisabledDiagnostics.Contains(d.Id))
                    .Select(d => GetCompilationErrorResultObject(d)).ToImmutableArray());
            }
        }

        private static CompilationErrorResultObject GetCompilationErrorResultObject(Diagnostic diagnostic)
        {
            var lineSpan = diagnostic.Location.GetLineSpan();

            var result = CompilationErrorResultObject.Create(diagnostic.Severity.ToString(),
                    diagnostic.Id, diagnostic.GetMessage(),
                    lineSpan.StartLinePosition.Line, lineSpan.StartLinePosition.Character);
            return result;
        }

        public Task TerminateAsync()
        {
            StopProcess();
            return Task.CompletedTask;
        }

        private void StopProcess() => _executeCts?.Cancel();

        public async Task UpdateReferencesAsync(bool alwaysRestore)
        {
            var syntaxRoot = await GetSyntaxRootAsync();
            if (syntaxRoot == null)
            {
                return;
            }

            var libraries = ParseReferences(syntaxRoot).Append(_runtimeAssemblyLibraryRef);
            if (UpdateLibraries(libraries))
            {
                await RestoreAsync().ConfigureAwait(false);
            }

            async ValueTask<SyntaxNode?> GetSyntaxRootAsync()
            {
                if (DocumentId == null)
                {
                    return null;
                }

                var document = _roslynHost.GetDocument(DocumentId);
                return document != null ? await document.GetSyntaxRootAsync().ConfigureAwait(false) : null;
            }

            bool UpdateLibraries(IEnumerable<LibraryRef> libraries)
            {
                lock (_libraries)
                {
                    if (!_libraries.SetEquals(libraries))
                    {
                        _libraries.Clear();
                        _libraries.UnionWith(libraries);
                        return true;
                    }
                    else if (alwaysRestore)
                    {
                        return true;
                    }
                }

                return false;
            }

            static List<LibraryRef> ParseReferences(SyntaxNode syntaxRoot)
            {
                const string LegacyNuGetPrefix = "$NuGet\\";
                const string FxPrefix = "framework:";

                var libraries = new List<LibraryRef>();

                if (syntaxRoot is not CompilationUnitSyntax compilation)
                {
                    return libraries;
                }

                foreach (var directive in compilation.GetReferenceDirectives())
                {
                    var value = directive.File.ValueText;
                    string? id, version;

                    if (HasPrefix(FxPrefix, value))
                    {
                        libraries.Add(LibraryRef.FrameworkReference(
                            value.Substring(FxPrefix.Length, value.Length - FxPrefix.Length)));
                        continue;
                    }

                    if (HasPrefix(ReferenceDirectiveHelper.NuGetPrefix, value))
                    {
                        (id, version) = ReferenceDirectiveHelper.ParseNuGetReference(value);
                    }
                    else if (HasPrefix(LegacyNuGetPrefix, value))
                    {
                        (id, version) = ParseLegacyNuGetReference(value);
                        if (id == null)
                        {
                            continue;
                        }
                    }
                    else
                    {
                        libraries.Add(LibraryRef.Reference(value));

                        continue;
                    }

                    if (!string.IsNullOrEmpty(version) && !VersionRange.TryParse(version, out _))
                    {
                        continue;
                    }

                    libraries.Add(LibraryRef.PackageReference(id, version ?? string.Empty));
                }

                return libraries;

                static bool HasPrefix(string prefix, string value) =>
                    value.Length > prefix.Length &&
                    value.StartsWith(prefix, StringComparison.InvariantCultureIgnoreCase);

                static (string? id, string? version) ParseLegacyNuGetReference(string value)
                {
                    var split = value.Split('\\', StringSplitOptions.RemoveEmptyEntries);
                    return split.Length >= 3 ? (split[1], split[2]) : (null, null);
                }
            }
        }

        private Task RestoreTask => _restoreTask ?? Task.CompletedTask;

        public DocumentId? DocumentId { get; set; }

        private async Task RestoreAsync()
        {
            if (!HasPlatform || string.IsNullOrEmpty(Name))
            {
                return;
            }

            if (_restoreCts != null)
            {
                _restoreCts.Cancel();
                _restoreCts.Dispose();
            }

            RestoreStarted?.Invoke();

            var lockDisposer = await _lock.DisposableWaitAsync();
            var restoreCts = new CancellationTokenSource();
            _restoreTask = DoRestoreAsync(RestoreTask, restoreCts.Token);
            _restoreCts = restoreCts;

            async Task DoRestoreAsync(Task previousTask, CancellationToken cancellationToken)
            {
                if (!HasDotNetExecutable)
                {
                    return;
                }

                try
                {
                    await previousTask.ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error in previous restore task");
                }

                try
                {
                    if (File.Exists(_parameters.NuGetConfigPath))
                    {
                        File.Copy(_parameters.NuGetConfigPath, Path.Combine(BuildPath, "nuget.config"), overwrite: true);
                    }

                    await BuildGlobalJson().ConfigureAwait(false);
                    var csprojPath = await BuildCsproj().ConfigureAwait(false);

                    var errorsPath = Path.Combine(BuildPath, "errors.log");
                    File.Delete(errorsPath);

                    cancellationToken.ThrowIfCancellationRequested();

                    using var result = await ProcessUtil.RunProcess(DotNetExecutable, BuildPath, $"build -nologo -p:nugetinteractive=true -flp:errorsonly;logfile=\"{errorsPath}\" \"{csprojPath}\"", cancellationToken).ConfigureAwait(false);

                    await foreach (var line in result.GetStandardOutputLines())
                    {
                        var trimmed = line.Trim();
                        var deviceCode = GetDeviceCode(trimmed);
                        if (deviceCode != null)
                        {
                            RestoreMessage?.Invoke(new RestoreResultObject(trimmed, "Warning", deviceCode));
                        }
                    }

                    if (result.ExitCode != 0)
                    {
                        var errors = await GetErrorsAsync(errorsPath, result, cancellationToken);
                        RestoreCompleted?.Invoke(RestoreResult.FromErrors(errors));
                        return;
                    }

                    await ReadReferences(cancellationToken).ConfigureAwait(false);

                    RestoreCompleted?.Invoke(RestoreResult.SuccessResult);
                }
                catch (Exception ex) when (!(ex is OperationCanceledException))
                {
                    _logger.LogWarning(ex, "Restore error");
                    RestoreCompleted?.Invoke(RestoreResult.FromErrors(new[] { ex.Message }));
                }
                finally
                {
                    lockDisposer.Dispose();
                }

                static string? GetDeviceCode(string line)
                {
                    if (!line.Contains("devicelogin", StringComparison.OrdinalIgnoreCase))
                    {
                        return null;
                    }

                    var match = Regex.Match(line, @"[A-Z0-9]{9,}");
                    return match.Success ? match.Value : null;
                }

                async Task ReadReferences(CancellationToken cancellationToken)
                {
                    var references = await ReadPathsFile(MSBuildHelper.ReferencesFile, cancellationToken).ConfigureAwait(false);
                    var analyzers = await ReadPathsFile(MSBuildHelper.AnalyzersFile, cancellationToken).ConfigureAwait(false);

                    cancellationToken.ThrowIfCancellationRequested();

                    MetadataReferences = references
                        .Where(r => !string.IsNullOrWhiteSpace(r))
                        .Select(r => _roslynHost.CreateMetadataReference(r))
                        .ToImmutableArray();

                    Analyzers = analyzers
                        .Where(r => !string.IsNullOrWhiteSpace(r))
                        .Select(r => new AnalyzerFileReference(r, _analyzerAssemblyLoader))
                        .ToImmutableArray();
                }
            }

            async Task BuildGlobalJson()
            {
                if (Platform?.IsCore != true)
                {
                    return;
                }

                var global = new JObject(
                    new JProperty("sdk", new JObject(
                        new JProperty("version", Platform.FrameworkVersion!.ToString()))));

                await File.WriteAllTextAsync(Path.Combine(BuildPath, "global.json"), global.ToString());
            }

            async Task<string> BuildCsproj()
            {
                var csproj = MSBuildHelper.CreateCsproj(
                    Platform.IsCore,
                    Platform.TargetFrameworkMoniker,
                    _libraries);
                var csprojPath = Path.Combine(BuildPath, $"{Name}.csproj");

                await Task.Run(() => csproj.Save(csprojPath)).ConfigureAwait(false);
                return csprojPath;
            }

            static async Task<string[]> GetErrorsAsync(string errorsPath, ProcessUtil.ProcessResult result, CancellationToken cancellationToken)
            {
                string[] errors;
                try
                {
                    errors = await File.ReadAllLinesAsync(errorsPath, cancellationToken).ConfigureAwait(false);
                    if (errors.Length == 0)
                    {
                        errors = GetErrorsFromResult(result);
                    }
                    else
                    {
                        for (var i = 0; i < errors.Length; i++)
                        {
                            var match = Regex.Match(errors[i], @"(?<=\: error )[^\]]+");
                            if (match.Success)
                            {
                                errors[i] = match.Value;
                            }
                        }
                    }
                }
                catch (FileNotFoundException)
                {
                    errors = GetErrorsFromResult(result);
                }

                return errors;

                static string[] GetErrorsFromResult(ProcessUtil.ProcessResult result) =>
                    new[] { result.StandardOutput, result.StandardError! };
            }

            async Task<string[]> ReadPathsFile(string file, CancellationToken cancellationToken)
            {
                var path = Path.Combine(BuildPath, file);
                var paths = await File.ReadAllLinesAsync(path, cancellationToken).ConfigureAwait(false);
                return paths;
            }
        }
    }
}
