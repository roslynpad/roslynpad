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
using Microsoft.CodeAnalysis.Scripting;
using Mono.Cecil;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RoslynPad.Build.ILDecompiler;
using RoslynPad.NuGet;
using RoslynPad.Roslyn;
using RoslynPad.Roslyn.Scripting;
using RoslynPad.Runtime;
using RoslynPad.Utilities;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace RoslynPad.Build
{
    /// <summary>
    /// An <see cref="IExecutionHost"/> implementation that compiles scripts to disk as EXEs and executes them in their own process.
    /// </summary>
    internal class ExecutionHost : IExecutionHost
    {
        private static Lazy<string> CurrentPid { get; } = new Lazy<string>(() => Process.GetCurrentProcess().Id.ToString());

        private readonly ExecutionHostParameters _parameters;
        private readonly IRoslynHost _roslynHost;
        private readonly IAnalyzerAssemblyLoader _analyzerAssemblyLoader;
        private readonly SyntaxTree _initHostSyntax;
        private readonly HashSet<LibraryRef> _libraries;
        private ScriptOptions _scriptOptions;
        private CancellationTokenSource? _executeCts;
        private Task? _restoreTask;
        private CancellationTokenSource? _restoreCts;
        private ExecutionPlatform? _platform;
        private string? _assemblyPath;
        private string _name;
        private bool _running;
        private bool _initializeBuildPathAfterRun;
        private TextWriter? _processInputStream;

        public ExecutionPlatform Platform
        {
            get => _platform ?? throw new InvalidOperationException("No platform selected");
            set
            {
                _platform = value;
                InitializeBuildPath(stop: true);
                TryRestore();
            }
        }

        public bool HasPlatform => _platform != null;

        public string? DotNetExecutable { get; set; }

        public string Name
        {
            get => _name;
            set
            {
                if (!string.Equals(_name, value, StringComparison.Ordinal))
                {
                    _name = value;
                    InitializeBuildPath(stop: false);
                    TryRestore();
                }
            }
        }

        private readonly JsonSerializer _jsonSerializer;

        private string BuildPath => _parameters.BuildPath;

        public ExecutionHost(ExecutionHostParameters parameters, IRoslynHost roslynHost)
        {
            _jsonSerializer = new JsonSerializer
            {
                TypeNameHandling = TypeNameHandling.Auto,
                TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple
            };

            _name = "";
            _parameters = parameters;
            _roslynHost = roslynHost;
            _analyzerAssemblyLoader = _roslynHost.GetService<IAnalyzerAssemblyLoader>();
            _libraries = new HashSet<LibraryRef>();
            _scriptOptions = ScriptOptions.Default
                   .WithImports(parameters.Imports)
                   .WithMetadataResolver(new CachedScriptMetadataResolver(parameters.WorkingDirectory));

            _initHostSyntax = ParseSyntaxTree(@"RoslynPad.Runtime.RuntimeInitializer.Initialize();", roslynHost.ParseOptions);

            MetadataReferences = ImmutableArray<MetadataReference>.Empty;
        }

        private static void WriteJson(string path, JToken token)
        {
            using var file = File.CreateText(path);
            using var writer = new JsonTextWriter(file);
            token.WriteTo(writer);
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
            await new NoContextYieldAwaitable();

            await RestoreTask.ConfigureAwait(false);

            try
            {
                _running = true;

                using var executeCts = new CancellationTokenSource();
                var cancellationToken = executeCts.Token;

                var script = CreateScriptRunner(code, optimizationLevel);

                _assemblyPath = Path.Combine(BuildPath, "bin", $"rp-{Name}.{AssemblyExtension}");

                var diagnostics = await script.SaveAssembly(_assemblyPath, cancellationToken).ConfigureAwait(false);
                SendDiagnostics(diagnostics);

                if (diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
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

        private string AssemblyExtension => Platform.IsCore ? "dll" : "exe";

        public ImmutableArray<MetadataReference> MetadataReferences { get; private set; }
        public ImmutableArray<AnalyzerFileReference> Analyzers { get; private set; }

        private ScriptRunner CreateScriptRunner(string code, OptimizationLevel? optimizationLevel)
        {
            Platform platform = Platform.Architecture == Architecture.X86
                ? Microsoft.CodeAnalysis.Platform.AnyCpu32BitPreferred
                : Microsoft.CodeAnalysis.Platform.AnyCpu;

            return new ScriptRunner(code: null,
                                    syntaxTrees: ImmutableList.Create(_initHostSyntax, ParseCode(code)),
                                    _roslynHost.ParseOptions as CSharpParseOptions,
                                    OutputKind.ConsoleApplication,
                                    platform,
                                    _scriptOptions.MetadataReferences,
                                    _scriptOptions.Imports,
                                    _scriptOptions.FilePath,
                                    _parameters.WorkingDirectory,
                                    _scriptOptions.MetadataResolver,
                                    optimizationLevel: optimizationLevel ?? OptimizationLevel.Release,
                                    checkOverflow: _parameters.CheckOverflow,
                                    allowUnsafe: _parameters.AllowUnsafe);
        }

        private async Task RunProcess(string assemblyPath, CancellationToken cancellationToken)
        {
            using (var process = new Process
            {
                StartInfo = GetProcessStartInfo(assemblyPath)
            })
            using (cancellationToken.Register(() =>
            {
                try
                {
                    _processInputStream = null;
                    process.Kill();
                }
                catch { }
            }))
            {
                if (process.Start())
                {
                    _processInputStream = new StreamWriter(process.StandardInput.BaseStream, Encoding.UTF8);

                    await Task.WhenAll(
                        Task.Run(() => ReadObjectProcessStream(process.StandardOutput)),
                        Task.Run(() => ReadProcessStream(process.StandardError)));
                }
            }

            ProcessStartInfo GetProcessStartInfo(string assemblyPath)
            {
                return new ProcessStartInfo
                {
                    FileName = Platform.IsCore ? DotNetExecutable : assemblyPath,
                    Arguments = $"\"{assemblyPath}\" --pid {CurrentPid.Value}",
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
                        case InputReadRequest _:
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
                    Dumped?.Invoke(ResultObject.Create("Error deserializing result: " + ex.Message, DumpQuotas.Default));
                }
            }
        }

        private SyntaxTree ParseCode(string code)
        {
            var tree = ParseSyntaxTree(code, _roslynHost.ParseOptions);
            var root = tree.GetRoot();

            if (root is CompilationUnitSyntax c)
            {
                var members = c.Members;

                // add .Dump() to the last bare expression
                var lastMissingSemicolon = c.Members.OfType<GlobalStatementSyntax>()
                    .LastOrDefault(m => m.Statement is ExpressionStatementSyntax expr && expr.SemicolonToken.IsMissing);
                if (lastMissingSemicolon != null)
                {
                    var statement = (ExpressionStatementSyntax)lastMissingSemicolon.Statement;

                    members = members.Replace(lastMissingSemicolon,
                        GlobalStatement(
                            ExpressionStatement(
                            InvocationExpression(
                                MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    statement.Expression,
                                    IdentifierName(nameof(ObjectExtensions.Dump)))))));
                }

                root = c.WithMembers(members);
            }

            return tree.WithRootAndOptions(root, _roslynHost.ParseOptions);
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

        private void StopProcess()
        {
            _executeCts?.Cancel();
        }

        public void UpdateLibraries(IList<LibraryRef> libraries)
        {
            lock (_libraries)
            {
                if (!_libraries.SetEquals(libraries))
                {
                    _libraries.Clear();
                    _libraries.UnionWith(libraries);

                    TryRestore();
                }
            }
        }

        private Task RestoreTask => _restoreTask ?? Task.CompletedTask;

        public void TryRestore()
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

            var restoreCts = new CancellationTokenSource();
            _restoreTask = RestoreAsync(RestoreTask, restoreCts.Token);
            _restoreCts = restoreCts;

            async Task RestoreAsync(Task previousTask, CancellationToken cancellationToken)
            {
                if (DotNetExecutable == null)
                {
                    return;
                }

                try
                {
                    await previousTask.ConfigureAwait(false);
                }
                catch { }

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

                    _scriptOptions = _scriptOptions.WithReferences(MetadataReferences);

                    RestoreCompleted?.Invoke(RestoreResult.SuccessResult);
                }
                catch (Exception ex) when (!(ex is OperationCanceledException))
                {
                    RestoreCompleted?.Invoke(RestoreResult.FromErrors(new[] { ex.Message }));
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
            }

            async Task BuildGlobalJson()
            {
                if (Platform?.IsCore == true)
                {
                    var global = new JObject(
                        new JProperty("sdk", new JObject(
                            new JProperty("version", Platform.FrameworkVersion))));

                    await File.WriteAllTextAsync(Path.Combine(BuildPath, "global.json"), global.ToString());
                }
            }

            async Task<string> BuildCsproj()
            {
                var csproj = MSBuildHelper.CreateCsproj(
                    Platform.TargetFrameworkMoniker,
                    _libraries);
                var csprojPath = Path.Combine(BuildPath, $"rp-{Name}.csproj");

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

                static string[] GetErrorsFromResult(ProcessUtil.ProcessResult result)
                {
                    return new[] { result.StandardOutput, result.StandardError! };
                }
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