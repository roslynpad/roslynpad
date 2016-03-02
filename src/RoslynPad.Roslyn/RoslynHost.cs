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
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Text;
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

        private DocumentId _currentDocumenId;
        private int _documentNumber;

        public Workspace Workspace => _workspace;

        #endregion

        #region Constructors

        class AttributeFilterProvider : AttributedModelProvider
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

        public RoslynHost()
        {
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
            _compilationOptions = new CSharpCompilationOptions(OutputKind.NetModule,
                usings: _assemblyTypes.Select(x => x.Namespace).ToImmutableArray());

            _workspace.Services.GetService<Microsoft.CodeAnalysis.SolutionCrawler.ISolutionCrawlerRegistrationService>()
                .Register(_workspace);

            // MEF v1
            var container = new CompositionContainer(new AggregateCatalog(
                new AssemblyCatalog(Assembly.Load("Microsoft.CodeAnalysis.EditorFeatures")),
                new AssemblyCatalog(Assembly.Load("Microsoft.CodeAnalysis.CSharp.EditorFeatures"))),
                CompositionOptions.DisableSilentRejection | CompositionOptions.IsThreadSafe);

            ((AggregateSignatureHelpProvider)GetService<ISignatureHelpProvider>()).Initialize(container);
        }

        public TService GetService<TService>()
        {
            return _compositionContext.GetExport<TService>();
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
