using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using RoslynPad.Roslyn.Diagnostics;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition.Hosting;
using System.Linq;
using System.Reflection;
using System.Threading;
using AnalyzerReference = Microsoft.CodeAnalysis.Diagnostics.AnalyzerReference;
using AnalyzerFileReference = Microsoft.CodeAnalysis.Diagnostics.AnalyzerFileReference;

namespace RoslynPad.Roslyn
{
    public class RoslynHost : IRoslynHost
    {
        #region Fields

        internal static readonly ImmutableArray<string> PreprocessorSymbols =
            ImmutableArray.CreateRange(new[] { "TRACE", "DEBUG" });

        internal static readonly ImmutableArray<Assembly> DefaultCompositionAssemblies =
            ImmutableArray.Create(
                // Microsoft.CodeAnalysis.Workspaces
                typeof(WorkspacesResources).Assembly,
                // Microsoft.CodeAnalysis.CSharp.Workspaces
                typeof(CSharpWorkspaceResources).Assembly,
                // Microsoft.CodeAnalysis.Features
                typeof(FeaturesResources).Assembly,
                // Microsoft.CodeAnalysis.CSharp.Features
                typeof(CSharpFeaturesResources).Assembly,
                // RoslynPad.Roslyn
                typeof(RoslynHost).Assembly);

        private readonly ConcurrentDictionary<DocumentId, RoslynWorkspace> _workspaces;
        private readonly ConcurrentDictionary<DocumentId, Action<DiagnosticsUpdatedArgs>> _diagnosticsUpdatedNotifiers;
        private readonly IDocumentationProviderService _documentationProviderService;
        private readonly CompositionHost _compositionContext;
        private int _documentNumber;

        public ParseOptions ParseOptions { get; }

        public HostServices HostServices { get; }

        public ImmutableArray<MetadataReference> DefaultReferences { get; }

        public ImmutableArray<string> DefaultImports { get; }

        public ImmutableArray<string> DisabledDiagnostics { get; }

        #endregion

        #region Constructors

        public RoslynHost(IEnumerable<Assembly>? additionalAssemblies = null,
            RoslynHostReferences? references = null,
            ImmutableArray<string>? disabledDiagnostics = null)
        {
            if (references == null) references = RoslynHostReferences.Empty;

            _workspaces = new ConcurrentDictionary<DocumentId, RoslynWorkspace>();
            _diagnosticsUpdatedNotifiers = new ConcurrentDictionary<DocumentId, Action<DiagnosticsUpdatedArgs>>();

            // ReSharper disable once VirtualMemberCallInConstructor
            var assemblies = GetDefaultCompositionAssemblies();

            if (additionalAssemblies != null)
            {
                assemblies = assemblies.Concat(additionalAssemblies);
            }

            var partTypes = assemblies
                .SelectMany(x => x.DefinedTypes)
                .Select(x => x.AsType());

            _compositionContext = new ContainerConfiguration()
                .WithParts(partTypes)
                .CreateContainer();

            HostServices = MefHostServices.Create(_compositionContext);

            // ReSharper disable once VirtualMemberCallInConstructor
            ParseOptions = CreateDefaultParseOptions();

            _documentationProviderService = GetService<IDocumentationProviderService>();

            DefaultReferences = references.GetReferences(DocumentationProviderFactory);
            DefaultImports = references.Imports;

            DisabledDiagnostics = disabledDiagnostics ?? ImmutableArray<string>.Empty;
            GetService<IDiagnosticService>().DiagnosticsUpdated += OnDiagnosticsUpdated;
        }

        public Func<string, DocumentationProvider> DocumentationProviderFactory => _documentationProviderService.GetDocumentationProvider;

        protected virtual IEnumerable<Assembly> GetDefaultCompositionAssemblies()
        {
            return DefaultCompositionAssemblies;
        }

        protected virtual ParseOptions CreateDefaultParseOptions()
        {
            return new CSharpParseOptions(kind: SourceCodeKind.Script,
                preprocessorSymbols: PreprocessorSymbols, languageVersion: LanguageVersion.Preview);
        }

