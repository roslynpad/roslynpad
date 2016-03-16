extern alias workspaces;

using System;
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
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using RoslynPad.Roslyn.Completion;
using RoslynPad.Roslyn.SignatureHelp;
using RoslynPad.Runtime;

namespace RoslynPad.Roslyn
{
    internal sealed class RoslynHost
    {
        #region Fields

        private static readonly Type[] _assemblyTypes =
        {
            typeof(object),
            typeof(Task),
            typeof(List<>),
            typeof(Regex),
            typeof(StringBuilder),
            typeof(Uri),
            typeof(Enumerable),
            typeof(ObjectExtensions)
        };

        private readonly RoslynWorkspace _workspace;
        private readonly CSharpParseOptions _parseOptions;
        private readonly CSharpCompilationOptions _compilationOptions;
        private readonly ImmutableArray<MetadataReference> _references;
        private readonly DocumentationProviderServiceFactory.DocumentationProviderService _documentationProviderService;
        private readonly string _referenceAssembliesPath;
        private readonly CompositionHost _compositionContext;
        private readonly Dictionary<string, DirectiveInfo> _referencesDirectives;
        private readonly SemaphoreSlim _referenceDirectivesLock;

        private DocumentId _currentDocumenId;
        private int _documentNumber;
        private CancellationTokenSource _referenceDirectivesCancellationTokenSource;

        public Workspace Workspace => _workspace;

        #endregion

        #region Constructors

        public RoslynHost()
        {
            _referencesDirectives = new Dictionary<string, DirectiveInfo>();
            _referenceDirectivesLock = new SemaphoreSlim(1, 1);

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

            var host = MefHostServices.Create(_compositionContext);

            _workspace = new RoslynWorkspace(host);
            _workspace.ApplyingTextChange += (d, s) => ApplyingTextChange?.Invoke(d, s);

            _parseOptions = new CSharpParseOptions(kind: SourceCodeKind.Script);

            _referenceAssembliesPath = GetReferenceAssembliesPath();
            _documentationProviderService = new DocumentationProviderServiceFactory.DocumentationProviderService();

            _references = _assemblyTypes.Select(t =>
                (MetadataReference)MetadataReference.CreateFromFile(t.Assembly.Location,
                    documentation: GetDocumentationProvider(t.Assembly.Location))).ToImmutableArray();
            var metadataReferenceResolver = CreateMetadataReferenceResolver();
            _compilationOptions = new CSharpCompilationOptions(OutputKind.NetModule,
                usings: _assemblyTypes.Select(x => x.Namespace).ToImmutableArray(),
                metadataReferenceResolver: metadataReferenceResolver);

            _workspace.Services.GetService<Microsoft.CodeAnalysis.SolutionCrawler.ISolutionCrawlerRegistrationService>()
                .Register(_workspace);

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
            OpenedDocumentSemanticChanged?.Invoke(this, document);

            await ProcessReferenceDirectives(document).ConfigureAwait(false);
        }


        public event EventHandler<Document> OpenedDocumentSemanticChanged;

        public TService GetService<TService>()
        {
            return _compositionContext.GetExport<TService>();
        }

        #endregion

        #region Reference Resolution

        private class DirectiveInfo
        {
            public MetadataReference MetadataReference { get; }

            public bool IsActive { get; set; }

            public DirectiveInfo(MetadataReference metadataReference)
            {
                MetadataReference = metadataReference;
                IsActive = true;
            }
        }

        private async Task ProcessReferenceDirectives(Document document)
        {
            CancellationToken cancellationToken;
            lock (_referenceDirectivesLock)
            {
                _referenceDirectivesCancellationTokenSource?.Cancel();
                _referenceDirectivesCancellationTokenSource = new CancellationTokenSource();
                cancellationToken = _referenceDirectivesCancellationTokenSource.Token;
            }

            // ReSharper disable once MethodSupportsCancellation
            using (await _referenceDirectivesLock.DisposableWaitAsync().ConfigureAwait(false))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var project = document.Project;
                var directives = ((CompilationUnitSyntax)await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false))
                        .GetReferenceDirectives().Select(x => x.File.ValueText).ToImmutableHashSet();

