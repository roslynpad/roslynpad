using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Scripting;
using Mono.Cecil;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RoslynPad.Hosting.ILDecompiler;
using RoslynPad.Roslyn;
using RoslynPad.Roslyn.Scripting;
using RoslynPad.Runtime;
using RoslynPad.Utilities;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace RoslynPad.Hosting
{
    /// <summary>
    /// An <see cref="IExecutionHost"/> implementation that compiles scripts to disk as EXEs and executes them in their own process.
    /// </summary>
    internal class AssemblyExecutionHost : IExecutionHost
    {
        private static readonly CSharpParseOptions _parseOptions = new CSharpParseOptions(preprocessorSymbols: new[] { "__DEMO__", "__DEMO_EXPERIMENTAL__" }, languageVersion: LanguageVersion.CSharp8, kind: SourceCodeKind.Script);

        private static readonly SyntaxList<MemberDeclarationSyntax> InitHostSyntax = ((CompilationUnitSyntax)ParseSyntaxTree(
@"#line hidden
RoslynPad.Runtime.RuntimeInitializer.Initialize();
#line default
", _parseOptions).GetRoot()).Members;

        public static Lazy<string> CurrentPid { get; } = new Lazy<string>(() => Process.GetCurrentProcess().Id.ToString());

        private InitializationParameters _parameters;
        private ScriptOptions _scriptOptions;

        private CancellationTokenSource? _executeCts;
        private ExecutionPlatform? _platform;
        private string _assemblyPath;
        private string _depsFile;

        public ExecutionPlatform Platform
        {
            get => _platform ?? throw new InvalidOperationException("No platform selected");
            set
            {
                _platform = value;
                CleanupBuildPath();
            }
        }

        private readonly JsonSerializer _jsonSerializer;

        public string BuildPath { get; }
        public string Name { get; }

#pragma warning disable CS8618 // Non-nullable field is uninitialized.
        public AssemblyExecutionHost(InitializationParameters parameters, string buildPath, string name)
#pragma warning restore CS8618 // Non-nullable field is uninitialized.
        {
            _jsonSerializer = new JsonSerializer { TypeNameHandling = TypeNameHandling.Objects };

            BuildPath = buildPath;
            Name = name;

            Initialize(parameters);
            CreateRuntimeConfig();
        }

        private void CreateRuntimeConfig()
        {
            var config = new JObject(
                new JProperty("runtimeOptions", new JObject(
                    new JProperty("tfm", "netcoreapp2.2"),
                    new JProperty("framework", new JObject(
                        new JProperty("name", "Microsoft.NETCore.App"),
                        new JProperty("version", "2.2.0"))))));

            File.WriteAllText(Path.Combine(BuildPath, $"RoslynPad-{Name}.runtimeconfig.json"), config.ToString());

            var devConfig = new JObject(
                new JProperty("runtimeOptions", new JObject(
                    new JProperty("additionalProbingPaths", new JArray(
                        _parameters.GlobalPackageFolder)))));

            File.WriteAllText(Path.Combine(BuildPath, $"RoslynPad-{Name}.runtimeconfig.dev.json"), devConfig.ToString());
        }

        private void Initialize(InitializationParameters parameters)
        {
            _parameters = parameters;
            _scriptOptions = ScriptOptions.Default
                   .WithReferences(parameters.CompileReferences.Select(p => MetadataReference.CreateFromFile(p)))
                   .WithImports(parameters.Imports)
                   .WithMetadataResolver(new CachedScriptMetadataResolver(parameters.WorkingDirectory));
        }

        public event Action<IList<CompilationErrorResultObject>> CompilationErrors;
        public event Action<string> Disassembled;
        public event Action<ResultObject> Dumped;
        public event Action<ExceptionResultObject> Error;

        public Task CompileAndSave(string code, string assemblyPath, OptimizationLevel? optimizationLevel)
        {
            return Task.CompletedTask;
        }

        public void Dispose()
        {
        }

        private void CleanupBuildPath()
        {
            StopProcess();
            
            var files = IOUtilities.EnumerateFiles(BuildPath, "*.dll").Concat(
                        IOUtilities.EnumerateFiles(BuildPath, "*.exe")).Concat(
                        IOUtilities.EnumerateFiles(BuildPath, "*.deps.json")).Concat(
                        IOUtilities.EnumerateFiles(BuildPath, "*.config"));

            foreach (var file in files)
            {
                IOUtilities.PerformIO(() => File.Delete(file));
            }
        }

        public async Task ExecuteAsync(string code, bool disassemble, OptimizationLevel? optimizationLevel)
        {
            await new NoContextYieldAwaitable();

            using var executeCts = new CancellationTokenSource();
            var cancellationToken = executeCts.Token;

            var script = CreateScriptRunner(code, optimizationLevel);

            _assemblyPath = Path.Combine(BuildPath, $"RoslynPad-{Name}.{AssemblyExtension}");
            _depsFile = Path.ChangeExtension(_assemblyPath, ".deps.json");
            CopyIfNewer(Path.Combine(BuildPath, "project.assets.json"), _depsFile);

            CopyDependencies();

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

            try
            {
                _executeCts = executeCts;

                await StartProcess(_assemblyPath, cancellationToken);
            }
            finally
            {
                _executeCts = null;
            }
        }

        private void Disassemble()
        {
            using (var assembly = AssemblyDefinition.ReadAssembly(_assemblyPath))
            {
                var output = new PlainTextOutput();
                var disassembler = new ReflectionDisassembler(output, false, CancellationToken.None);
                disassembler.WriteModuleContents(assembly.MainModule);
                Disassembled?.Invoke(output.ToString());
            }
        }

        private string AssemblyExtension => Platform.IsCore ? "dll" : "exe";

        private void CopyDependencies()
        {
            CopyCommonAssembly();

            if (!Platform.IsDesktop)
            {
                return;
            }

            if (CopyReferences())
            {
                CreateAppConfig();
            }

            void CopyCommonAssembly()
            {
                var initAssemblySourcePath = typeof(RuntimeInitializer).Assembly.Location;
                var initAssemblyPath = Path.Combine(BuildPath, Path.GetFileName(initAssemblySourcePath));
                CopyIfNewer(initAssemblySourcePath, initAssemblyPath);
            }

            bool CopyReferences()
            {
                var copied = false;

                foreach (var file in _parameters.RuntimeReferences)
                {
                    if (CopyIfNewer(file, Path.Combine(BuildPath, Path.GetFileName(file))))
                    {
                        copied = true;
                    }
                }

                return copied;
            }

            void CreateAppConfig()
            {
                var runtime = new XElement("runtime");
                XNamespace ns = "urn:schemas-microsoft-com:asm.v1";

                foreach (var file in _parameters.RuntimeReferences)
                {
                    using (var assembly = Mono.Cecil.AssemblyDefinition.ReadAssembly(file))
                    {
                        var publicKeyToken = assembly.Name.PublicKeyToken;
                        var publicKeyTokenString = publicKeyToken == null || publicKeyToken.Length == 0
                            ? string.Empty
                            : string.Join("", publicKeyToken.Select(t => t.ToString("x2")));

                        var element = new XElement(ns + "assemblyBinding",
                            new XElement(ns + "dependentAssembly",
                                new XElement(ns + "assemblyIdentity",
                                    new XAttribute("name", assembly.Name.Name),
                                    new XAttribute("publicKeyToken", publicKeyTokenString),
                                    new XAttribute("culture", string.IsNullOrEmpty(assembly.Name.Culture) ? "neutral" : assembly.Name.Culture)),
                                new XElement(ns + "bindingRedirect",
                                    new XAttribute("oldVersion", "0.0.0.0-" + assembly.Name.Version),
                                    new XAttribute("newVersion", assembly.Name.Version))));

                        runtime.Add(element);
                    }
                }

                var doc = new XDocument(
                    new XElement("configuration",
                        new XElement(runtime)));

                doc.Save(Path.ChangeExtension(_assemblyPath, ".exe.config"));
            }
        }

        private static bool CopyIfNewer(string source, string destination)
        {
            var sourceInfo = new FileInfo(source);
            var destinationInfo = new FileInfo(destination);

            if (!destinationInfo.Exists || destinationInfo.CreationTimeUtc < sourceInfo.CreationTimeUtc)
            {
                sourceInfo.CopyTo(destination, overwrite: true);
                return true;
            }

            return false;
        }

        private ScriptRunner CreateScriptRunner(string code, OptimizationLevel? optimizationLevel)
        {
            return new ScriptRunner(code: null, ParseCode(code), _parseOptions,
                            OutputKind.ConsoleApplication, Microsoft.CodeAnalysis.Platform.AnyCpu,
                            _scriptOptions.MetadataReferences, _scriptOptions.Imports,
                            _scriptOptions.FilePath, _parameters.WorkingDirectory, _scriptOptions.MetadataResolver,
                            optimizationLevel: optimizationLevel ?? _parameters.OptimizationLevel,
                            checkOverflow: _parameters.CheckOverflow,
                            allowUnsafe: _parameters.AllowUnsafe);
        }

        private async Task StartProcess(string assemblyPath, CancellationToken cancellationToken)
        {
            using (var process = new Process
            {
                StartInfo = GetProcessStartInfo(assemblyPath)
            })
            using (cancellationToken.Register(() =>
            {
                try { process.Kill(); } catch { }
            }))
            {
                if (process.Start())
                {
                    await Task.WhenAll(
                        Task.Run(() => ReadObjectProcessStream(process.StandardOutput)),
                        Task.Run(() => ReadProcessStream(process.StandardError)));
                }
            }
        }

        private ProcessStartInfo GetProcessStartInfo(string assemblyPath)
        {
            return new ProcessStartInfo
            {
                FileName = Platform.IsCore ? Platform.HostPath : assemblyPath,
                Arguments = $"\"{assemblyPath}\" --pid {CurrentPid.Value}",
                WorkingDirectory = BuildPath,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
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
            using (var jsonReader = new JsonTextReader(reader) { SupportMultipleContent = true })
            {
                while (!reader.EndOfStream && jsonReader.Read())
                {
                    try
                    {
                        var result = _jsonSerializer.Deserialize<ResultObject>(jsonReader);
                        if (result is ExceptionResultObject exceptionResult)
                        {
                            Error?.Invoke(exceptionResult);
                        }
                        else
                        {
                            Dumped?.Invoke(result);
                        }
                    }
                    catch (Exception ex)
                    {
                        Dumped?.Invoke(ResultObject.Create("Error deserializing result: " + ex.Message, DumpQuotas.Default));
                    }
                }
            }
        }

        private SyntaxTree ParseCode(string code)
        {
            var tree = ParseSyntaxTree(code, _parseOptions);
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

                // add host initialization code
                members = members.InsertRange(0, InitHostSyntax);

                root = c.WithMembers(members);
            }

            return tree.WithRootAndOptions(root, _parseOptions);
        }

        private void SendDiagnostics(ImmutableArray<Diagnostic> diagnostics)
        {
            if (diagnostics.Length > 0)
            {
                CompilationErrors?.Invoke(diagnostics.Select(d => GetCompilationErrorResultObject(d)).ToImmutableArray());
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

        public Task ResetAsync()
        {
            StopProcess();
            return Task.CompletedTask;
        }

        private void StopProcess()
        {
            _executeCts?.Cancel();
        }

        public Task Update(InitializationParameters parameters)
        {
            Initialize(parameters);
            return Task.CompletedTask;
        }
    }
}