        public MetadataReference CreateMetadataReference(string location)
        {
            return MetadataReference.CreateFromFile(location,
                documentation: _documentationProviderService.GetDocumentationProvider(location));
        }

        private void OnDiagnosticsUpdated(object sender, DiagnosticsUpdatedArgs diagnosticsUpdatedArgs)
        {
            var documentId = diagnosticsUpdatedArgs.DocumentId;
            if (documentId == null) return;

            if (_diagnosticsUpdatedNotifiers.TryGetValue(documentId, out var notifier))
            {
                if (diagnosticsUpdatedArgs.Kind == DiagnosticsUpdatedKind.DiagnosticsCreated)
                {
                    var remove = diagnosticsUpdatedArgs.Diagnostics.RemoveAll(d => DisabledDiagnostics.Contains(d.Id));
                    if (remove.Length != diagnosticsUpdatedArgs.Diagnostics.Length)
                    {
                        diagnosticsUpdatedArgs = diagnosticsUpdatedArgs.WithDiagnostics(remove);
                    }
                }

                notifier(diagnosticsUpdatedArgs);
            }
        }

        public TService GetService<TService>()
        {
            return _compositionContext.GetExport<TService>();
        }

        #endregion

        #region Reference Resolution

        internal void AddMetadataReference(ProjectId projectId, AssemblyIdentity assemblyIdentity)
        {
            // TODO
        }

        public bool HasReference(DocumentId documentId, string text)
        {
            if (documentId == null) throw new ArgumentNullException(nameof(documentId));

            if (!_workspaces.TryGetValue(documentId, out var workspace))
            {
                return false;
            }

            if (workspace.CurrentSolution.GetDocument(documentId) is Document document &&
                document.Project.TryGetCompilation(out var compilation))
            {
                return compilation.ReferencedAssemblyNames.Any(a => a.Name == text);
            }

            return false;
        }

        #endregion

        #region Documents

        public void CloseWorkspace(RoslynWorkspace workspace)
        {
            if (workspace == null) throw new ArgumentNullException(nameof(workspace));

            foreach (var documentId in workspace.CurrentSolution.Projects.SelectMany(p => p.DocumentIds))
            {
                _workspaces.TryRemove(documentId, out _);
                _diagnosticsUpdatedNotifiers.TryRemove(documentId, out _);
            }

            using (workspace) { }
        }

        public virtual RoslynWorkspace CreateWorkspace() => new RoslynWorkspace(HostServices, roslynHost: this);

        public void CloseDocument(DocumentId documentId)
        {
            if (documentId == null) throw new ArgumentNullException(nameof(documentId));

            if (_workspaces.TryGetValue(documentId, out var workspace))
            {
                workspace.CloseDocument(documentId);

                var document = workspace.CurrentSolution.GetDocument(documentId);

                if (document != null)
                {
                    var solution = document.Project.RemoveDocument(documentId).Solution;

                    if (!solution.Projects.SelectMany(d => d.DocumentIds).Any())
                    {
                        _workspaces.TryRemove(documentId, out workspace);

                        using (workspace) { }
                    }
                    else
                    {
                        workspace.SetCurrentSolution(solution);
                    }
                }
            }

            _diagnosticsUpdatedNotifiers.TryRemove(documentId, out _);
        }

        public Document? GetDocument(DocumentId documentId)
        {
            if (documentId == null) throw new ArgumentNullException(nameof(documentId));

            return _workspaces.TryGetValue(documentId, out var workspace)
                ? workspace.CurrentSolution.GetDocument(documentId)
                : null;
        }

        public DocumentId AddDocument(DocumentCreationArgs args)
        {
            if (args == null) throw new ArgumentNullException(nameof(args));
            if (args.SourceTextContainer == null) throw new ArgumentNullException(nameof(args.SourceTextContainer));

            return AddDocument(CreateWorkspace(), args);
        }

        public DocumentId AddRelatedDocument(DocumentId relatedDocumentId, DocumentCreationArgs args, bool addProjectReference = true)
        {
            if (args == null) throw new ArgumentNullException(nameof(args));

            if (!_workspaces.TryGetValue(relatedDocumentId, out var workspace))
            {
                throw new ArgumentException("Unable to locate the document's workspace", nameof(relatedDocumentId));
            }

            if (args.SourceTextContainer == null) throw new ArgumentNullException(nameof(args.SourceTextContainer));

            var documentId = AddDocument(workspace, args,
                addProjectReference ? workspace.CurrentSolution.GetDocument(relatedDocumentId) : null);

            return documentId;
        }

