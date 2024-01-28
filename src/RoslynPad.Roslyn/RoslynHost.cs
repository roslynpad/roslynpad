using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using RoslynPad.Roslyn.Diagnostics;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Composition.Hosting;
using System.Reflection;
using AnalyzerReference = Microsoft.CodeAnalysis.Diagnostics.AnalyzerReference;
using AnalyzerFileReference = Microsoft.CodeAnalysis.Diagnostics.AnalyzerFileReference;
using Roslyn.Utilities;

namespace RoslynPad.Roslyn;

public class RoslynHost : IRoslynHost
{
    internal static readonly ImmutableArray<string> PreprocessorSymbols =
        ["TRACE", "DEBUG"];

    internal static readonly ImmutableArray<Assembly> DefaultCompositionAssemblies =
        [
            typeof(WorkspacesResources).Assembly,
            typeof(CSharpWorkspaceResources).Assembly,
            typeof(FeaturesResources).Assembly,
            typeof(CSharpFeaturesResources).Assembly,
            typeof(RoslynHost).Assembly,
        ];

    internal static readonly ImmutableArray<Type> DefaultCompositionTypes =
        DefaultCompositionAssemblies.SelectMany(t => t.DefinedTypes).Select(t => t.AsType())
        .Concat(GetDiagnosticCompositionTypes())
        .ToImmutableArray();

    private static IEnumerable<Type> GetDiagnosticCompositionTypes() => MetadataUtil.LoadTypesByNamespaces(
        typeof(Microsoft.CodeAnalysis.Diagnostics.IDiagnosticService).Assembly,
        "Microsoft.CodeAnalysis.Diagnostics",
        "Microsoft.CodeAnalysis.CodeFixes");

    private readonly ConcurrentDictionary<DocumentId, RoslynWorkspace> _workspaces;
    private readonly ConcurrentDictionary<DocumentId, Action<DiagnosticsUpdatedArgs>> _diagnosticsUpdatedNotifiers;
    private readonly IDocumentationProviderService _documentationProviderService;
    private readonly CompositionHost _compositionContext;

    public HostServices HostServices { get; }
    public ParseOptions ParseOptions { get; }
    public ImmutableArray<MetadataReference> DefaultReferences { get; }
    public ImmutableArray<string> DefaultImports { get; }
    public ImmutableArray<string> DisabledDiagnostics { get; }
    public ImmutableArray<string> AnalyzerConfigFiles { get; }

    public RoslynHost(IEnumerable<Assembly>? additionalAssemblies = null,
        RoslynHostReferences? references = null,
        ImmutableArray<string>? disabledDiagnostics = null,
        ImmutableArray<string>? analyzerConfigFiles = null)
    {
        references ??= RoslynHostReferences.Empty;

        _workspaces = [];
        _diagnosticsUpdatedNotifiers = [];

        var partTypes = GetDefaultCompositionTypes();

        if (additionalAssemblies != null)
        {
            partTypes = partTypes.Concat(additionalAssemblies.SelectMany(a => a.DefinedTypes).Select(t => t.AsType()));
        }

        _compositionContext = new ContainerConfiguration()
            .WithParts(partTypes)
            .CreateContainer();

        HostServices = MefHostServices.Create(_compositionContext);

        ParseOptions = CreateDefaultParseOptions();

        _documentationProviderService = GetService<IDocumentationProviderService>();

        DefaultReferences = references.GetReferences(DocumentationProviderFactory);
        DefaultImports = references.Imports;

        DisabledDiagnostics = disabledDiagnostics ?? [];
        AnalyzerConfigFiles = analyzerConfigFiles ?? [];
        GetService<IDiagnosticService>().DiagnosticsUpdated += OnDiagnosticsUpdated;
    }

    public Func<string, DocumentationProvider> DocumentationProviderFactory => _documentationProviderService.GetDocumentationProvider;

    protected virtual IEnumerable<Type> GetDefaultCompositionTypes() => DefaultCompositionTypes;

    protected virtual ParseOptions CreateDefaultParseOptions() => new CSharpParseOptions(
        preprocessorSymbols: PreprocessorSymbols,
        languageVersion: LanguageVersion.Preview);

    public MetadataReference CreateMetadataReference(string location) => MetadataReference.CreateFromFile(location,
        documentation: _documentationProviderService.GetDocumentationProvider(location));

    private void OnDiagnosticsUpdated(object? sender, DiagnosticsUpdatedArgs diagnosticsUpdatedArgs)
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

    public TService GetService<TService>() => _compositionContext.GetExport<TService>();

    protected internal virtual void AddMetadataReference(ProjectId projectId, AssemblyIdentity assemblyIdentity)
    {
        // TODO
    }

