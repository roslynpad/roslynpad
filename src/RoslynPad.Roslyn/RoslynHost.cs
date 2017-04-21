using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
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
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using RoslynPad.Annotations;
using RoslynPad.Roslyn.Diagnostics;
using ObjectExtensions = RoslynPad.Runtime.ObjectExtensions;

namespace RoslynPad.Roslyn
{
    public sealed class RoslynHost : IRoslynHost
    {
        #region Fields

        private static readonly ImmutableArray<Type> _defaultReferenceAssemblyTypes = new[] {
            typeof(object),
            typeof(Thread),
            typeof(Task),
            typeof(List<>),
            typeof(Regex),
            typeof(StringBuilder),
            typeof(Uri),
            typeof(Enumerable),
            typeof(IEnumerable),
            typeof(ObjectExtensions),
            typeof(Path),
            typeof(Assembly),
        }.ToImmutableArray();

        private static readonly ImmutableArray<Assembly> _defaultReferenceAssemblies =
            _defaultReferenceAssemblyTypes.Select(x => x.Assembly).Distinct().Concat(new[]
            {
                Assembly.Load("System.Runtime, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a"),
                typeof(Microsoft.CSharp.RuntimeBinder.Binder).Assembly,
            })
            .ToImmutableArray();

        internal static readonly ImmutableArray<string> PreprocessorSymbols = ImmutableArray.CreateRange(new[] { "__DEMO__", "__DEMO_EXPERIMENTAL__", "TRACE", "DEBUG" });

        private readonly NuGetConfiguration _nuGetConfiguration;
        private readonly ConcurrentDictionary<DocumentId, RoslynWorkspace> _workspaces;
        private readonly ConcurrentDictionary<DocumentId, Action<DiagnosticsUpdatedArgs>> _diagnosticsUpdatedNotifiers;
        private readonly CSharpParseOptions _parseOptions;
        private readonly DocumentationProviderServiceFactory.DocumentationProviderService _documentationProviderService;
        private readonly string _referenceAssembliesPath;
        private readonly string _documentationPath;
        private readonly CompositionHost _compositionContext;
        private readonly MefHostServices _host;

        private int _documentNumber;

        internal ImmutableArray<MetadataReference> DefaultReferences { get; }

        internal ImmutableArray<string> DefaultImports { get; }

        #endregion

        #region Constructors

        public RoslynHost(NuGetConfiguration nuGetConfiguration = null, 
            IEnumerable<Assembly> additionalAssemblies = null, 
            IEnumerable<string> additionalReferencedAssemblyLocations = null)
        {
            _nuGetConfiguration = nuGetConfiguration;

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

            if (additionalAssemblies != null)
            {
                assemblies = assemblies.Concat(additionalAssemblies).ToArray();
            }

            var partTypes = MefHostServices.DefaultAssemblies.Concat(assemblies)
                    .Distinct()
                    .SelectMany(x => x.GetTypes())
                    .Concat(new[] { typeof(DocumentationProviderServiceFactory) })
                    .ToArray();

            _compositionContext = new ContainerConfiguration()
                .WithParts(partTypes)
                .CreateContainer();

            _host = MefHostServices.Create(_compositionContext);

            _parseOptions = new CSharpParseOptions(kind: SourceCodeKind.Script, preprocessorSymbols: PreprocessorSymbols);

            (_referenceAssembliesPath, _documentationPath) = GetReferenceAssembliesPath();
            _documentationProviderService = new DocumentationProviderServiceFactory.DocumentationProviderService();

            DefaultReferences = GetMetadataReferences(additionalReferencedAssemblyLocations);

            DefaultImports = _defaultReferenceAssemblyTypes.Select(x => x.Namespace).Distinct().ToImmutableArray();

            GetService<IDiagnosticService>().DiagnosticsUpdated += OnDiagnosticsUpdated;
        }

