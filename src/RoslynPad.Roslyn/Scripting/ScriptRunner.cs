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

namespace RoslynPad.Roslyn.Scripting
{
    /// <summary>
    /// Provides an alternative to the <see cref="Script"/> class that also emits PDBs.
    /// </summary>
    public sealed class ScriptRunner
    {
        private static readonly string _globalAssemblyNamePrefix = "\u211B\u2118*" + Guid.NewGuid();

        private readonly InteractiveAssemblyLoader _assemblyLoader;
        private Func<object[], Task<object>> _lazyExecutor;
        private Compilation _lazyCompilation;

        public ScriptRunner(string code, CSharpParseOptions parseOptions = null, IEnumerable<MetadataReference> references = null, IEnumerable<string> usings = null, string filePath = null, MetadataReferenceResolver metadataResolver = null)
        {
            Code = code;
            _assemblyLoader = new InteractiveAssemblyLoader();
            ParseOptions = (parseOptions ?? new CSharpParseOptions())
                               .WithKind(SourceCodeKind.Script)
                               .WithPreprocessorSymbols("__DEMO__", "__DEMO_EXPERIMENTAL__");
            References = references?.AsImmutable() ?? ImmutableArray<MetadataReference>.Empty;
            Usings = usings?.AsImmutable() ?? ImmutableArray<string>.Empty;
            FilePath = filePath ?? string.Empty;
            MetadataResolver = metadataResolver;
        }

        public string Code { get; }

        public ImmutableArray<MetadataReference> References { get; }

        public MetadataReferenceResolver MetadataResolver { get; }

        public ImmutableArray<string> Usings { get; }

        public string FilePath { get; set; }

        public CSharpParseOptions ParseOptions { get; set; }

        public ImmutableArray<Diagnostic> Compile(CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                GetExecutor(cancellationToken);

                return ImmutableArray.CreateRange(GetCompilation().GetDiagnostics(cancellationToken).Where(d => d.Severity == DiagnosticSeverity.Warning));
            }
            catch (CompilationErrorException e)
            {
                return ImmutableArray.CreateRange(e.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error || d.Severity == DiagnosticSeverity.Warning));
            }
        }

        public async Task<object> RunAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            var entryPoint = GetExecutor(cancellationToken);

            var result = await entryPoint(new object[2]).ConfigureAwait(false);

            return result;
        }

        private Func<object[], Task<object>> GetExecutor(CancellationToken cancellationToken)
        {
            if (_lazyExecutor == null)
            {
                Interlocked.CompareExchange(ref _lazyExecutor, CreateExecutor(cancellationToken), null);
            }

            return _lazyExecutor;
        }

        private Func<object[], Task<object>> CreateExecutor(CancellationToken cancellationToken)
        {
            var compilation = GetCompilation();

            var diagnosticFormatter = CSharpDiagnosticFormatter.Instance;

            var diagnostics = new DiagnosticBag();
            diagnostics.AddRange(compilation.GetParseDiagnostics());
            ThrowIfAnyCompilationErrors(diagnostics, diagnosticFormatter);
            diagnostics.Clear();

            var entryPoint = Build(compilation, diagnostics, cancellationToken);
            ThrowIfAnyCompilationErrors(diagnostics, diagnosticFormatter);
            return entryPoint;
        }

        private Func<object[], Task<object>> Build(Compilation compilation, DiagnosticBag diagnostics, CancellationToken cancellationToken)
        {
            var entryPoint = compilation.GetEntryPoint(cancellationToken);

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

                foreach (var referencedAssembly in compilation.References.Select(
                    x => new { Key = x, Value = compilation.GetAssemblyOrModuleSymbol(x) }))
                {
                    var path = (referencedAssembly.Key as PortableExecutableReference)?.FilePath;
                    if (path != null)
                    {
                        _assemblyLoader.RegisterDependency(((IAssemblySymbol)referencedAssembly.Value).Identity, path);
                    }
                }

                peStream.Position = 0;
                pdbStream.Position = 0;

                var assembly = _assemblyLoader.LoadAssemblyFromStream(peStream, pdbStream);
                var runtimeEntryPoint = GetEntryPointRuntimeMethod(entryPoint, assembly);

                return (Func<object[], Task<object>>)runtimeEntryPoint.CreateDelegate(typeof(Func<object[], Task<object>>));
            }
        }

        private static MethodInfo GetEntryPointRuntimeMethod(IMethodSymbol entryPoint, Assembly assembly)
        {
            var entryPointTypeName = BuildQualifiedName(entryPoint.ContainingNamespace.MetadataName, entryPoint.ContainingType.MetadataName);
            var entryPointMethodName = entryPoint.MetadataName;

            var entryPointType = assembly.GetType(entryPointTypeName, throwOnError: true, ignoreCase: false).GetTypeInfo();
            return entryPointType.GetDeclaredMethod(entryPointMethodName);
        }

        private static string BuildQualifiedName(
            string qualifier,
            string name)
        {
            return !string.IsNullOrEmpty(qualifier) ? string.Concat(qualifier, ".", name) : name;
        }

        private Compilation GetCompilation()
        {
            if (_lazyCompilation == null)
            {
                var compilation = GetCompilationFromCode(Code);
                Interlocked.CompareExchange(ref _lazyCompilation, compilation, null);
            }

            return _lazyCompilation;
        }

        private Compilation GetCompilationFromCode(string code)
        {
            var tree = SyntaxFactory.ParseSyntaxTree(code, ParseOptions, FilePath);

            var references = GetReferences();

            var compilation = CSharpCompilation.CreateScriptCompilation(
                _globalAssemblyNamePrefix,
                tree,
                references,
                new CSharpCompilationOptions(
                    outputKind: OutputKind.DynamicallyLinkedLibrary,
                    mainTypeName: null,
                    scriptClassName: "Program",
                    usings: Usings,
                    optimizationLevel: OptimizationLevel.Debug, // TODO
                    checkOverflow: false,                       // TODO
                    allowUnsafe: true,                          // TODO
                    platform: Platform.AnyCpu,
                    warningLevel: 4,
                    xmlReferenceResolver: null,
                    sourceReferenceResolver: null,
                    metadataReferenceResolver: MetadataResolver,
                    assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default
                ), //.WithTopLevelBinderFlags(BinderFlags.IgnoreCorLibraryDuplicatedTypes),
                null,
                typeof(object));

            return compilation;
        }

        private IEnumerable<MetadataReference> GetReferences()
        {
            var references = ImmutableList.CreateBuilder<MetadataReference>();
            foreach (var reference in References)
            {
                var unresolved = reference as UnresolvedMetadataReference;
                if (unresolved != null)
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
            if (diagnostics.IsEmptyWithoutResolution)
            {
                return;
            }
            var filtered = diagnostics.AsEnumerable().Where(d => d.Severity == DiagnosticSeverity.Error).AsImmutable();
            if (filtered.IsEmpty)
            {
                return;
            }
            throw new CompilationErrorException(
                formatter.Format(filtered[0], CultureInfo.CurrentCulture),
                filtered);
        }

        private class DiagnosticBag
        {
            private ConcurrentQueue<Diagnostic> _lazyBag;

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