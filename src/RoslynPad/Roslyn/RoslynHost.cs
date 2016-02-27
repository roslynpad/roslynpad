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
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Text;
using RoslynPad.Roslyn.CodeFixes;
using RoslynPad.Roslyn.Completion;
using RoslynPad.Roslyn.Diagnostics;
using RoslynPad.Roslyn.SignatureHelp;
using RoslynPad.Runtime;
using Expression = System.Linq.Expressions.Expression;

namespace RoslynPad.Roslyn
{
    internal sealed class RoslynHost
    {
        #region Fields

        private static readonly Type[] _assemblyTypes =
        {
            typeof (object),
            typeof (Task),
            typeof (List<>),
            typeof (Regex),
            typeof (StringBuilder),
            typeof (Uri),
            typeof (Enumerable),
            typeof (ObjectExtensions)
        };

        private readonly RoslynWorkspace _workspace;
        private readonly CSharpParseOptions _parseOptions;
        private readonly CSharpCompilationOptions _compilationOptions;
        private readonly ImmutableArray<MetadataReference> _references;
        private readonly IReadOnlyList<ISignatureHelpProvider> _signatureHelpProviders;
        private readonly Func<string, DocumentationProvider> _documentationProviderFactory;
        private readonly string _referenceAssembliesPath;
        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        private readonly IDiagnosticService _diagnosticsService;
        private readonly ICodeFixService _codeFixService;

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
            var documentTrackingServiceType = DocumentTrackingServiceProxy.GeneratedType.Value;
            // ReSharper disable once UnusedVariable
            var workspaceDiagnosticAnalyzerProviderServiceType =
                WorkspaceDiagnosticAnalyzerProviderServiceProxy.GeneratedType.Value;

            var assemblies = new[]
            {
                Assembly.Load("Microsoft.CodeAnalysis"),
                Assembly.Load("Microsoft.CodeAnalysis.CSharp"),
                Assembly.Load("Microsoft.CodeAnalysis.Features"),
                Assembly.Load("Microsoft.CodeAnalysis.CSharp.Features"),
                documentTrackingServiceType.Assembly,
            };

            // we can't import this entire assembly due to composition errors
            // and we don't need all the VS services
            var editorFeaturesAssembly = Assembly.Load("Microsoft.CodeAnalysis.EditorFeatures");
            var types = editorFeaturesAssembly.GetTypes().Where(x => x.Namespace == "Microsoft.CodeAnalysis.CodeFixes");

            var compositionHost = new ContainerConfiguration()
                .WithAssemblies(MefHostServices.DefaultAssemblies.Concat(assemblies))
                .WithParts(types)
                .WithDefaultConventions(new AttributeFilterProvider())
                .CreateContainer();

            var host = MefHostServices.Create(compositionHost);

            _workspace = new RoslynWorkspace(host);
            _workspace.ApplyingTextChange += (d, s) => ApplyingTextChange?.Invoke(d, s);

            var documentTrackingService = _workspace.Services.GetService(DocumentTrackingServiceProxy.InterfaceType);
            RoslynInterfaceProxy.Initialize(documentTrackingService, new DocumentTrackingServiceProxy(), _workspace);

            var workspaceDiagnosticAnalyzerProviderService = compositionHost.GetExport(WorkspaceDiagnosticAnalyzerProviderServiceProxy.InterfaceType);
            RoslynInterfaceProxy.Initialize(workspaceDiagnosticAnalyzerProviderService, new WorkspaceDiagnosticAnalyzerProviderServiceProxy(), _workspace);

            _parseOptions = new CSharpParseOptions(kind: SourceCodeKind.Script);

            _referenceAssembliesPath = GetReferenceAssembliesPath();
            _documentationProviderFactory = GetDocumentationProviderFactory();

            _references = _assemblyTypes.Select(t =>
                (MetadataReference)MetadataReference.CreateFromFile(t.Assembly.Location,
                    documentation: GetDocumentationProvider(t.Assembly.Location))).ToImmutableArray();
            _compilationOptions = new CSharpCompilationOptions(OutputKind.NetModule,
                usings: _assemblyTypes.Select(x => x.Namespace).ToImmutableArray());

