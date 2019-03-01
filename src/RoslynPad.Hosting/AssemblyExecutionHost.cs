using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Scripting;
using Newtonsoft.Json;
using RoslynPad.Roslyn;
using RoslynPad.Roslyn.Scripting;
using RoslynPad.Runtime;
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

        private InitializationParameters _parameters;
        private ScriptOptions _scriptOptions;

        private CancellationTokenSource? _executeCts;

        public string? HostArguments { get; set; }
        public string? HostPath { get; set; }
        public int CurrentPid { get; }

#pragma warning disable CS8618 // Non-nullable field is uninitialized.
        public AssemblyExecutionHost(InitializationParameters parameters)
#pragma warning restore CS8618 // Non-nullable field is uninitialized.
        {
            CurrentPid = Process.GetCurrentProcess().Id;
            Initialize(parameters);
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
        public event Action<IList<ResultObject>> Dumped;
        public event Action<ExceptionResultObject> Error;

        public Task CompileAndSave(string code, string assemblyPath, OptimizationLevel? optimizationLevel)
        {
            return Task.CompletedTask;
        }

        public void Dispose()
        {
        }

        public async Task ExecuteAsync(string code, bool disassemble, OptimizationLevel? optimizationLevel)
        {
            await Task.Run(() => { });

            using var executeCts = new CancellationTokenSource();
            var cancellationToken = executeCts.Token;
            _executeCts = executeCts;

            var script = new ScriptRunner(code: null, ParseCode(code), _parseOptions, OutputKind.ConsoleApplication, Platform.AnyCpu,
                _scriptOptions.MetadataReferences, _scriptOptions.Imports,
                _scriptOptions.FilePath, _parameters.WorkingDirectory, _scriptOptions.MetadataResolver,
                optimizationLevel: optimizationLevel ?? _parameters.OptimizationLevel,
                checkOverflow: _parameters.CheckOverflow,
                allowUnsafe: _parameters.AllowUnsafe);

            var path = Path.GetTempPath();

            var initAssemblySourcePath = typeof(RuntimeInitializer).Assembly.Location;
            var initAssemblyPath = Path.Combine(path, Path.GetFileName(initAssemblySourcePath));
            if (!File.Exists(initAssemblyPath))
            {
                File.Copy(initAssemblySourcePath, initAssemblyPath);
            }

            var assemblyPath = Path.Combine(path, "RP-" + Guid.NewGuid().ToString() + ".exe");

            var diagnostics = await script.SaveAssembly(assemblyPath, cancellationToken).ConfigureAwait(false);
            SendDiagnostics(diagnostics);

            if (diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
            {
                return;
            }

            if (disassemble)
            {
                Disassembled?.Invoke(string.Empty);
            }

            try
            {
                using (var process = new Process
                {
                    StartInfo = new ProcessStartInfo(assemblyPath)
                    {
                        Arguments = CurrentPid.ToString(),
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                    }
                })
                {
                    using var registration = cancellationToken.Register(() =>
                    {
                        try { process.Kill(); } catch { }
                    });

                    if (process.Start())
                    {
                        await Task.Run(() => ReadProcessStream(process.StandardOutput, cancellationToken), cancellationToken);
                    }
                }
            }
            finally
            {
                _executeCts = null;
            }
        }

        private void ReadProcessStream(StreamReader reader, CancellationToken cancellationToken)
        {
            var jsonSerializer = new JsonSerializer { TypeNameHandling = TypeNameHandling.Objects };
            using (var jsonReader = new JsonTextReader(reader) { SupportMultipleContent = true })
            {
                while (jsonReader.Read() && !cancellationToken.IsCancellationRequested)
                {
                    var result = jsonSerializer.Deserialize<ResultObject>(jsonReader);
                    if (result is ExceptionResultObject exceptionResult)
                    {
                        Error?.Invoke(exceptionResult);
                    }
                    else
                    {
                        Dumped?.Invoke(new[] { result });
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

        private void SendError(Exception ex)
        {
            Error?.Invoke(ExceptionResultObject.Create(ex));
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
            _executeCts?.Cancel();
            return Task.CompletedTask;
        }

        public Task Update(InitializationParameters parameters)
        {
            Initialize(parameters);
            return Task.CompletedTask;
        }
    }
}