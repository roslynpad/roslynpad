using System.Buffers;
using System.Buffers.Text;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Extensions.Logging;
using Mono.Cecil;
using RoslynPad.Roslyn.FileBasedPrograms;
using Nerdbank.Streams;
using NuGet.Versioning;
using RoslynPad.Build.ILDecompiler;
using RoslynPad.Roslyn;

// Reflection-based System.Text.Json on build-output DTOs: this assembly is a trim root
// (TrimMode=partial), so the serialized types are preserved.
#pragma warning disable IL2026

namespace RoslynPad.Build;

/// <summary>
/// An <see cref="IExecutionHost"/> implementation that compiles to disk and executes in separated processes.
/// </summary>
internal partial class ExecutionHost : IExecutionHost, IDisposable
{
    private static readonly string s_version = typeof(ExecutionContext).Assembly.GetName().Version?.ToString() ?? string.Empty;

    private static readonly JsonSerializerOptions s_serializerOptions = new()
    {
        Converters =
        {
            // needed since JsonReaderWriterFactory writes those types as strings
            new BooleanConverter(),
        },
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    private static readonly ImmutableArray<byte> s_newLine = [.. Encoding.UTF8.GetBytes(Environment.NewLine)];

    // Restore directories are content-hashed and shared across documents; serialize builds
    // of the same directory so concurrent MSBuild processes don't collide on its output files.
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> s_restoreLocks = new();

    private readonly ExecutionHostParameters _parameters;
    private readonly IRoslynHost _roslynHost;
    private readonly ILogger _logger;
    private readonly IAnalyzerAssemblyLoader _analyzerAssemblyLoader;
    private readonly SortedSet<LibraryRef> _libraries;
    private readonly SemaphoreSlim _lock;
    private readonly LibraryRef _runtimeAssemblyLibraryRef;
    private readonly LibraryRef _runtimeNetFxAssemblyLibraryRef;
    private readonly string _restoreCachePath;
    private readonly object _ctsLock;
    private CancellationTokenSource? _executeCts;
    private CancellationTokenSource? _restoreCts;
    private ExecutionPlatform? _platform;
    private string? _restorePath;
    private string? _assemblyPath;
    private string _name;
    private bool _running;
    private bool _initializeBuildPathAfterRun;
    private bool _hasFileBasedDirectives;
    private bool _hasLegacyPackageDirectives;
    private ImmutableArray<string> _fileBasedDirectives = [];
    private string? _targetFrameworkOverride;
    private TextWriter? _processInputStream;
    private string? _dotNetExecutable;

    public ExecutionPlatform Platform
    {
        get => _platform ?? throw new InvalidOperationException("No platform selected");
        set
        {
            _platform = value;
            InitializeBuildPath(stopProcess: true);
        }
    }

    private bool IsScript => _parameters.SourceCodeKind == SourceCodeKind.Script;

    private const string TargetFrameworkPropertyName = "TargetFramework";

    /// <summary>
    /// The target framework the code is compiled against: the platform's, unless the code
    /// overrides it with <c>#:property TargetFramework=...</c> (e.g. to target
    /// <c>net10.0-windows</c>). The selected SDK - and with it <c>global.json</c> - is unaffected;
    /// a newer SDK can compile an older or platform-specific framework.
    /// </summary>
    private string TargetFrameworkMoniker => _targetFrameworkOverride ?? Platform.TargetFrameworkMoniker;

    public bool UseCache => Platform.FrameworkVersion?.Major >= 6;

    /// <summary>
    /// Returns true if the current platform supports .NET file-based apps (dotnet run file.cs)
    /// and the code contains file-based directives (#:package or #:sdk).
    /// </summary>
    private bool UseFileBasedExecution
    {
        get
        {
            if (!Platform.SupportsFileBasedApps)
            {
                return false;
            }

            lock (_libraries)
            {
                // Check if any file-based package references exist (parsed from #:package)
                // We detect this by checking if we have package references but no #r nuget: directives
                // Actually, we need to track this separately since both parse to PackageReference
                return _hasFileBasedDirectives;
            }
        }
    }

    public bool UseFileBasedReferences => !IsScript && Platform.SupportsFileBasedApps && !_hasLegacyPackageDirectives;

    public bool HasPlatform => _platform != null;

    public string DotNetExecutable
    {
        get => HasDotNetExecutable ? _dotNetExecutable : throw new InvalidOperationException("Missing dotnet");
        set => _dotNetExecutable = value;
    }

    [MemberNotNullWhen(true, nameof(_dotNetExecutable))]
    private bool HasDotNetExecutable => !string.IsNullOrEmpty(_dotNetExecutable);

    public string Name
    {
        get => _name;
        set
        {
            if (!string.Equals(_name, value, StringComparison.Ordinal))
            {
                _name = value;
                InitializeBuildPath(stopProcess: false);
                _ = RestoreAsync();
            }
        }
    }

    private string BuildPath => _parameters.BuildPath;

    private string ScriptCompileTaskAssemblyPath { get; }

    private string ExecutableExtension => Platform.IsDotNet ? "dll" : "exe";

    public ImmutableArray<MetadataReference> MetadataReferences { get; private set; } = [];
    public ImmutableArray<AnalyzerFileReference> Analyzers { get; private set; } = [];

    public ExecutionHost(ExecutionHostParameters parameters, IRoslynHost roslynHost, ILogger logger)
    {
        _name = "";
        _parameters = parameters;
        _roslynHost = roslynHost;
        _logger = logger;
        _analyzerAssemblyLoader = _roslynHost.GetService<IAnalyzerAssemblyLoader>();
        _libraries = [];

        _ctsLock = new object();
        _lock = new SemaphoreSlim(1, 1);

        MetadataReferences = [];

        _runtimeAssemblyLibraryRef = LibraryRef.Reference(Path.Combine(AppContext.BaseDirectory, "runtimes", "net", "RoslynPad.Runtime.dll"));
        _runtimeNetFxAssemblyLibraryRef = LibraryRef.Reference(Path.Combine(AppContext.BaseDirectory, "runtimes", "netfx", "RoslynPad.Runtime.dll"));

        ScriptCompileTaskAssemblyPath = Path.Combine(AppContext.BaseDirectory, "BuildTasks", "RoslynPad.BuildTasks.dll");

        _restoreCachePath = Path.Combine(Path.GetTempPath(), "roslynpad", "restore");
    }

    public event Action<IList<CompilationErrorResultObject>>? CompilationErrors;
    public event Action<string>? Disassembled;
    public event Action<ResultObject>? Dumped;
    public event Action<ExceptionResultObject>? Error;
    public event Action? ReadInput;
    public event Action? RestoreStarted;
    public event Action<RestoreResult>? RestoreCompleted;
    public event Action<ProgressResultObject>? ProgressChanged;

    public Func<BuildOutputSource, bool, TextWriter>? BuildOutputWriterFactory { get; set; }

    private TextWriter CreateBuildOutputWriter(BuildOutputSource source, bool cached = false) =>
        BuildOutputWriterFactory?.Invoke(source, cached) ?? TextWriter.Null;

    public void Dispose()
    {
        _executeCts?.Dispose();
        _restoreCts?.Dispose();
    }

    private void InitializeBuildPath(bool stopProcess)
    {
        if (!HasPlatform)
        {
            return;
        }

        if (stopProcess)
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

    public void ClearRestoreCache() => Directory.Delete(_restoreCachePath);

    public async Task ExecuteAsync(string path, bool disassemble, OptimizationLevel? optimizationLevel, CancellationToken cancellationToken)
    {
        if (!HasDotNetExecutable)
        {
            NoDotNetError();
            return;
        }

        _logger.StartExecuteAsync();

        await new NoContextYieldAwaitable();

        if (!(await RestoreTask.ConfigureAwait(false)).Success)
        {
            return;
        }

        using var executeCts = CancelAndCreateNew(ref _executeCts, cancellationToken);
        cancellationToken = executeCts.Token;

        using var _ = await _lock.DisposableWaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            _running = true;

            // Traditional execution: compile first, then run
            _assemblyPath = Path.Combine(BuildPath, "bin", $"{Name}.{ExecutableExtension}");

            var success = await CompileWithMsbuild(path, optimizationLevel, cancellationToken).ConfigureAwait(false);

            if (!success)
            {
                return;
            }

            if (disassemble)
            {
                Disassemble();
            }

            await ExecuteAssemblyAsync(_assemblyPath, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _executeCts?.Dispose();
            _executeCts = null;
            _running = false;

            if (_initializeBuildPathAfterRun)
            {
                _initializeBuildPathAfterRun = false;
                InitializeBuildPath(stopProcess: false);
            }
        }
    }

    private async Task<bool> CompileWithMsbuild(string path, OptimizationLevel? optimizationLevel, CancellationToken cancellationToken)
    {
        if (_restorePath is null)
        {
            return false;
        }

        var targetPath = Path.Combine(BuildPath, IsScript ? MSBuildHelper.ScriptFileName : "Program.cs");
        var code = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
        var parseOptions = ((CSharpParseOptions)_roslynHost.ParseOptions).WithKind(_parameters.SourceCodeKind);
        var syntaxTree = ParseAndTransformCode(code, path, parseOptions, cancellationToken: cancellationToken);
        var finalCode = syntaxTree.ToString();
        if (!File.Exists(targetPath) || !string.Equals(await File.ReadAllTextAsync(targetPath, cancellationToken).ConfigureAwait(false), finalCode, StringComparison.Ordinal))
        {
            await File.WriteAllTextAsync(targetPath, finalCode, cancellationToken).ConfigureAwait(false);
        }

        var csprojPath = Path.Combine(BuildPath, UseCache ? "program.csproj" : $"{Name}.csproj");
        if (IsScript)
        {
            var scriptInitFile = Path.Combine(BuildPath, MSBuildHelper.ScriptInitFileName);
            if (!File.Exists(scriptInitFile))
            {
                await File.WriteAllTextAsync(scriptInitFile, BuildCode.ScriptInit, cancellationToken).ConfigureAwait(false);
            }
        }
        else
        {
            if (Platform.IsDotNetFramework || Platform.FrameworkVersion?.Major < 5)
            {
                var moduleInitAttributeFile = Path.Combine(BuildPath, BuildCode.ModuleInitAttributeFileName);
                if (!File.Exists(moduleInitAttributeFile))
                {
                    await File.WriteAllTextAsync(moduleInitAttributeFile, BuildCode.ModuleInitAttribute, cancellationToken).ConfigureAwait(false);
                }
            }

            var moduleInitFile = Path.Combine(BuildPath, BuildCode.ModuleInitFileName);
            if (!File.Exists(moduleInitFile))
            {
                await File.WriteAllTextAsync(moduleInitFile, BuildCode.ModuleInit, cancellationToken).ConfigureAwait(false);
            }
        }

        var buildWarningsPath = Path.Combine(BuildPath, "build-warnings.log");
        var buildErrorsPath = Path.Combine(BuildPath, "build-errors.log");

        var scriptArgs = IsScript
            ? $"\"-p:RoslynPadWorkingDirectory={_parameters.WorkingDirectory}\" " +
              $"-p:RoslynPadPrefer32Bit={(Platform.Architecture == Architecture.X86 ? "true" : "false")} " +
              $"-p:CheckForOverflowUnderflow={(_parameters.CheckOverflow ? "true" : "false")} "
            : string.Empty;
        var buildArgs =
            $"-nologo -v:m -p:Configuration={optimizationLevel} \"-p:AssemblyName={Name}\" {scriptArgs}" +
            $"\"-flp1:logfile={buildWarningsPath};warningsonly;Encoding=UTF-8\" \"-flp2:logfile={buildErrorsPath};errorsonly;Encoding=UTF-8\" \"{csprojPath}\" ";

        using var buildResult = await ProcessUtil.RunProcessAsync(DotNetExecutable, BuildPath,
            $"build {buildArgs}", cancellationToken).ConfigureAwait(false);

        using (var output = CreateBuildOutputWriter(BuildOutputSource.Compile))
        {
            await foreach (var line in buildResult.GetStandardOutputLinesAsync().WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                await output.WriteLineAsync(line).ConfigureAwait(false);
            }

            await WriteErrorLinesAsync(output, buildResult.StandardError).ConfigureAwait(false);
        }

        var compilationErrors = await ReadBuildLogAsync(buildWarningsPath, "Warning")
            .Concat(ReadBuildLogAsync(buildErrorsPath, "Error"))
            .ToArrayAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

        var success = buildResult.ExitCode == 0;
        if (!success && compilationErrors.Length == 0)
        {
            var output = buildResult.StandardError;
            if (string.IsNullOrWhiteSpace(output))
            {
                output = buildResult.StandardOutput;
            }
            compilationErrors = [new CompilationErrorResultObject { Severity = "Error", Message = "Build failed: " + output }];
        }

        CompilationErrors?.Invoke(compilationErrors);

        return success;
    }

    private static async Task WriteErrorLinesAsync(TextWriter output, string? standardError)
    {
        if (standardError is not { Length: > 0 })
        {
            return;
        }

        foreach (var line in standardError.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            await output.WriteLineAsync(line).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Runs <c>dotnet project convert</c> on a minimal file containing the stored file-based
    /// directives, then patches the resulting csproj with RoslynPad build settings.
    /// </summary>
    private async Task<XDocument> ConvertFileBasedToCsprojAsync(CancellationToken cancellationToken)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "roslynpad", "convert", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            var tempFile = Path.Combine(tempDir, "Program.cs");
            var document = DocumentId is not null ? _roslynHost.GetDocument(DocumentId) : null;
            var sourceText = document is not null ? await document.GetTextAsync(cancellationToken).ConfigureAwait(false) : null;
            if (sourceText is null)
            {
                return MSBuildHelper.CreateCsproj(TargetFrameworkMoniker, _libraries, _parameters.Imports);
            }
            await File.WriteAllTextAsync(tempFile, sourceText.ToString(), cancellationToken).ConfigureAwait(false);

            // Use a separate output directory because --output requires a non-existent directory
            var outputDir = Path.Combine(tempDir, "out");

            // Suppress Directory.Build.props/targets from parent directories
            await File.WriteAllTextAsync(Path.Combine(tempDir, "Directory.Build.props"), "<Project/>", cancellationToken).ConfigureAwait(false);
            await File.WriteAllTextAsync(Path.Combine(tempDir, "Directory.Build.targets"), "<Project/>", cancellationToken).ConfigureAwait(false);

            using var convertResult = await ProcessUtil.RunProcessAsync(DotNetExecutable, tempDir,
                $"project convert \"{tempFile}\" --output \"{outputDir}\"", cancellationToken).ConfigureAwait(false);
            await convertResult.GetStandardOutputLinesAsync().LastOrDefaultAsync(cancellationToken).ConfigureAwait(false);

            var csprojPath = Path.Combine(outputDir, "Program.csproj");
            if (convertResult.ExitCode != 0 || !File.Exists(csprojPath))
            {
                var error = convertResult.StandardError ?? convertResult.StandardOutput;
                throw new InvalidOperationException($"dotnet project convert failed (exit code {convertResult.ExitCode}): {error}");
            }

            var csproj = XDocument.Load(csprojPath);

            MSBuildHelper.PatchConvertedCsproj(csproj,
                TargetFrameworkMoniker,
                _runtimeAssemblyLibraryRef.Value,
                _parameters.Imports);

            return csproj;
        }
        finally
        {
            IOUtilities.PerformIO(() => Directory.Delete(tempDir, recursive: true));
        }
    }

    private async IAsyncEnumerable<CompilationErrorResultObject> ReadBuildLogAsync(string path, string severity)
    {
        if (!File.Exists(path))
        {
            yield break;
        }

        await foreach (var line in File.ReadLinesAsync(path).ConfigureAwait(false))
        {
            var match = MsbuildLogRegex().Match(line);
            if (!match.Success)
            {
                continue;
            }

            var code = match.Groups["code"].Value;
            var error = new CompilationErrorResultObject
            {
                Severity = severity,
                ErrorCode = code,
                Message = match.Groups["message"].Value,
            };

            if (match.Groups["file"].Value.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) ||
                match.Groups["file"].Value.EndsWith(".csx", StringComparison.OrdinalIgnoreCase))
            {
                error.LineNumber = int.Parse(match.Groups["line"].ValueSpan, CultureInfo.InvariantCulture);
                error.Column = int.Parse(match.Groups["column"].ValueSpan, CultureInfo.InvariantCulture);
            }

            yield return error;
        }
    }

    private void NoDotNetError()
    {
        CompilationErrors?.Invoke(
        [
            CompilationErrorResultObject.Create("Error", errorCode: "",
                message: ErrorMessages.MissingSdk, line: 0, column: 0)
        ]);
    }

    private void Disassemble()
    {
        using var assembly = AssemblyDefinition.ReadAssembly(_assemblyPath);
        var output = new PlainTextOutput();
        var disassembler = new ReflectionDisassembler(output, false, CancellationToken.None);
        disassembler.WriteModuleContents(assembly.MainModule);
        Disassembled?.Invoke(output.ToString());
    }

    private async Task ExecuteAssemblyAsync(string assemblyPath, CancellationToken cancellationToken)
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
                _logger.ErrorKillingProcess(ex);
            }
        });

        _logger.StartingProcess(process.StartInfo.FileName, process.StartInfo.Arguments);
        if (!process.Start())
        {
            _logger.ProcessStartReturnedFalse();
            return;
        }

        _processInputStream = new StreamWriter(process.StandardInput.BaseStream, Encoding.UTF8);

        await Task.WhenAll(
            Task.Run(() => ReadObjectProcessStreamAsync(process.StandardOutput), cancellationToken),
            Task.Run(() => ReadProcessStreamAsync(process.StandardError), cancellationToken)).ConfigureAwait(false);

        ProcessStartInfo GetProcessStartInfo(string assemblyPath) => new()
        {
            FileName = Platform.IsDotNet ? DotNetExecutable : assemblyPath,
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

    private async Task ReadProcessStreamAsync(StreamReader reader)
    {
        while (await reader.ReadLineAsync().ConfigureAwait(false) is { } line)
        {
            Dumped?.Invoke(new ResultObject { Value = line });
        }
    }

    private async Task ReadObjectProcessStreamAsync(StreamReader reader)
    {
        const int prefixLength = 2;
        using var sequence = new Sequence<byte>(ArrayPool<byte>.Shared) { AutoIncreaseMinimumSpanLength = false };
        while (true)
        {
            var eolPosition = await ReadLineAsync().ConfigureAwait(false);
            if (eolPosition == null)
            {
                return;
            }

            var readOnlySequence = sequence.AsReadOnlySequence;
            if (readOnlySequence.FirstSpan.Length > 1 && readOnlySequence.FirstSpan[1] == ':')
            {
                switch (readOnlySequence.FirstSpan[0])
                {
                    case (byte)'i':
                        ReadInput?.Invoke();
                        break;
                    case (byte)'o':
                        var objectResult = Deserialize<ResultObject>(readOnlySequence);
                        Dumped?.Invoke(objectResult);
                        break;
                    case (byte)'e':
                        var exceptionResult = Deserialize<ExceptionResultObject>(readOnlySequence);
                        Error?.Invoke(exceptionResult);
                        break;
                    case (byte)'p':
                        var progressResult = Deserialize<ProgressResultObject>(readOnlySequence);
                        ProgressChanged?.Invoke(progressResult);
                        break;

                }
            }

            sequence.AdvanceTo(eolPosition.Value);
        }

        async ValueTask<SequencePosition?> ReadLineAsync()
        {
            var readOnlySequence = sequence.AsReadOnlySequence;
            var position = readOnlySequence.PositionOf(s_newLine[^1]);
            if (position != null)
            {
                return readOnlySequence.GetPosition(1, position.Value);
            }

            while (true)
            {
                var memory = sequence.GetMemory(0);
                var read = await reader.BaseStream.ReadAsync(memory).ConfigureAwait(false);
                if (read == 0)
                {
                    return null;
                }

                var eolIndex = memory.Span.Slice(0, read).IndexOf(s_newLine[^1]);
                if (eolIndex != -1)
                {
                    var length = sequence.Length;
                    sequence.Advance(read);
                    var index = length + eolIndex + 1;
                    return sequence.AsReadOnlySequence.GetPosition(index);
                }

                sequence.Advance(read);
            }
        }

        static T Deserialize<T>(ReadOnlySequence<byte> sequence)
        {
            var jsonReader = new Utf8JsonReader(sequence.Slice(prefixLength));
            return JsonSerializer.Deserialize<T>(ref jsonReader, s_serializerOptions)!;
        }
    }

    private static SyntaxTree ParseAndTransformCode(string code, string path, CSharpParseOptions parseOptions, CancellationToken cancellationToken)
    {
        var tree = SyntaxFactory.ParseSyntaxTree(code, parseOptions, path, cancellationToken: cancellationToken);
        var root = tree.GetRoot(cancellationToken);

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

        // Remove file-level directives (#:package, #:sdk, etc.) from leading trivia -
        // these are resolved by dotnet project convert / msbuild, not the compiler
        var fileLevelDirectives = tree.FindFileLevelDirectives();
        if (fileLevelDirectives.Length > 0)
        {
            var leadingTrivia = compilationUnit.GetLeadingTrivia();
            var newTrivia = leadingTrivia.Where(t =>
                !t.IsKind(SyntaxKind.IgnoredDirectiveTrivia) &&
                !t.IsKind(SyntaxKind.ShebangDirectiveTrivia));
            compilationUnit = compilationUnit.WithLeadingTrivia(newTrivia);
        }

        var members = compilationUnit.Members;

        // add .Dump() to the last bare expression
        var lastMissingSemicolon = BuildCode.FindTrailingExpression(compilationUnit);
        if (lastMissingSemicolon != null)
        {
            var statement = (ExpressionStatementSyntax)lastMissingSemicolon.Statement;
            members = members.Replace(lastMissingSemicolon, BuildCode.GetDumpCall(statement));
        }

        root = compilationUnit.WithMembers(members);

        return tree.WithRootAndOptions(root, parseOptions);
    }

    public Task TerminateAsync()
    {
        StopProcess();
        return Task.CompletedTask;
    }

    private void StopProcess() => _executeCts?.Cancel();

    public async Task UpdateReferencesAsync(bool alwaysRestore)
    {
        var syntaxRoot = await GetSyntaxRootAsync().ConfigureAwait(false);
        if (syntaxRoot == null)
        {
            return;
        }

        var parsed = ParseReferences(syntaxRoot);
        var allLibraries = parsed.Libraries.Append(Platform.IsDotNet ? _runtimeAssemblyLibraryRef : _runtimeNetFxAssemblyLibraryRef);
        if (UpdateLibraries(allLibraries, parsed))
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

        bool UpdateLibraries(IEnumerable<LibraryRef> libraries, ParsedReferences parsed)
        {
            lock (_libraries)
            {
                var librariesChanged = !_libraries.SetEquals(libraries);
                // Directives that don't map to a library (#:property, #:sdk) still change the
                // generated csproj, so any change in the directive list must trigger a restore.
                var directivesChanged = !_fileBasedDirectives.SequenceEqual(parsed.Directives, StringComparer.Ordinal);
                var legacyChanged = _hasLegacyPackageDirectives != parsed.HasLegacyPackageDirectives;

                if (librariesChanged || directivesChanged || legacyChanged)
                {
                    _libraries.Clear();
                    _libraries.UnionWith(libraries);
                    _fileBasedDirectives = parsed.Directives;
                    _hasFileBasedDirectives = parsed.Directives.Length > 0;
                    _hasLegacyPackageDirectives = parsed.HasLegacyPackageDirectives;
                    _targetFrameworkOverride = parsed.TargetFramework;
                    return true;
                }
                else if (alwaysRestore)
                {
                    return true;
                }
            }

            return false;
        }

        static ParsedReferences ParseReferences(SyntaxNode syntaxRoot)
        {
            var libraries = new List<LibraryRef>();
            var directives = ImmutableArray.CreateBuilder<string>();
            var hasLegacyPackageDirectives = false;
            string? targetFramework = null;

            if (syntaxRoot is not CompilationUnitSyntax compilation)
            {
                return new(libraries, [], hasLegacyPackageDirectives, targetFramework);
            }

            // Parse file-level directives (#:package, #:property, ...) using syntax tree
            foreach (var directive in syntaxRoot.SyntaxTree.FindFileLevelDirectives())
            {
                switch (directive.DirectiveKind)
                {
                    case "package":
                        var (id, version) = ReferenceDirectiveHelpers.ParsePackageDirective(directive.DirectiveText);
                        if (!string.IsNullOrEmpty(id))
                        {
                            libraries.Add(LibraryRef.PackageReference(id, version ?? string.Empty));
                        }
                        break;
                    case "framework":
                        if (!string.IsNullOrEmpty(directive.DirectiveText))
                        {
                            libraries.Add(LibraryRef.FrameworkReference(directive.DirectiveText));
                        }
                        break;
                    case "property":
                        var (name, value) = ReferenceDirectiveHelpers.ParsePropertyDirective(directive.DirectiveText);
                        if (string.Equals(name, TargetFrameworkPropertyName, StringComparison.OrdinalIgnoreCase) &&
                            !string.IsNullOrEmpty(value))
                        {
                            targetFramework = value;
                        }
                        break;
                }

                if (directive.DirectiveKind is not ("" or "shebang"))
                {
                    directives.Add($"{directive.DirectiveKind} {directive.DirectiveText}");
                }
            }

            // Parse traditional #r directives
            foreach (var directive in compilation.GetReferenceDirectives())
            {
                var value = directive.File.ValueText;
                string? id, version;

                if (ReferenceDirectiveHelpers.HasPrefix(ReferenceDirectiveHelpers.FrameworkPrefix, value))
                {
                    libraries.Add(LibraryRef.FrameworkReference(
                        value[ReferenceDirectiveHelpers.FrameworkPrefix.Length..]));
                    continue;
                }

                if (ReferenceDirectiveHelpers.HasPrefix(ReferenceDirectiveHelpers.NuGetPrefix, value))
                {
                    (id, version) = ReferenceDirectiveHelpers.ParseNuGetReference(value);
                    hasLegacyPackageDirectives = true;
                }
                else if (ReferenceDirectiveHelpers.HasPrefix(ReferenceDirectiveHelpers.LegacyNuGetPrefix, value))
                {
                    (id, version) = ReferenceDirectiveHelpers.ParseLegacyNuGetReference(value);
                    hasLegacyPackageDirectives = true;
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

            return new(libraries, directives.ToImmutable(), hasLegacyPackageDirectives, targetFramework);
        }
    }

    private sealed record ParsedReferences(
        List<LibraryRef> Libraries,
        ImmutableArray<string> Directives,
        bool HasLegacyPackageDirectives,
        string? TargetFramework);

    private Task<RestoreResult> RestoreTask { get => field ?? Task.FromResult(RestoreResult.SuccessResult); set; }

    public DocumentId? DocumentId { get; set; }

    private async Task RestoreAsync(CancellationToken cancellationToken = default)
    {
        if (!HasPlatform || string.IsNullOrEmpty(Name))
        {
            return;
        }

        var restoreCts = CancelAndCreateNew(ref _restoreCts, cancellationToken);
        cancellationToken = restoreCts.Token;

        RestoreStarted?.Invoke();

        var lockDisposer = await _lock.DisposableWaitAsync(cancellationToken).ConfigureAwait(false);
        RestoreTask = DoRestoreAsync(RestoreTask, cancellationToken);

        async Task<RestoreResult> DoRestoreAsync(Task previousTask, CancellationToken cancellationToken)
        {
            try
            {
                if (!HasDotNetExecutable)
                {
                    NoDotNetError();
                    return RestoreResult.FromErrors([ErrorMessages.MissingSdk]);
                }

                try
                {
                    await previousTask.ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.ErrorInPreviousRestoreTask(ex);
                }

                var projBuildResult = await BuildCsproj().ConfigureAwait(false);

                var outputPath = Path.Combine(projBuildResult.RestorePath, "output.json");
                var outputLogPath = Path.Combine(projBuildResult.RestorePath, "output.log");

                var restored = false;
                if (!projBuildResult.MarkerExists)
                {
                    var restoreLock = s_restoreLocks.GetOrAdd(projBuildResult.RestorePath, static _ => new SemaphoreSlim(1, 1));
                    using var restoreLockDisposer = await restoreLock.DisposableWaitAsync(cancellationToken).ConfigureAwait(false);

                    // another document may have restored this directory while we waited
                    if (!projBuildResult.UsesCache || !File.Exists(projBuildResult.MarkerPath))
                    {
                        await Task.Run(() => projBuildResult.Csproj.Save(projBuildResult.CsprojPath), cancellationToken).ConfigureAwait(false);
                        await BuildGlobalJson(projBuildResult.RestorePath).ConfigureAwait(false);
                        File.Copy(_parameters.NuGetConfigPath, Path.Combine(projBuildResult.RestorePath, "nuget.config"), overwrite: true);

                        var restoreErrorsPath = Path.Combine(projBuildResult.RestorePath, "restore-errors.log");
                        File.Delete(restoreErrorsPath);

                        cancellationToken.ThrowIfCancellationRequested();

                        // A design-time build (the properties custom targets key off): references and
                        // analyzers resolve without compiling or producing outputs, and
                        // -getResultOutputFile keeps the item JSON off stdout so it stays a
                        // human-readable, streamable log.
                        var buildArgs =
                            $"-restore -interactive -nologo -v:m " +
                            $"-flp:errorsonly;logfile=\"{restoreErrorsPath}\";Encoding=UTF-8 \"{projBuildResult.CsprojPath}\" " +
                            $"-t:Compile -p:DesignTimeBuild=true -p:SkipCompilerExecution=true " +
                            $"-getItem:ReferencePathWithRefAssemblies,Analyzer \"-getResultOutputFile:{outputPath}\" ";
                        using var restoreResult = await ProcessUtil.RunProcessAsync(DotNetExecutable, BuildPath,
                            $"msbuild {buildArgs}", cancellationToken).ConfigureAwait(false);

                        // The log is persisted next to the cache marker so cache hits can replay it.
                        using (var output = CreateBuildOutputWriter(BuildOutputSource.Restore))
                        using (var logWriter = IOUtilities.PerformIO(() => File.CreateText(outputLogPath)))
                        {
                            await foreach (var line in restoreResult.GetStandardOutputLinesAsync().WithCancellation(cancellationToken).ConfigureAwait(false))
                            {
                                await output.WriteLineAsync(line).ConfigureAwait(false);
                                if (logWriter is not null)
                                {
                                    await logWriter.WriteLineAsync(line).ConfigureAwait(false);
                                }
                            }

                            await WriteErrorLinesAsync(output, restoreResult.StandardError).ConfigureAwait(false);
                            await WriteErrorLinesAsync(logWriter ?? TextWriter.Null, restoreResult.StandardError).ConfigureAwait(false);
                        }

                        if (restoreResult.ExitCode != 0)
                        {
                            var errors = await GetRestoreErrorsAsync(restoreErrorsPath, restoreResult, cancellationToken).ConfigureAwait(false);
                            var errorResult = RestoreResult.FromErrors(errors);
                            RestoreCompleted?.Invoke(errorResult);
                            return errorResult;
                        }

                        if (projBuildResult.UsesCache)
                        {
                            await File.WriteAllTextAsync(projBuildResult.MarkerPath, string.Empty, cancellationToken).ConfigureAwait(false);
                        }

                        restored = true;
                    }
                }

                if (!restored)
                {
                    ReplayRestoreOutput(outputLogPath);
                }

                if (projBuildResult.UsesCache)
                {
                    IOUtilities.DirectoryCopy(projBuildResult.RestorePath, BuildPath, overwrite: true, recursive: false);
                    await File.WriteAllTextAsync(Path.Combine(BuildPath, Path.GetFileName(projBuildResult.RestorePath)), string.Empty, cancellationToken).ConfigureAwait(false);
                }

                await ReadReferencesAsync(outputPath, cancellationToken).ConfigureAwait(false);
                RestoreCompleted?.Invoke(RestoreResult.SuccessResult);
                return RestoreResult.SuccessResult;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.RestoreError(ex);
                var errorResult = RestoreResult.FromErrors([ex.ToString()]);
                RestoreCompleted?.Invoke(errorResult);
                return errorResult;
            }
            finally
            {
                lockDisposer.Dispose();
            }
        }

        async Task ReadReferencesAsync(string path, CancellationToken cancellationToken)
        {
            using var stream = File.OpenRead(path);
            var output = await JsonSerializer.DeserializeAsync<BuildOutput>(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
            if (output is null)
            {
                return;
            }

            MetadataReferences = [.. output.Items.ReferencePathWithRefAssemblies
                .Select(r => r.FullPath)
                .Where(r => !string.IsNullOrWhiteSpace(r))
                .Select(_roslynHost.CreateMetadataReference)];

            Analyzers = [.. output.Items.Analyzer
                .Select(r => r.FullPath)
                .Where(r => !string.IsNullOrWhiteSpace(r))
                .Select(r => new AnalyzerFileReference(r, _analyzerAssemblyLoader))];
        }

        // On a cache hit no restore process runs, so the pane replays the log persisted by the
        // restore that populated the cache directory (a pre-feature cache has no log).
        void ReplayRestoreOutput(string outputLogPath)
        {
            using var output = CreateBuildOutputWriter(BuildOutputSource.Restore, cached: true);
            var lines = IOUtilities.PerformIO(() => File.ReadAllLines(outputLogPath), []);
            if (lines is { Length: > 0 })
            {
                foreach (var line in lines)
                {
                    output.WriteLine(line);
                }
            }
            else
            {
                output.WriteLine("Restore up to date (cached).");
            }
        }

        async Task BuildGlobalJson(string restorePath)
        {
            if (Platform?.IsDotNet != true)
            {
                return;
            }

            var globalJson = $@"{{ ""sdk"": {{ ""version"": ""{Platform.FrameworkVersion}"" }} }}";
            await File.WriteAllTextAsync(Path.Combine(restorePath, "global.json"), globalJson, cancellationToken).ConfigureAwait(false);
        }

        async Task<CsprojBuildResult> BuildCsproj()
        {
            XDocument csproj;
            if (UseFileBasedExecution)
            {
                csproj = await ConvertFileBasedToCsprojAsync(cancellationToken).ConfigureAwait(false);
                if (IsScript)
                {
                    MSBuildHelper.ConvertToScriptCsproj(csproj, ScriptCompileTaskAssemblyPath);
                }
            }
            else
            {
                csproj = IsScript
                    ? MSBuildHelper.CreateScriptCsproj(
                        TargetFrameworkMoniker,
                        _libraries,
                        _parameters.Imports,
                        ScriptCompileTaskAssemblyPath)
                    : MSBuildHelper.CreateCsproj(
                        TargetFrameworkMoniker,
                        _libraries,
                        _parameters.Imports);
            }

            string csprojPath;
            string? markerPath;
            bool markerExists;

            if (UseCache)
            {
                var hash = GetHash(csproj.ToString(SaveOptions.DisableFormatting), Platform.Description, s_version);
                var hashedRestorePath = Path.Combine(_restoreCachePath, hash);
                Directory.CreateDirectory(hashedRestorePath);

                csprojPath = Path.Combine(hashedRestorePath, "program.csproj");
                markerPath = Path.Combine(hashedRestorePath, ".restored");
                _restorePath = hashedRestorePath;
                markerExists = File.Exists(markerPath);
            }
            else
            {
                csprojPath = Path.Combine(BuildPath, $"{Name}.csproj");
                markerPath = null;
                _restorePath = BuildPath;
                markerExists = false;
            }

            return new(_restorePath, csprojPath, markerPath, markerExists, csproj);
        }

        static async Task<string[]> GetRestoreErrorsAsync(string errorsPath, ProcessUtil.ProcessResult result, CancellationToken cancellationToken)
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
                        var match = RestoreErrorRegex().Match(errors[i]);
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
        }

        static string[] GetErrorsFromResult(ProcessUtil.ProcessResult result) =>
            [result.StandardError ?? string.Empty];
    }

    private CancellationTokenSource CancelAndCreateNew(ref CancellationTokenSource? cts, CancellationToken cancellationToken)
    {
        lock (_ctsLock)
        {
            if (cts != null)
            {
                cts.Cancel();
                cts.Dispose();
            }

            var newCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts = newCts;
            return newCts;
        }
    }

    private static string GetHash(string a, string b, string c)
    {
        Span<byte> hashBuffer = stackalloc byte[32];
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        hash.AppendData(MemoryMarshal.AsBytes(a.AsSpan()));
        hash.AppendData(MemoryMarshal.AsBytes(b.AsSpan()));
        hash.AppendData(MemoryMarshal.AsBytes(c.AsSpan()));
        hash.TryGetHashAndReset(hashBuffer, out _);
        return Convert.ToHexString(hashBuffer);
    }

    private class BooleanConverter : JsonConverter<bool>
    {
        public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var span = reader.GetSpan();
            return Utf8Parser.TryParse(span.Span, out bool value, out _) ? value : throw new FormatException();
        }

        public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options) => throw new NotSupportedException();
    }

    [GeneratedRegex(@"(?<=\: error )[^\]]+")]
    private static partial Regex RestoreErrorRegex();

    // The span is (line,col) from csc and (line,col,endLine,endCol) from ScriptCompileTask
    [GeneratedRegex(@"(?<file>[\\/][^\\/(]+)?\((?<line>\d+),(?<column>\d+)(,\d+,\d+)?\): (?<severity>warning|error) (?<code>\w+): ((?<message>.+)\s*\[.+\]|(?<message>.+))", RegexOptions.ExplicitCapture)]
    private static partial Regex MsbuildLogRegex();

    private record BuildOutput(BuildOutputItems Items);
    private record BuildOutputItems(BuildOutputReferenceItem[] ReferencePathWithRefAssemblies, BuildOutputReferenceItem[] Analyzer);
    private record BuildOutputReferenceItem(string FullPath);
    private record CsprojBuildResult(string RestorePath, string CsprojPath, string? MarkerPath, bool MarkerExists, XDocument Csproj)
    {
        [MemberNotNullWhen(true, nameof(MarkerPath))]
        public bool UsesCache => MarkerPath is not null;
    }
}
