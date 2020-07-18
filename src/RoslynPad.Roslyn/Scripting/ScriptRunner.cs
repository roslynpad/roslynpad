using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Scripting.Hosting;

namespace RoslynPad.Roslyn.Scripting
{
    /// <summary>
    /// Provides an alternative to the <see cref="Script"/> class that also emits PDBs.
    /// </summary>
    public sealed class ScriptRunner
    {
        private static readonly string _globalAssemblyNamePrefix = "\u211B\u2118-" + Guid.NewGuid() + "-";
        private static int _assemblyNumber;

        private readonly InteractiveAssemblyLoader _assemblyLoader;
        private readonly OptimizationLevel _optimizationLevel;
        private readonly bool _checkOverflow;
        private readonly bool _allowUnsafe;
        private readonly bool _registerDependencies;

        private Func<object[], Task<object>>? _lazyExecutor;
        private Compilation? _lazyCompilation;

        public ScriptRunner(string? code, ImmutableList<SyntaxTree>? syntaxTrees = null, CSharpParseOptions? parseOptions = null, OutputKind outputKind = OutputKind.DynamicallyLinkedLibrary,
            Platform platform = Platform.AnyCpu, IEnumerable<MetadataReference>? references = null,
            IEnumerable<string>? usings = null, string? filePath = null, string? workingDirectory = null,
            MetadataReferenceResolver? metadataResolver = null, SourceReferenceResolver? sourceResolver = null,
            InteractiveAssemblyLoader? assemblyLoader = null,
            OptimizationLevel optimizationLevel = OptimizationLevel.Debug, bool checkOverflow = false, bool allowUnsafe = true,
            bool registerDependencies = false)
        {
            _optimizationLevel = optimizationLevel;
            _checkOverflow = checkOverflow;
            _allowUnsafe = allowUnsafe;
            _registerDependencies = registerDependencies;
            Code = code;
            SyntaxTrees = syntaxTrees;
            OutputKind = outputKind;
            Platform = platform;
            _assemblyLoader = assemblyLoader ?? new InteractiveAssemblyLoader();
            ParseOptions = (parseOptions ?? new CSharpParseOptions())
                               .WithKind(SourceCodeKind.Script)
                               .WithPreprocessorSymbols(RoslynHost.PreprocessorSymbols);
            References = references?.AsImmutable() ?? ImmutableArray<MetadataReference>.Empty;
            Usings = usings?.AsImmutable() ?? ImmutableArray<string>.Empty;
            FilePath = filePath ?? string.Empty;
            MetadataResolver = metadataResolver ?? ScriptMetadataResolver.Default;
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

        public MetadataReferenceResolver MetadataResolver { get; }

        public SourceReferenceResolver SourceResolver { get; }

        public ImmutableArray<string> Usings { get; }

        public string FilePath { get; }

        public CSharpParseOptions ParseOptions { get; }

        public ImmutableArray<Diagnostic> Compile(Action<Stream>? peStreamAction, CancellationToken cancellationToken = default)
        {
            try
            {
                GetExecutor(peStreamAction, cancellationToken);

                return ImmutableArray.CreateRange(GetCompilation(GetScriptAssemblyName()).GetDiagnostics(cancellationToken).Where(d => d.Severity == DiagnosticSeverity.Warning));
            }
            catch (CompilationErrorException e)
            {
                return ImmutableArray.CreateRange(e.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error || d.Severity == DiagnosticSeverity.Warning));
            }
        }

        public async Task<object?> RunAsync(CancellationToken cancellationToken = default)
        {
            var entryPoint = GetExecutor(null, cancellationToken);
            if (entryPoint == null)
            {
                return null;
            }

            var result = await entryPoint(new object[2]).ConfigureAwait(false);

            return result;
        }

        public async Task<ImmutableArray<Diagnostic>> SaveAssembly(string assemblyPath, CancellationToken cancellationToken = default)
        {
            var compilation = GetCompilation(Path.GetFileNameWithoutExtension(assemblyPath));

            var diagnostics = compilation.GetParseDiagnostics(cancellationToken);
            if (!diagnostics.IsEmpty)
            {
                return diagnostics;
            }

            var diagnosticsBag = new DiagnosticBag();
            await SaveAssembly(assemblyPath, compilation, diagnosticsBag, cancellationToken).ConfigureAwait(false);
            return GetDiagnostics(diagnosticsBag, includeWarnings: true);
        }

        private Func<object[], Task<object>>? GetExecutor(Action<Stream>? peStreamAction, CancellationToken cancellationToken)
        {
            if (_lazyExecutor == null)
            {
                Interlocked.CompareExchange(ref _lazyExecutor, CreateExecutor(peStreamAction, cancellationToken), null);
            }

            return _lazyExecutor;
        }

        private static string GetScriptAssemblyName() => _globalAssemblyNamePrefix + Interlocked.Increment(ref _assemblyNumber);

        private Func<object[], Task<object>>? CreateExecutor(Action<Stream>? peStreamAction, CancellationToken cancellationToken)
        {
            var compilation = GetCompilation(GetScriptAssemblyName());

            var diagnosticFormatter = CSharpDiagnosticFormatter.Instance;
            var diagnostics = DiagnoseCompilation(compilation, diagnosticFormatter);

            var entryPoint = Build(peStreamAction, compilation, diagnostics, cancellationToken);
            ThrowIfAnyCompilationErrors(diagnostics, diagnosticFormatter);
            return entryPoint;
        }

