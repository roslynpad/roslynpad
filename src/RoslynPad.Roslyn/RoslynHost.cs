using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using RoslynPad.Roslyn.Diagnostics;
using System.Collections.Generic;

namespace RoslynPad.Roslyn
{
    public class RoslynHost : IRoslynHost
    {
        #region Fields

        internal static readonly ImmutableArray<string> PreprocessorSymbols = ImmutableArray.CreateRange(new[] { "__DEMO__", "__DEMO_EXPERIMENTAL__", "TRACE", "DEBUG" });

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
                        .Select(x => x.AsType())
                        .ToArray();

            _compositionContext = new ContainerConfiguration()
                .WithParts(partTypes)
                .CreateContainer();

            _host = MefHostServices.Create(_compositionContext);

            _parseOptions = new CSharpParseOptions(kind: SourceCodeKind.Script, preprocessorSymbols: PreprocessorSymbols, languageVersion: LanguageVersion.Latest);

            _documentationProviderService = new DocumentationProviderService();

            DefaultReferences = references.GetReferences(_documentationProviderService.GetDocumentationProvider);
            DefaultImports = references.Imports;

            GetService<IDiagnosticService>().DiagnosticsUpdated += OnDiagnosticsUpdated;
        }

        internal MetadataReference CreateMetadataReference(string location)
        {
            return MetadataReference.CreateFromFile(location, documentation: _documentationProviderService.GetDocumentationProvider(location));
        }

        private void OnDiagnosticsUpdated(object sender, DiagnosticsUpdatedArgs diagnosticsUpdatedArgs)
        {
            var documentId = diagnosticsUpdatedArgs?.DocumentId;
            if (documentId == null) return;

            OnOpenedDocumentSyntaxChanged(GetDocument(documentId));

            if (_diagnosticsUpdatedNotifiers.TryGetValue(documentId, out var notifier))
            {
                notifier(diagnosticsUpdatedArgs);
            }
        }

        private async void OnOpenedDocumentSyntaxChanged(Document document)
        {
            if (_workspaces.TryGetValue(document.Id, out var workspace))
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
            if (_workspaces.TryGetValue(documentId, out var workspace) && workspace.HasReference(text))
            {
                return true;
            }

            if (workspace.CurrentSolution.GetDocument(documentId).Project.TryGetCompilation(out var compilation))
            {
                return compilation.ReferencedAssemblyNames.Any(a => a.Name == text);
            }

            return false;
        }

        private static MetadataReferenceResolver CreateMetadataReferenceResolver(Workspace workspace, string workingDirectory)
        {
            var resolver = Activator.CreateInstance(
                // can't access this type due to a name collision with Scripting assembly
                // can't use extern alias because of project.json
                // ReSharper disable once AssignNullToNotNullAttribute
                Type.GetType("Microsoft.CodeAnalysis.RelativePathResolver, Microsoft.CodeAnalysis.Workspaces"),
                ImmutableArray<string>.Empty,
                workingDirectory);
            return (MetadataReferenceResolver)Activator.CreateInstance(typeof(WorkspaceMetadataFileReferenceResolver),
                workspace.Services.GetService<IMetadataService>(),
                resolver);
        }

        #endregion

        #region Documentation

        private sealed class DocumentationProviderService : IDocumentationProviderService
        {
            private readonly ConcurrentDictionary<string, DocumentationProvider> _assemblyPathToDocumentationProviderMap =
                new ConcurrentDictionary<string, DocumentationProvider>();

            public DocumentationProvider GetDocumentationProvider(string location)
            {
                var finalPath = Path.ChangeExtension(location, "xml");
                if (!File.Exists(finalPath))
                {
                    finalPath = GetFilePath(RoslynHostReferences.ReferenceAssembliesPath.docPath, finalPath) ??
                                GetFilePath(RoslynHostReferences.ReferenceAssembliesPath.assemblyPath, finalPath);
                }

                return _assemblyPathToDocumentationProviderMap.GetOrAdd(location, 
                    _ => finalPath == null ? null : XmlDocumentationProvider.CreateFromFile(finalPath));
            }

            private static string GetFilePath(string path, string location)
            {
                if (path != null)
                {
                    // ReSharper disable once AssignNullToNotNullAttribute
                    var referenceLocation = Path.Combine(path, Path.GetFileName(location));
                    if (File.Exists(referenceLocation))
                    {
                        return referenceLocation;
                    }
                }

                return null;
            }
        }

        #endregion

        #region Documents

        public void CloseDocument(DocumentId documentId)
        {
            if (_workspaces.TryGetValue(documentId, out var workspace))
            {
                DiagnosticProvider.Disable(workspace);
                workspace.Dispose();
                _workspaces.TryRemove(documentId, out workspace);
            }
            _diagnosticsUpdatedNotifiers.TryRemove(documentId, out _);
        }

        public Document GetDocument(DocumentId documentId)
        {
            return _workspaces.TryGetValue(documentId, out var workspace)
                ? workspace.CurrentSolution.GetDocument(documentId)
                : null;
        }

        public DocumentId AddDocument(SourceTextContainer sourceTextContainer, string workingDirectory, Action<DiagnosticsUpdatedArgs> onDiagnosticsUpdated, Action<SourceText> onTextUpdated)
        {
            if (sourceTextContainer == null) throw new ArgumentNullException(nameof(sourceTextContainer));

            var workspace = new RoslynWorkspace(_host, _nuGetConfiguration, this);
            if (onTextUpdated != null)
            {
                workspace.ApplyingTextChange += (d, s) => onTextUpdated(s);
            }

            DiagnosticProvider.Enable(workspace, DiagnosticProvider.Options.Semantic);

            var currentSolution = workspace.CurrentSolution;
            var project = CreateSubmissionProject(currentSolution, CreateCompilationOptions(workspace, workingDirectory));
            var currentDocument = SetSubmissionDocument(workspace, sourceTextContainer, project);

            _workspaces.TryAdd(currentDocument.Id, workspace);

            if (onDiagnosticsUpdated != null)
            {
                _diagnosticsUpdatedNotifiers.TryAdd(currentDocument.Id, onDiagnosticsUpdated);
            }

            return currentDocument.Id;
        }

        public void UpdateDocument(Document document)
        {
            if (!_workspaces.TryGetValue(document.Id, out var workspace))
            {
                return;
            }

            workspace.TryApplyChanges(document.Project.Solution);
        }

        public ImmutableArray<string> GetReferencesDirectives(DocumentId documentId)
        {
            if (_workspaces.TryGetValue(documentId, out var workspace))
            {
                return workspace.ReferencesDirectives;
            }

            return ImmutableArray<string>.Empty;
        }

        private CSharpCompilationOptions CreateCompilationOptions(Workspace workspace, string workingDirectory)
        {
            var metadataReferenceResolver = CreateMetadataReferenceResolver(workspace, workingDirectory);
            var compilationOptions = new CSharpCompilationOptions(OutputKind.NetModule,
                usings: DefaultImports,
                allowUnsafe: true,
                sourceReferenceResolver: new SourceFileResolver(ImmutableArray<string>.Empty, workingDirectory),
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
    }
}