            SolutionCrawlerRegistrationService.Register(_workspace);

            _diagnosticsService = DiagnosticsService.Load(compositionHost);
            _diagnosticsService.DiagnosticsUpdated += (sender, args) => DiagnosticsUpdated?.Invoke(this, args);

            _codeFixService = CodeFixService.Load(compositionHost);

            // MEF v1
            var container = new CompositionContainer(new AggregateCatalog(
                new AssemblyCatalog(Assembly.Load("Microsoft.CodeAnalysis.EditorFeatures")),
                new AssemblyCatalog(Assembly.Load("Microsoft.CodeAnalysis.CSharp.EditorFeatures"))),
                CompositionOptions.DisableSilentRejection | CompositionOptions.IsThreadSafe);

            _signatureHelpProviders = SignatureHelperProvider.LoadProviders(container);
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
            return _documentationProviderFactory(location);
        }

        private static Func<string, DocumentationProvider> GetDocumentationProviderFactory()
        {
            var docProviderType = Type.GetType("Microsoft.CodeAnalysis.Host.DocumentationProviderServiceFactory+DocumentationProviderService, Microsoft.CodeAnalysis.Workspaces.Desktop",
                    throwOnError: true);
            var docProvider = Activator.CreateInstance(docProviderType);
            var p = Expression.Parameter(typeof(string));
            var docProviderFunc =
                Expression.Lambda<Func<string, DocumentationProvider>>(
                    Expression.Call(Expression.Constant(docProvider, docProviderType),
                        docProviderType.GetMethod("GetDocumentationProvider"), p), p).Compile();
            return docProviderFunc;
        }

        #endregion

        #region Diagnostics

        public event EventHandler<DiagnosticsUpdatedArgs> DiagnosticsUpdated;

        #endregion

        #region Code Fixes

        public Task<IEnumerable<CodeFixCollection>> GetFixesAsync(TextSpan textSpan,
            bool includeSuppressionFixes,
            CancellationToken cancellationToken)
        {
            return _codeFixService.GetFixesAsync(GetCurrentDocument(), textSpan, includeSuppressionFixes,
                cancellationToken);
        }

        #endregion

        #region Documents

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

        #region Completion

        public async Task<CompletionList> GetCompletion(CompletionTriggerInfo trigger, int position)
        {
            var document = GetCurrentDocument();
            var list = await CompletionService.GetCompletionListAsync(
                document, position, trigger).ConfigureAwait(false);
            return list;
        }

        private Document GetCurrentDocument()
        {
            return _workspace.CurrentSolution.GetDocument(_currentDocumenId);
        }

        public Task<bool> IsCompletionTriggerCharacter(int position)
        {
            return CompletionService.IsCompletionTriggerCharacterAsync(GetCurrentDocument(), position);
        }

        #endregion

        #region Signature Help

        public async Task<bool> IsSignatureHelpTriggerCharacter(int position)
        {
            var text = await GetCurrentDocument().GetTextAsync().ConfigureAwait(false);
            var character = text.GetSubText(new TextSpan(position, 1))[0];
            return _signatureHelpProviders.Any(p => p.IsTriggerCharacter(character));
        }

        public async Task<SignatureHelpItems> GetSignatureHelp(SignatureHelpTriggerInfo trigger, int position)
        {
            var document = GetCurrentDocument();
            foreach (var provider in _signatureHelpProviders)
            {
                var items = await provider.GetItemsAsync(document, position, trigger, CancellationToken.None)
                            .ConfigureAwait(false);
                if (items != null)
                {
                    return items;
                }
            }
            return null;
        }

        #endregion

        #region Scripting

        public async Task Execute()
        {
            SourceText text;
            if (GetCurrentDocument().TryGetText(out text))
            {
                await CSharpScript.RunAsync(text.ToString(),
                    ScriptOptions.Default
                        .AddImports(_assemblyTypes.Select(x => x.Namespace))
                        .AddReferences(_assemblyTypes.Select(x => x.Assembly))).ConfigureAwait(false);
            }
        }

        #endregion
    }
}