        private static DiagnosticBag DiagnoseCompilation(Compilation compilation, DiagnosticFormatter diagnosticFormatter)
        {
            var diagnostics = new DiagnosticBag();
            diagnostics.AddRange(compilation.GetParseDiagnostics());
            ThrowIfAnyCompilationErrors(diagnostics, diagnosticFormatter);
            diagnostics.Clear();
            return diagnostics;
        }

        private static async Task SaveAssembly(string assemblyPath, Compilation compilation, DiagnosticBag diagnostics, CancellationToken cancellationToken)
        {
            using (var peStream = new MemoryStream())
            using (var pdbStream = new MemoryStream())
            {
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
        }

        private static async Task CopyToFileAsync(string path, Stream stream)
        {
            using (var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous))
            {
                await stream.CopyToAsync(fileStream).ConfigureAwait(false);
            }
        }

        private Func<object[], Task<object>>? Build(Action<Stream>? peStreamAction, Compilation compilation, DiagnosticBag diagnostics, CancellationToken cancellationToken)
        {
            var entryPoint = compilation.GetEntryPoint(cancellationToken);
            if (entryPoint == null)
            {
                return null;
            }

            using (var peStream = new MemoryStream())
            using (var pdbStream = new MemoryStream())
            {
                var emitResult = compilation.Emit(
                    peStream: peStream,
                    pdbStream: pdbStream,
                    cancellationToken: cancellationToken);

                diagnostics.AddRange(emitResult.Diagnostics);

                if (!emitResult.Success)
                {
                    return null;
                }

                if (_registerDependencies)
                {
                    foreach (var referencedAssembly in compilation.References.Select(
                        x => new { Key = x, Value = compilation.GetAssemblyOrModuleSymbol(x) as IAssemblySymbol }))
                    {
                        if (referencedAssembly.Value == null) continue;

                        var path = (referencedAssembly.Key as PortableExecutableReference)?.FilePath;
                        if (path == null) continue;

                        _assemblyLoader.RegisterDependency(referencedAssembly.Value.Identity, path);
                    }
                }

                peStream.Position = 0;
                pdbStream.Position = 0;

                var assembly = _assemblyLoader.LoadAssemblyFromStream(peStream, pdbStream);
                var runtimeEntryPoint = GetEntryPointRuntimeMethod(entryPoint, assembly);

                if (peStreamAction != null)
                {
                    peStream.Position = 0;
                    peStreamAction(peStream);
                }

                return (Func<object[], Task<object>>)runtimeEntryPoint.CreateDelegate(typeof(Func<object[], Task<object>>));
            }
        }

        private static MethodInfo GetEntryPointRuntimeMethod(IMethodSymbol entryPoint, Assembly assembly)
        {
            var entryPointTypeName = BuildQualifiedName(entryPoint.ContainingNamespace.MetadataName, entryPoint.ContainingType.MetadataName);
            var entryPointMethodName = entryPoint.MetadataName;

            var entryPointType = assembly.GetType(entryPointTypeName, throwOnError: true, ignoreCase: false);
            return entryPointType.GetTypeInfo().GetDeclaredMethod(entryPointMethodName);
        }

        private static string BuildQualifiedName(
            string qualifier,
            string name)
        {
            return !string.IsNullOrEmpty(qualifier) ? string.Concat(qualifier, ".", name) : name;
        }

        private Compilation GetCompilation(string assemblyName)
        {
            if (_lazyCompilation == null)
            {
                var compilation = GetCompilationFromCode(assemblyName);
                Interlocked.CompareExchange(ref _lazyCompilation, compilation, null);
            }

            return _lazyCompilation!;
        }

        private Compilation GetCompilationFromCode(string assemblyName)
        {
            var trees = SyntaxTrees;
            if (trees == null)
            {
                if (Code == null) throw new InvalidOperationException($"Either specify {nameof(Code)} or {nameof(SyntaxTrees)}");

                trees = ImmutableList.Create(SyntaxFactory.ParseSyntaxTree(Code, ParseOptions, FilePath));
            }

            var references = GetReferences();

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
                metadataReferenceResolver: MetadataResolver,
                assemblyIdentityComparer: AssemblyIdentityComparer.Default,
                nullableContextOptions: NullableContextOptions.Enable
            );
            //.WithTopLevelBinderFlags(BinderFlags.IgnoreCorLibraryDuplicatedTypes),

            return CSharpCompilation.Create(
                 assemblyName,
                 trees,
                 references,
                 compilationOptions);
        }

        private IEnumerable<MetadataReference> GetReferences()
        {
            var references = ImmutableList.CreateBuilder<MetadataReference>();
            foreach (var reference in References)
            {
                if (reference is UnresolvedMetadataReference unresolved)
                {
                    var resolved = MetadataResolver.ResolveReference(unresolved.Reference, null, unresolved.Properties);
                    if (!resolved.IsDefault)
                    {
                        references.AddRange(resolved);
                    }
                }
                else
                {
                    references.Add(reference);
                }
            }
            return references.ToImmutable();
        }

        private static void ThrowIfAnyCompilationErrors(DiagnosticBag diagnostics, DiagnosticFormatter formatter)
        {
            var filtered = GetDiagnostics(diagnostics, includeWarnings: false);
            if (!filtered.IsEmpty)
            {
                throw new CompilationErrorException(
                  formatter.Format(filtered[0], CultureInfo.CurrentCulture),
                  filtered);
            }
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
