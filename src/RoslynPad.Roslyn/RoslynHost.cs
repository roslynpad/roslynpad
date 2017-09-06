using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using RoslynPad.Roslyn.Diagnostics;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition.Hosting;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace RoslynPad.Roslyn
{
    public class RoslynHost : IRoslynHost
    {
        #region Fields

        internal static readonly ImmutableArray<string> PreprocessorSymbols =
            ImmutableArray.CreateRange(new[] { "__DEMO__", "__DEMO_EXPERIMENTAL__", "TRACE", "DEBUG" });

        private readonly NuGetConfiguration _nuGetConfiguration;
        private readonly ConcurrentDictionary<DocumentId, RoslynWorkspace> _workspaces;
        private readonly ConcurrentDictionary<DocumentId, Action<DiagnosticsUpdatedArgs>> _diagnosticsUpdatedNotifiers;
        private readonly CSharpParseOptions _parseOptions;
        private readonly IDocumentationProviderService _documentationProviderService;
        private readonly CompositionHost _compositionContext;
        private readonly MefHostServices _host;

        private int _documentNumber;

        public ImmutableArray<MetadataReference> DefaultReferences { get; }

        public ImmutableArray<string> DefaultImports { get; }

        #endregion

        #region Constructors

        static RoslynHost()
        {
            WorkaroundForDesktopShim(typeof(Compilation));
            WorkaroundForDesktopShim(typeof(TaggedText));
        }

        private static void WorkaroundForDesktopShim(Type typeInAssembly)
        {
            // DesktopShim doesn't work on Linux, so we hack around it

            typeInAssembly.GetTypeInfo().Assembly
                .GetType("Roslyn.Utilities.DesktopShim+FileNotFoundException")
                ?.GetRuntimeFields().FirstOrDefault(f => f.Name == "s_fusionLog")
                ?.SetValue(null, typeof(Exception).GetRuntimeProperty(nameof(Exception.InnerException)));
        }

        public RoslynHost(NuGetConfiguration nuGetConfiguration = null,
            IEnumerable<Assembly> additionalAssemblies = null,
            RoslynHostReferences references = null)
        {
            _nuGetConfiguration = nuGetConfiguration;
            if (references == null) references = RoslynHostReferences.Default;

            _workspaces = new ConcurrentDictionary<DocumentId, RoslynWorkspace>();
            _diagnosticsUpdatedNotifiers = new ConcurrentDictionary<DocumentId, Action<DiagnosticsUpdatedArgs>>();

            IEnumerable<Assembly> assemblies = new[]
            {
                Assembly.Load(new AssemblyName("Microsoft.CodeAnalysis")),
                Assembly.Load(new AssemblyName("Microsoft.CodeAnalysis.CSharp")),
                Assembly.Load(new AssemblyName("Microsoft.CodeAnalysis.Features")),
                Assembly.Load(new AssemblyName("Microsoft.CodeAnalysis.CSharp.Features")),
                typeof(RoslynHost).GetTypeInfo().Assembly,
            };

            if (additionalAssemblies != null)
            {
                assemblies = assemblies.Concat(additionalAssemblies);
            }

            var partTypes = MefHostServices.DefaultAssemblies.Concat(assemblies)
                .Distinct()
                .SelectMany(x => x.DefinedTypes)
                .Select(x => x.AsType());

            _compositionContext = new ContainerConfiguration()
                .WithParts(partTypes)
                .CreateContainer();

            _host = MefHostServices.Create(_compositionContext);

            _parseOptions = new CSharpParseOptions(kind: SourceCodeKind.Script,
                preprocessorSymbols: PreprocessorSymbols, languageVersion: LanguageVersion.Latest);

            _documentationProviderService = GetService<IDocumentationProviderService>();

            DefaultReferences = references.GetReferences(_documentationProviderService.GetDocumentationProvider);
            DefaultImports = references.Imports;

            GetService<IDiagnosticService>().DiagnosticsUpdated += OnDiagnosticsUpdated;
        }

        internal MetadataReference CreateMetadataReference(string location)
        {
            return MetadataReference.CreateFromFile(location,
                documentation: _documentationProviderService.GetDocumentationProvider(location));
        }

        private void OnDiagnosticsUpdated(object sender, DiagnosticsUpdatedArgs diagnosticsUpdatedArgs)
        {
            var documentId = diagnosticsUpdatedArgs?.DocumentId;
            if (documentId == null) return;

            if (_diagnosticsUpdatedNotifiers.TryGetValue(documentId, out var notifier))
            {
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

            if (workspace.CurrentSolution.GetDocument(documentId).Project.TryGetCompilation(out var compilation))
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

        public RoslynWorkspace CreateWorkspace() => new RoslynWorkspace(_host, this);

        public void CloseDocument(DocumentId documentId)
        {
            if (documentId == null) throw new ArgumentNullException(nameof(documentId));

            if (_workspaces.TryGetValue(documentId, out var workspace))
            {
                workspace.CloseDocument(documentId);

                var document = workspace.CurrentSolution.GetDocument(documentId);
                Debug.Assert(document != null, "document != null");

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


            _diagnosticsUpdatedNotifiers.TryRemove(documentId, out _);
        }

        public Document GetDocument(DocumentId documentId)
        {
            if (documentId == null) throw new ArgumentNullException(nameof(documentId));

            return _workspaces.TryGetValue(documentId, out var workspace)
                ? workspace.CurrentSolution.GetDocument(documentId)
                : null;
        }

        public DocumentId AddDocument(SourceTextContainer sourceTextContainer, string workingDirectory,
            Action<DiagnosticsUpdatedArgs> onDiagnosticsUpdated, Action<SourceText> onTextUpdated)
        {
            if (sourceTextContainer == null) throw new ArgumentNullException(nameof(sourceTextContainer));

            return AddDocument(CreateWorkspace(), sourceTextContainer, workingDirectory, onDiagnosticsUpdated, onTextUpdated);
        }

        public DocumentId AddRelatedDocument(DocumentId relatedDocumentId, SourceTextContainer sourceTextContainer,
            string workingDirectory, Action<DiagnosticsUpdatedArgs> onDiagnosticsUpdated, Action<SourceText> onTextUpdated,
            bool addProjectReference = true)
        {
            if (!_workspaces.TryGetValue(relatedDocumentId, out var workspace))
            {
                throw new ArgumentException("Unable to locate the document's workspace", nameof(relatedDocumentId));
            }

            if (sourceTextContainer == null) throw new ArgumentNullException(nameof(sourceTextContainer));

            var documentId = AddDocument(workspace, sourceTextContainer, workingDirectory, onDiagnosticsUpdated, onTextUpdated,
                addProjectReference ? workspace.CurrentSolution.GetDocument(relatedDocumentId) : null);

            return documentId;
        }

        private DocumentId AddDocument(RoslynWorkspace workspace, SourceTextContainer sourceTextContainer, string workingDirectory, Action<DiagnosticsUpdatedArgs> onDiagnosticsUpdated, Action<SourceText> onTextUpdated, Document previousDocument = null)
        {
            var currentSolution = workspace.CurrentSolution;
            var project = CreateSubmissionProject(currentSolution,
                CreateCompilationOptions(workspace, workingDirectory, previousDocument == null), previousDocument?.Project);
            var currentDocument = SetSubmissionDocument(workspace, sourceTextContainer, project);

            var documentId = currentDocument.Id;
            _workspaces.TryAdd(documentId, workspace);

            if (onDiagnosticsUpdated != null)
            {
                _diagnosticsUpdatedNotifiers.TryAdd(documentId, onDiagnosticsUpdated);
            }

            if (onTextUpdated != null)
            {
                workspace.ApplyingTextChange += (d, s) =>
                {
                    if (documentId == d) onTextUpdated(s);
                };
            }

            return documentId;
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

        private CSharpCompilationOptions CreateCompilationOptions(Workspace workspace, string workingDirectory, bool addImports)
        {
            var compilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                usings: addImports ? DefaultImports : ImmutableArray<string>.Empty,
                allowUnsafe: true,
                sourceReferenceResolver: new SourceFileResolver(ImmutableArray<string>.Empty, workingDirectory),
                metadataReferenceResolver: new NuGetScriptMetadataResolver(_nuGetConfiguration, workingDirectory, useCache: true));
            return compilationOptions;
        }

        private static Document SetSubmissionDocument(RoslynWorkspace workspace, SourceTextContainer textContainer,
            Project project)
        {
            var id = DocumentId.CreateNewId(project.Id);
            var solution = project.Solution.AddDocument(id, project.Name, textContainer.CurrentText);
            workspace.SetCurrentSolution(solution);
            workspace.OpenDocument(id, textContainer);
            return solution.GetDocument(id);
        }

        private Project CreateSubmissionProject(Solution solution, CSharpCompilationOptions compilationOptions, Project previousProject)
        {
            var name = "Program" + _documentNumber++;
            var id = ProjectId.CreateNewId(name);
            solution = solution.AddProject(ProjectInfo.Create(
                id, 
                VersionStamp.Create(),
                name, 
                name,
                LanguageNames.CSharp,
                isSubmission: true,
                parseOptions: _parseOptions,
                compilationOptions: compilationOptions.WithScriptClassName(name),
                metadataReferences: previousProject != null ? ImmutableArray<MetadataReference>.Empty : DefaultReferences));

            var project = solution.GetProject(id);

            if (previousProject != null)
            {
                project = project.AddProjectReference(new ProjectReference(previousProject.Id));
            }

            return project;
        }

        #endregion
    }
}
