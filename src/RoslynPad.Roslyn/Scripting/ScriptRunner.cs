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
        private readonly OptimizationLevel _optimizationLevel;
        private readonly bool _checkOverflow;
        private readonly bool _allowUnsafe;

        public ScriptRunner(string code, CSharpParseOptions parseOptions = null, OutputKind outputKind = OutputKind.DynamicallyLinkedLibrary,
            Platform platform = Platform.AnyCpu, IEnumerable<MetadataReference> references = null,
            IEnumerable<string> usings = null, string filePath = null, string workingDirectory = null, 
            MetadataReferenceResolver metadataResolver = null, SourceReferenceResolver sourceResolver = null,
            InteractiveAssemblyLoader assemblyLoader = null, 
            OptimizationLevel optimizationLevel = OptimizationLevel.Debug, bool checkOverflow = false, bool allowUnsafe = true)
        {
            _optimizationLevel = optimizationLevel;
            _checkOverflow = checkOverflow;
            _allowUnsafe = allowUnsafe;
            Code = code;
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

        public string Code { get; }

        public OutputKind OutputKind { get; }
        public Platform Platform { get; }

        public ImmutableArray<MetadataReference> References { get; }

        public MetadataReferenceResolver MetadataResolver { get; }

        public SourceReferenceResolver SourceResolver { get; }

        public ImmutableArray<string> Usings { get; }

        public string FilePath { get; }

        public CSharpParseOptions ParseOptions { get; }

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
            if (entryPoint == null)
            {
                return null;
            }

            var result = await entryPoint(new object[2]).ConfigureAwait(false);

            return result;
        }

        public async Task<ImmutableArray<Diagnostic>> SaveAssembly(string assemblyPath, CancellationToken cancellationToken = default(CancellationToken))
        {
            var compilation = GetCompilation().WithAssemblyName(Path.GetFileNameWithoutExtension(assemblyPath));

            var diagnostics = compilation.GetParseDiagnostics(cancellationToken);
            if (!diagnostics.IsEmpty)
            {
                return diagnostics;
            }

            var diagnosticsBag = new DiagnosticBag();
            await SaveAssembly(assemblyPath, compilation, diagnosticsBag, cancellationToken).ConfigureAwait(false);
            return GetDiagnostics(diagnosticsBag);
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
            var diagnostics = DiagnoseCompilation(compilation, diagnosticFormatter);

            var entryPoint = Build(compilation, diagnostics, cancellationToken);
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
                    x => new { Key = x, Value = compilation.GetAssemblyOrModuleSymbol(x) as IAssemblySymbol }))
                {
                    if (referencedAssembly.Value == null) continue;
                    
                    var path = (referencedAssembly.Key as PortableExecutableReference)?.FilePath;
                    if (path == null) continue;

                    _assemblyLoader.RegisterDependency(referencedAssembly.Value.Identity, path);
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

        // TODO:
        //public bool HasSubmissionResult => GetCompilation().HasSubmissionResult;

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
                xmlReferenceResolver: null,
                sourceReferenceResolver: SourceResolver,
                metadataReferenceResolver: MetadataResolver,
                assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default
            );
            //.WithTopLevelBinderFlags(BinderFlags.IgnoreCorLibraryDuplicatedTypes),

            if (OutputKind == OutputKind.ConsoleApplication || OutputKind == OutputKind.WindowsApplication)
            {
                return CSharpCompilation.Create(
                 _globalAssemblyNamePrefix,
                 new[] { tree },
                 references,
                 compilationOptions);
            }

            return CSharpCompilation.CreateScriptCompilation(
                    _globalAssemblyNamePrefix,
                    tree,
                    references,
                    compilationOptions,
                    returnType: typeof(object));
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
            var filtered = GetDiagnostics(diagnostics);
            if (!filtered.IsEmpty)
            {
                throw new CompilationErrorException(
                  formatter.Format(filtered[0], CultureInfo.CurrentCulture),
                  filtered);
            }
        }

        private static ImmutableArray<Diagnostic> GetDiagnostics(DiagnosticBag diagnostics)
        {
            if (diagnostics.IsEmptyWithoutResolution)
            {
                return ImmutableArray<Diagnostic>.Empty;
            }
            return diagnostics.AsEnumerable().Where(d => d.Severity == DiagnosticSeverity.Error).AsImmutable();
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