        private ImmutableArray<MetadataReference> GetMetadataReferences(IEnumerable<string> additionalReferencedAssemblyLocations = null)
        {
            // allow facade assemblies to take precedence
            var dictionary = _defaultReferenceAssemblies
                .Select(x => x.Location)
                .Concat(additionalReferencedAssemblyLocations ?? Enumerable.Empty<string>())
                .ToImmutableDictionary(Path.GetFileNameWithoutExtension)
                .SetItems(TryGetFacadeAssemblies()
                    .ToImmutableDictionary(Path.GetFileNameWithoutExtension));

            var metadataReferences = dictionary.Values
                .Select(CreateMetadataReference)
                .ToImmutableArray();

            return metadataReferences;
        }

        internal MetadataReference CreateMetadataReference(string location)
        {
            return MetadataReference.CreateFromFile(location, documentation: GetDocumentationProvider(location));
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
            return _defaultReferenceAssemblies.Any(x => x.GetName().Name == text);
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

        private static (string assemblyPath, string docPath) GetReferenceAssembliesPath()
        {
            string assemblyPath = null;
            string docPath = null;

            var programFiles =
                Environment.GetFolderPath(Environment.Is64BitOperatingSystem
                    ? Environment.SpecialFolder.ProgramFilesX86
                    : Environment.SpecialFolder.ProgramFiles);
            var path = Path.Combine(programFiles, @"Reference Assemblies\Microsoft\Framework\.NETFramework");
            if (Directory.Exists(path))
            {
                assemblyPath = IOUtilities.PerformIO(() => Directory.GetDirectories(path), Array.Empty<string>())
                    .Select(x => new { path = x, version = GetFxVersionFromPath(x) })
                    .OrderByDescending(x => x.version)
                    .FirstOrDefault(x => File.Exists(Path.Combine(x.path, "System.dll")))?.path;

                if (assemblyPath == null || !File.Exists(Path.Combine(assemblyPath, "System.xml")))
                {
                    docPath = GetReferenceDocumentationPath(path);
                }
            }

            return (assemblyPath, docPath);
        }

        private static string GetReferenceDocumentationPath(string path)
        {
            string docPath = null;

            var docPathTemp = Path.Combine(path, "V4.X");
            if (File.Exists(Path.Combine(docPathTemp, "System.xml")))
            {
                docPath = docPathTemp;
            }
            else
            {
                var localeDirectory = IOUtilities.PerformIO(() => Directory.GetDirectories(docPathTemp),
                        Array.Empty<string>()).FirstOrDefault();
                if (localeDirectory != null && File.Exists(Path.Combine(localeDirectory, "System.xml")))
                {
                    docPath = localeDirectory;
                }
            }

            return docPath;
        }

        private static Version GetFxVersionFromPath(string path)
        {
            var name = Path.GetFileName(path);
            if (name?.StartsWith("v", StringComparison.OrdinalIgnoreCase) == true)
            {
                if (Version.TryParse(name.Substring(1), out var version))
                {
                    return version;
                }
            }
            
            return new Version(0, 0);
        }

        private IEnumerable<string> TryGetFacadeAssemblies()
        {
            if (_referenceAssembliesPath != null)
            {
                var facadesPath = Path.Combine(_referenceAssembliesPath, "Facades");
                if (Directory.Exists(facadesPath))
                {
                    return Directory.EnumerateFiles(facadesPath, "*.dll");
                }
            }

            return Array.Empty<string>();
        }

        private DocumentationProvider GetDocumentationProvider(string location)
        {
            if (File.Exists(Path.ChangeExtension(location, "xml")))
            {
                return _documentationProviderService.GetDocumentationProvider(location);
            }

            return GetDocumentationProviderFromPath(_documentationPath, location) ??
                   GetDocumentationProviderFromPath(_referenceAssembliesPath, location);
        }

        private DocumentationProvider GetDocumentationProviderFromPath(string path, string location)
        {
            if (path != null)
            {
                // ReSharper disable once AssignNullToNotNullAttribute
                var referenceLocation = Path.Combine(path, Path.GetFileName(location));
                if (File.Exists(Path.ChangeExtension(referenceLocation, "xml")))
                {
                    return _documentationProviderService.GetDocumentationProvider(referenceLocation);
                }
            }

            return null;
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

        public DocumentId AddDocument([NotNull] SourceTextContainer sourceTextContainer, [NotNull] string workingDirectory, Action<DiagnosticsUpdatedArgs> onDiagnosticsUpdated, Action<SourceText> onTextUpdated)
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