        private DocumentId AddDocument(RoslynWorkspace workspace, DocumentCreationArgs args, Document? previousDocument = null)
        {
            var solution = workspace.CurrentSolution.AddAnalyzerReferences(GetSolutionAnalyzerReferences());
            var project = CreateProject(solution, args,
                CreateCompilationOptions(args, previousDocument == null), previousDocument?.Project);
            var document = CreateDocument(project, args);
            var documentId = document.Id;

            workspace.SetCurrentSolution(document.Project.Solution);
            workspace.OpenDocument(documentId, args.SourceTextContainer);

            _workspaces.TryAdd(documentId, workspace);

            if (args.OnDiagnosticsUpdated != null)
            {
                _diagnosticsUpdatedNotifiers.TryAdd(documentId, args.OnDiagnosticsUpdated);
            }

            var onTextUpdated = args.OnTextUpdated;
            if (onTextUpdated != null)
            {
                workspace.ApplyingTextChange += (d, s) =>
                {
                    if (documentId == d) onTextUpdated(s);
                };
            }

            return documentId;
        }

        protected virtual IEnumerable<AnalyzerReference> GetSolutionAnalyzerReferences()
        {
            var loader = GetService<IAnalyzerAssemblyLoader>();
            yield return new AnalyzerFileReference(typeof(Compilation).Assembly.Location, loader);
            yield return new AnalyzerFileReference(typeof(CSharpResources).Assembly.Location, loader);
            yield return new AnalyzerFileReference(typeof(FeaturesResources).Assembly.Location, loader);
            yield return new AnalyzerFileReference(typeof(CSharpFeaturesResources).Assembly.Location, loader);
        }

        public void UpdateDocument(Document document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));

            if (!_workspaces.TryGetValue(document.Id, out var workspace))
            {
                return;
            }

            workspace.TryApplyChanges(document.Project.Solution);
        }

        protected virtual CompilationOptions CreateCompilationOptions(DocumentCreationArgs args, bool addDefaultImports)
        {
            var compilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                usings: addDefaultImports ? DefaultImports : ImmutableArray<string>.Empty,
                allowUnsafe: true,
                sourceReferenceResolver: new SourceFileResolver(ImmutableArray<string>.Empty, args.WorkingDirectory),
                metadataReferenceResolver: new CachedScriptMetadataResolver(args.WorkingDirectory, useCache: true),
                nullableContextOptions: NullableContextOptions.Enable);
            return compilationOptions;
        }

        protected virtual Document CreateDocument(Project project, DocumentCreationArgs args)
        {
            var id = DocumentId.CreateNewId(project.Id);
            var solution = project.Solution.AddDocument(id, args.Name ?? project.Name, args.SourceTextContainer.CurrentText);
            return solution.GetDocument(id)!;
        }

        protected virtual Project CreateProject(Solution solution, DocumentCreationArgs args, CompilationOptions compilationOptions, Project? previousProject = null)
        {
            var name = args.Name ?? "Program" + Interlocked.Increment(ref _documentNumber);
            var id = ProjectId.CreateNewId(name);

            var isScript = ParseOptions.Kind == SourceCodeKind.Script;

            if (isScript)
            {
                compilationOptions = compilationOptions.WithScriptClassName(name);
            }

            solution = solution.AddProject(ProjectInfo.Create(
                id,
                VersionStamp.Create(),
                name,
                name,
                LanguageNames.CSharp,
                isSubmission: isScript,
                parseOptions: ParseOptions,
                compilationOptions: compilationOptions,
                metadataReferences: previousProject != null ? ImmutableArray<MetadataReference>.Empty : DefaultReferences,
                projectReferences: previousProject != null ? new[] { new ProjectReference(previousProject.Id) } : null));

            var project = solution.GetProject(id);

            return project!;
        }

        #endregion
    }
}