                foreach (var referenceDirective in _referencesDirectives)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (referenceDirective.Value.IsActive && !directives.Contains(referenceDirective.Key))
                    {
                        referenceDirective.Value.IsActive = false;
                    }
                }

                foreach (var directive in directives)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    DirectiveInfo referenceDirective;
                    if (_referencesDirectives.TryGetValue(directive, out referenceDirective))
                    {
                        referenceDirective.IsActive = true;
                    }
                    else
                    {
                        _referencesDirectives.Add(directive, new DirectiveInfo(ResolveReference(directive)));
                    }
                }

                var solution = project.Solution;
                var references =
                    _referencesDirectives.Where(x => x.Value.IsActive).Select(x => x.Value.MetadataReference).WhereNotNull();
                var newSolution = solution.WithProjectMetadataReferences(project.Id, _references.Concat(references));

                cancellationToken.ThrowIfCancellationRequested();

                _workspace.SetCurrentSolution(newSolution);
            }
        }

        private static MetadataReference ResolveReference(string name)
        {
            if (File.Exists(name))
            {
                return MetadataReference.CreateFromFile(name);
            }
            try
            {
                var assemblyName = GlobalAssemblyCache.Instance.ResolvePartialName(name);
                if (assemblyName == null)
                {
                    return null;
                }
                var assembly = Assembly.Load(assemblyName.ToString());
                return MetadataReference.CreateFromFile(assembly.Location);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private MetadataReferenceResolver CreateMetadataReferenceResolver()
        {
            return new WorkspaceMetadataFileReferenceResolver(
                _workspace.Services.GetService<IMetadataService>(),
                new workspaces::Microsoft.CodeAnalysis.RelativePathResolver(ImmutableArray<string>.Empty,
                    AppDomain.CurrentDomain.BaseDirectory));
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

        public Document CurrentDocument => _workspace.CurrentSolution.GetDocument(_currentDocumenId);

        public event Action<DocumentId, SourceText> ApplyingTextChange;

        public void SetDocument(SourceTextContainer textContainer)
        {
            var currentSolution = _workspace.CurrentSolution;
            var project = CreateSubmissionProject(currentSolution);
            var currentDocument = SetSubmissionDocument(textContainer, project);
            _currentDocumenId = currentDocument.Id;
        }

        private Document SetSubmissionDocument(SourceTextContainer textContainer, Project project)
        {
            var id = DocumentId.CreateNewId(project.Id);
            var solution = project.Solution.AddDocument(id, project.Name, textContainer.CurrentText);
            _workspace.SetCurrentSolution(solution);
            _workspace.OpenDocument(id, textContainer);
            return solution.GetDocument(id);
        }

        private Project CreateSubmissionProject(Solution solution)
        {
            var name = "Program" + _documentNumber++;
            var id = ProjectId.CreateNewId(name);
            solution = solution.AddProject(ProjectInfo.Create(id, VersionStamp.Create(), name, name, LanguageNames.CSharp,
                parseOptions: _parseOptions,
                compilationOptions: _compilationOptions.WithScriptClassName(name),
                metadataReferences: _references));
            return solution.GetProject(id);
        }

        #endregion

        #region Scripting

        public async Task<object> Execute()
        {
            var text = await CurrentDocument.GetTextAsync().ConfigureAwait(false);
            var state = await CSharpScript.RunAsync(text.ToString(),
                ScriptOptions.Default
                    .AddImports(_assemblyTypes.Select(x => x.Namespace))
                    .AddReferences(_assemblyTypes.Select(x => x.Assembly))).ConfigureAwait(false);
            return state.ReturnValue;
        }

        #endregion
    }
}
