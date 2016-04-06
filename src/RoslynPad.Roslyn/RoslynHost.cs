using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition.Hosting;
using System.Composition.Convention;
using System.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Text;
using RoslynPad.Annotations;
using RoslynPad.Roslyn.Completion;
using RoslynPad.Roslyn.Diagnostics;
using RoslynPad.Roslyn.SignatureHelp;
using RoslynPad.Runtime;

namespace RoslynPad.Roslyn
{
    public sealed class RoslynHost
    {
        #region Fields

        private static readonly ImmutableArray<Type> _defaultReferenceAssemblyTypes = new[] {
            typeof(object),
            typeof(Task),
            typeof(List<>),
            typeof(Regex),
            typeof(StringBuilder),
            typeof(Uri),
            typeof(Enumerable),
            typeof(ObjectExtensions)
        }.ToImmutableArray();

        private static readonly ImmutableArray<Assembly> _defaultReferenceAssemblies =
            _defaultReferenceAssemblyTypes.Select(x => x.Assembly).Distinct().Concat(new[]
            {
                Assembly.Load("System.Runtime, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")
            }).ToImmutableArray();

        private readonly INuGetProvider _nuGetProvider;
        private readonly ConcurrentDictionary<DocumentId, RoslynWorkspace> _workspaces;
        private readonly ConcurrentDictionary<DocumentId, Action<DiagnosticsUpdatedArgs>> _diagnosticsUpdatedNotifiers;
        private readonly CSharpParseOptions _parseOptions;
        private readonly DocumentationProviderServiceFactory.DocumentationProviderService _documentationProviderService;
        private readonly string _referenceAssembliesPath;
        private readonly CompositionHost _compositionContext;
        private readonly MefHostServices _host;

        private int _documentNumber;

        internal ImmutableArray<MetadataReference> DefaultReferences { get; }

        #endregion

        #region Constructors

        public RoslynHost(INuGetProvider nuGetProvider = null)
        {
            _nuGetProvider = nuGetProvider;

            _workspaces = new ConcurrentDictionary<DocumentId, RoslynWorkspace>();
            _diagnosticsUpdatedNotifiers = new ConcurrentDictionary<DocumentId, Action<DiagnosticsUpdatedArgs>>();

            var assemblies = new[]
            {
                Assembly.Load("Microsoft.CodeAnalysis"),
                Assembly.Load("Microsoft.CodeAnalysis.CSharp"),
                Assembly.Load("Microsoft.CodeAnalysis.Features"),
                Assembly.Load("Microsoft.CodeAnalysis.CSharp.Features"),
                typeof(RoslynHost).Assembly,
            };

            // we can't import this entire assembly due to composition errors
            // and we don't need all the VS services
            var editorFeaturesAssembly = Assembly.Load("Microsoft.CodeAnalysis.EditorFeatures");
            var types = editorFeaturesAssembly.GetTypes().Where(x => x.Namespace == "Microsoft.CodeAnalysis.CodeFixes");

            _compositionContext = new ContainerConfiguration()
                .WithAssemblies(MefHostServices.DefaultAssemblies.Concat(assemblies))
                .WithParts(types)
                .WithDefaultConventions(new AttributeFilterProvider())
                .CreateContainer();

            _host = MefHostServices.Create(_compositionContext);

            _parseOptions = new CSharpParseOptions(kind: SourceCodeKind.Script);

            _referenceAssembliesPath = GetReferenceAssembliesPath();
            _documentationProviderService = new DocumentationProviderServiceFactory.DocumentationProviderService();

            DefaultReferences = _defaultReferenceAssemblies.Select(t =>
                (MetadataReference)MetadataReference.CreateFromFile(t.Location,
                    documentation: GetDocumentationProvider(t.Location))).ToImmutableArray();

            GetService<IDiagnosticService>().DiagnosticsUpdated += OnDiagnosticsUpdated;

            _compositionContext.GetExport<ISemanticChangeNotificationService>().OpenedDocumentSemanticChanged +=
                OnOpenedDocumentSemanticChanged;

            // MEF v1
            var container = new CompositionContainer(new AggregateCatalog(
                new AssemblyCatalog(Assembly.Load("Microsoft.CodeAnalysis.EditorFeatures")),
                new AssemblyCatalog(Assembly.Load("Microsoft.CodeAnalysis.CSharp.EditorFeatures")),
                new AssemblyCatalog(typeof(RoslynHost).Assembly)),
                CompositionOptions.DisableSilentRejection | CompositionOptions.IsThreadSafe);

            ((AggregateSignatureHelpProvider)GetService<ISignatureHelpProvider>()).Initialize(container);

            CompletionService.Initialize(container);
        }

