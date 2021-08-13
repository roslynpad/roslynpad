using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Scripting;

namespace RoslynPad.Roslyn.Scripting
{
    /// <summary>
    /// Provides an alternative to the <see cref="Script"/> class that also emits PDBs.
    /// </summary>
    public sealed class ScriptRunner
    {
        private readonly OptimizationLevel _optimizationLevel;
        private readonly bool _checkOverflow;
        private readonly bool _allowUnsafe;

        public ScriptRunner(string? code, ImmutableList<SyntaxTree>? syntaxTrees = null, CSharpParseOptions? parseOptions = null, OutputKind outputKind = OutputKind.DynamicallyLinkedLibrary,
            Platform platform = Platform.AnyCpu, IEnumerable<MetadataReference>? references = null,
            IEnumerable<string>? usings = null, string? filePath = null, string? workingDirectory = null,
            SourceReferenceResolver? sourceResolver = null,
            OptimizationLevel optimizationLevel = OptimizationLevel.Debug, bool checkOverflow = false, bool allowUnsafe = true)
        {
            _optimizationLevel = optimizationLevel;
            _checkOverflow = checkOverflow;
            _allowUnsafe = allowUnsafe;
            Code = code;
            SyntaxTrees = syntaxTrees;
            OutputKind = outputKind;
            Platform = platform;
            ParseOptions = (parseOptions ?? new CSharpParseOptions())
                               .WithKind(SourceCodeKind.Script)
                               .WithPreprocessorSymbols(RoslynHost.PreprocessorSymbols);
            References = references?.AsImmutable() ?? ImmutableArray<MetadataReference>.Empty;
            Usings = usings?.AsImmutable() ?? ImmutableArray<string>.Empty;
            FilePath = filePath ?? string.Empty;
            SourceResolver = sourceResolver ??
                             (workingDirectory != null
                                 ? new SourceFileResolver(ImmutableArray<string>.Empty, workingDirectory)
                                 : SourceFileResolver.Default);
        }

        public string? Code { get; }
        public ImmutableList<SyntaxTree>? SyntaxTrees { get; }
        public OutputKind OutputKind { get; }
        public Platform Platform { get; }
        public ImmutableArray<MetadataReference> References { get; }
        public SourceReferenceResolver SourceResolver { get; }
        public ImmutableArray<string> Usings { get; }
        public string FilePath { get; }
        public CSharpParseOptions ParseOptions { get; }

        public async Task<ImmutableArray<Diagnostic>> CompileAndSaveAssembly(string assemblyPath, CancellationToken cancellationToken = default)
        {
            var compilation = GetCompilationFromCode(Path.GetFileNameWithoutExtension(assemblyPath));

            var diagnostics = compilation.GetParseDiagnostics(cancellationToken);
            if (!diagnostics.IsEmpty)
            {
                return diagnostics;
            }

            var diagnosticsBag = new DiagnosticBag();
            await SaveAssembly(assemblyPath, compilation, diagnosticsBag, cancellationToken).ConfigureAwait(false);
            return GetDiagnostics(diagnosticsBag, includeWarnings: true);
        }

        private static async Task SaveAssembly(string assemblyPath, Compilation compilation, DiagnosticBag diagnostics, CancellationToken cancellationToken)
        {
            using var peStream = new MemoryStream();
            using var pdbStream = new MemoryStream();
            var emitResult = compilation.Emit(
                peStream: peStream,
                pdbStream: pdbStream,
                cancellationToken: cancellationToken);

            diagnostics.AddRange(emitResult.Diagnostics);

            if (emitResult.Success)
            {
                peStream.Position = 0;
                pdbStream.Position = 0;

                await CopyToFileAsync(assemblyPath, peStream).ConfigureAwait(false);
                await CopyToFileAsync(Path.ChangeExtension(assemblyPath, "pdb"), pdbStream).ConfigureAwait(false);
            }
        }

        private static async Task CopyToFileAsync(string path, Stream stream)
        {
            using var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous);
            await stream.CopyToAsync(fileStream).ConfigureAwait(false);
        }

        private Compilation GetCompilationFromCode(string assemblyName)
        {
            var trees = SyntaxTrees;
            if (trees == null)
            {
                if (Code == null) throw new InvalidOperationException($"Either specify {nameof(Code)} or {nameof(SyntaxTrees)}");

                trees = ImmutableList.Create(SyntaxFactory.ParseSyntaxTree(Code, ParseOptions, FilePath));
            }

            var compilationOptions = new CSharpCompilationOptions(
                OutputKind,
                mainTypeName: null,
                scriptClassName: "Program",
                usings: Usings,
                optimizationLevel: _optimizationLevel,
                checkOverflow: _checkOverflow,
                allowUnsafe: _allowUnsafe,
                platform: Platform,
                warningLevel: 4,
                deterministic: true,
                xmlReferenceResolver: null,
                sourceReferenceResolver: SourceResolver,
                assemblyIdentityComparer: AssemblyIdentityComparer.Default,
                nullableContextOptions: NullableContextOptions.Enable
            );

            return CSharpCompilation.Create(
                 assemblyName,
                 trees,
                 References,
                 compilationOptions);
        }

        private static ImmutableArray<Diagnostic> GetDiagnostics(DiagnosticBag diagnostics, bool includeWarnings)
        {
            if (diagnostics.IsEmptyWithoutResolution)
            {
                return ImmutableArray<Diagnostic>.Empty;
            }

            return diagnostics.AsEnumerable().Where(d =>
                d.Severity == DiagnosticSeverity.Error || (includeWarnings && d.Severity == DiagnosticSeverity.Warning)).AsImmutable();
        }

        private class DiagnosticBag
        {
            private ConcurrentQueue<Diagnostic>? _lazyBag;

            public bool IsEmptyWithoutResolution
            {
                get
                {
                    var bag = _lazyBag;
                    return bag == null || bag.IsEmpty;
                }
            }

            private ConcurrentQueue<Diagnostic> Bag
            {
                get
                {
                    var bag = _lazyBag;
                    if (bag != null)
                    {
                        return bag;
                    }

                    var newBag = new ConcurrentQueue<Diagnostic>();
                    return Interlocked.CompareExchange(ref _lazyBag, newBag, null) ?? newBag;
                }
            }

            public void AddRange<T>(ImmutableArray<T> diagnostics) where T : Diagnostic
            {
                if (!diagnostics.IsDefaultOrEmpty)
                {
                    var bag = Bag;
                    foreach (var t in diagnostics)
                    {
                        bag.Enqueue(t);
                    }
                }
            }

            public IEnumerable<Diagnostic> AsEnumerable()
            {
                var bag = Bag;

                var foundVoid = bag.Any(diagnostic => diagnostic.Severity == DiagnosticSeverityVoid);

                return foundVoid
                    ? AsEnumerableFiltered()
                    : bag;
            }

            internal void Clear()
            {
                var bag = _lazyBag;
                if (bag != null)
                {
                    _lazyBag = null;
                }
            }

            private static DiagnosticSeverity DiagnosticSeverityVoid => ~DiagnosticSeverity.Info;

            private IEnumerable<Diagnostic> AsEnumerableFiltered()
            {
                return Bag.Where(diagnostic => diagnostic.Severity != DiagnosticSeverityVoid);
            }
        }
    }
}