    public void CloseWorkspace(RoslynWorkspace workspace)
    {
        ArgumentNullException.ThrowIfNull(workspace);

        foreach (var documentId in workspace.CurrentSolution.Projects.SelectMany(p => p.DocumentIds))
        {
            _workspaces.TryRemove(documentId, out _);
            _diagnosticsUpdatedNotifiers.TryRemove(documentId, out _);
        }

        using (workspace) { }
    }

    public virtual RoslynWorkspace CreateWorkspace() => new(HostServices, roslynHost: this);

    public void CloseDocument(DocumentId documentId)
    {
        ArgumentNullException.ThrowIfNull(documentId);

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
        ArgumentNullException.ThrowIfNull(documentId);

        return _workspaces.TryGetValue(documentId, out var workspace)
            ? workspace.CurrentSolution.GetDocument(documentId)
            : null;
    }

    public DocumentId AddDocument(DocumentCreationArgs args)
    {
        ArgumentNullException.ThrowIfNull(args);

        return AddDocument(CreateWorkspace(), args);
    }

    public DocumentId AddRelatedDocument(DocumentId relatedDocumentId, DocumentCreationArgs args, bool addProjectReference = true)
    {
        ArgumentNullException.ThrowIfNull(args);

        if (!_workspaces.TryGetValue(relatedDocumentId, out var workspace))
        {
            throw new ArgumentException("Unable to locate the document's workspace", nameof(relatedDocumentId));
        }

        var documentId = AddDocument(workspace, args,
            addProjectReference ? workspace.CurrentSolution.GetDocument(relatedDocumentId) : null);

        return documentId;
    }

    private DocumentId AddDocument(RoslynWorkspace workspace, DocumentCreationArgs args, Document? previousDocument = null)
    {
        var solution = workspace.CurrentSolution;

        if (previousDocument == null)
        { 
            solution = solution.AddAnalyzerReferences(GetSolutionAnalyzerReferences());
        }

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
        yield return new AnalyzerFileReference(MetadataUtil.GetAssemblyPath(typeof(Compilation).Assembly), loader);
        yield return new AnalyzerFileReference(MetadataUtil.GetAssemblyPath(typeof(CSharpResources).Assembly), loader);
        yield return new AnalyzerFileReference(MetadataUtil.GetAssemblyPath(typeof(FeaturesResources).Assembly), loader);
        yield return new AnalyzerFileReference(MetadataUtil.GetAssemblyPath(typeof(CSharpFeaturesResources).Assembly), loader);
    }

    public void UpdateDocument(Document document)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (!_workspaces.TryGetValue(document.Id, out var workspace))
        {
            return;
        }

        workspace.TryApplyChanges(document.Project.Solution);
    }

    protected virtual CompilationOptions CreateCompilationOptions(DocumentCreationArgs args, bool addDefaultImports)
    {
        var compilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
            usings: addDefaultImports ? DefaultImports : [],
            allowUnsafe: true,
            sourceReferenceResolver: new SourceFileResolver([], args.WorkingDirectory),
            // all #r references are resolved by the editor/msbuild
            metadataReferenceResolver: DummyScriptMetadataResolver.Instance,
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
        var name = args.Name ?? "New";
        var path = Path.Combine(args.WorkingDirectory, name);
        var id = ProjectId.CreateNewId(name);

        var parseOptions = ParseOptions.WithKind(args.SourceCodeKind);
        var isScript = args.SourceCodeKind == SourceCodeKind.Script;

        if (isScript)
        {
            compilationOptions = compilationOptions.WithScriptClassName(name);
        }

        var analyzerConfigDocuments = AnalyzerConfigFiles.Where(File.Exists).Select(file => DocumentInfo.Create(
            DocumentId.CreateNewId(id, debugName: file),
            name: file,
            loader: new FileTextLoader(file, defaultEncoding: null),
            filePath: file));

        solution = solution.AddProject(ProjectInfo.Create(
            id,
            VersionStamp.Create(),
            name,
            name,
            LanguageNames.CSharp,
            filePath: path,
            isSubmission: isScript,
            parseOptions: parseOptions,
            compilationOptions: compilationOptions,
            metadataReferences: previousProject != null ? [] : DefaultReferences,
            projectReferences: previousProject != null ? new[] { new ProjectReference(previousProject.Id) } : null)
            .WithAnalyzerConfigDocuments(analyzerConfigDocuments));

        var project = solution.GetProject(id)!;

        if (!isScript && GetUsings(project) is { Length: > 0 } usings)
        {
            project = project.AddDocument("RoslynPadGeneratedUsings", usings).Project;
        }

        return project;

        static string GetUsings(Project project)
        {
            if (project.CompilationOptions is CSharpCompilationOptions options)
            {
                return string.Join(" ", options.Usings.Select(i => $"global using {i};"));
            }

            return string.Empty;
        }
    }
}