        private void OnDiagnosticsUpdated(object sender, DiagnosticsUpdatedArgs diagnosticsUpdatedArgs)
        {
            Action<DiagnosticsUpdatedArgs> notifier;
            if (_diagnosticsUpdatedNotifiers.TryGetValue(diagnosticsUpdatedArgs.DocumentId, out notifier))
            {
                notifier(diagnosticsUpdatedArgs);
            }
        }

        private class AttributeFilterProvider : AttributedModelProvider
        {
            public override IEnumerable<Attribute> GetCustomAttributes(Type reflectedType, MemberInfo member)
            {
                return member.GetCustomAttributes().Where(x => !(x is ExtensionOrderAttribute));
            }

            public override IEnumerable<Attribute> GetCustomAttributes(Type reflectedType, ParameterInfo member)
            {
                return member.GetCustomAttributes().Where(x => !(x is ExtensionOrderAttribute));
            }
        }

        private async void OnOpenedDocumentSemanticChanged(object sender, Document document)
        {
            RoslynWorkspace workspace;
            if (_workspaces.TryGetValue(document.Id, out workspace))
            {
                await workspace.ProcessReferenceDirectives(document).ConfigureAwait(false);
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
            RoslynWorkspace workspace;
            if (_workspaces.TryGetValue(documentId, out workspace) && workspace.HasReference(text))
            {
                return true;
            }
            return _defaultReferenceAssemblies.Any(x => x.GetName().Name == text);
        }

        private static MetadataReferenceResolver CreateMetadataReferenceResolver(Workspace workspace)
        {
            var resolver = Activator.CreateInstance(
                // can't access this type due to a name collision with Scripting assembly
                // can't use extern alias because of project.json
                // ReSharper disable once AssignNullToNotNullAttribute
                Type.GetType("Microsoft.CodeAnalysis.RelativePathResolver, Microsoft.CodeAnalysis.Workspaces"),
                ImmutableArray<string>.Empty,
                AppDomain.CurrentDomain.BaseDirectory);
            return (MetadataReferenceResolver)Activator.CreateInstance(typeof(WorkspaceMetadataFileReferenceResolver),
                workspace.Services.GetService<IMetadataService>(),
                resolver);
        }

        #endregion

        #region Documentation

        private static string GetReferenceAssembliesPath()
        {
            var programFiles =
                Environment.GetFolderPath(Environment.Is64BitOperatingSystem
                    ? Environment.SpecialFolder.ProgramFilesX86
                    : Environment.SpecialFolder.ProgramFiles);
            var path = Path.Combine(programFiles, @"Reference Assemblies\Microsoft\Framework\.NETFramework");
            var directories = Directory.EnumerateDirectories(path).OrderByDescending(Path.GetFileName);
            return directories.FirstOrDefault();
        }

        private DocumentationProvider GetDocumentationProvider(string location)
        {
            if (_referenceAssembliesPath != null)
            {
                var fileName = Path.GetFileName(location);
                // ReSharper disable once AssignNullToNotNullAttribute
                var referenceLocation = Path.Combine(_referenceAssembliesPath, fileName);
                if (File.Exists(referenceLocation))
                {
                    location = referenceLocation;
                }
            }
            return _documentationProviderService.GetDocumentationProvider(location);
        }

        #endregion

        #region Documents

        public Document GetDocument(DocumentId documentId)
        {
            RoslynWorkspace workspace;
            return _workspaces.TryGetValue(documentId, out workspace) ? workspace.CurrentSolution.GetDocument(documentId) : null;
        }

        public DocumentId AddDocument([NotNull] SourceTextContainer sourceTextContainer, Action<DiagnosticsUpdatedArgs> onDiagnosticsUpdated, Action<SourceText> onTextUpdated)
        {
            if (sourceTextContainer == null) throw new ArgumentNullException(nameof(sourceTextContainer));

            var workspace = new RoslynWorkspace(_host, _nuGetProvider, this);
            if (onTextUpdated != null)
            {
                workspace.ApplyingTextChange += (d, s) => onTextUpdated(s);
            }
            workspace.Services.GetService<Microsoft.CodeAnalysis.SolutionCrawler.ISolutionCrawlerRegistrationService>()
                .Register(workspace);

            var currentSolution = workspace.CurrentSolution;
            var project = CreateSubmissionProject(currentSolution, CreateCompilationOptions(workspace));
            var currentDocument = SetSubmissionDocument(workspace, sourceTextContainer, project);

            _workspaces.TryAdd(currentDocument.Id, workspace);

            if (onDiagnosticsUpdated != null)
            {
                _diagnosticsUpdatedNotifiers.TryAdd(currentDocument.Id, onDiagnosticsUpdated);
            }

            return currentDocument.Id;
        }

        private static CSharpCompilationOptions CreateCompilationOptions(Workspace workspace)
        {
            var metadataReferenceResolver = CreateMetadataReferenceResolver(workspace);
            var compilationOptions = new CSharpCompilationOptions(OutputKind.NetModule,
                usings: _defaultReferenceAssemblyTypes.Select(x => x.Namespace).ToImmutableArray(),
                metadataReferenceResolver: metadataReferenceResolver);
            return compilationOptions;
        }

        private static Document SetSubmissionDocument(RoslynWorkspace workspace, SourceTextContainer textContainer, Project project)
        {
            var id = DocumentId.CreateNewId(project.Id);
            var solution = project.Solution.AddDocument(id, project.Name, textContainer.CurrentText);
            workspace.SetCurrentSolution(solution);
            workspace.OpenDocument(id, textContainer);
            return solution.GetDocument(id);
        }

        private Project CreateSubmissionProject(Solution solution, CSharpCompilationOptions compilationOptions)
        {
            var name = "Program" + _documentNumber++;
            var id = ProjectId.CreateNewId(name);
            solution = solution.AddProject(ProjectInfo.Create(id, VersionStamp.Create(), name, name, LanguageNames.CSharp,
                parseOptions: _parseOptions,
                compilationOptions: compilationOptions.WithScriptClassName(name),
                metadataReferences: DefaultReferences));
            return solution.GetProject(id);
        }

        #endregion

        #region Scripting

        public async Task<object> Execute(DocumentId documentId)
        {
            var text = await GetDocument(documentId).GetTextAsync().ConfigureAwait(false);
            var state = await CSharpScript.RunAsync(text.ToString(),
                ScriptOptions.Default
                    .AddImports(_defaultReferenceAssemblyTypes.Select(x => x.Namespace))
                    .AddReferences(_defaultReferenceAssemblies)
                    .WithMetadataResolver(new NuGetScriptMetadataResolver(_nuGetProvider))).ConfigureAwait(false);
            return state.ReturnValue;
        }

        class NuGetScriptMetadataResolver : MetadataReferenceResolver
        {
            private readonly INuGetProvider _nuGetProvider;
            private readonly ScriptMetadataResolver _inner;

            public NuGetScriptMetadataResolver(INuGetProvider nuGetProvider)
            {
                _nuGetProvider = nuGetProvider;
                _inner = ScriptMetadataResolver.Default;
            }

            public override bool Equals(object other)
            {
                return _inner.Equals(other);
            }

            public override int GetHashCode()
            {
                return _inner.GetHashCode();
            }

            public override ImmutableArray<PortableExecutableReference> ResolveReference(string reference, string baseFilePath, MetadataReferenceProperties properties)
            {
                reference = _nuGetProvider.ResolveReference(reference);
                return _inner.ResolveReference(reference, baseFilePath, properties);
            }
        }

        #endregion
    }
}
