using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Text;
using NuGet.Versioning;
using RoslynPad.Hosting;
using RoslynPad.Roslyn.Rename;
using RoslynPad.Runtime;
using RoslynPad.Utilities;

namespace RoslynPad.UI
{
    [Export]
    public class OpenDocumentViewModel : NotificationObject
    {
        private const string DefaultILText = "// Run to view IL";

        private readonly IServiceProvider _serviceProvider;
        private readonly IAppDispatcher _dispatcher;
        private readonly ITelemetryProvider _telemetryProvider;
        private IExecutionHost? _executionHost;
        private ObservableCollection<IResultObject> _results;
        private CancellationTokenSource _cts;
        private bool _isRunning;
        private bool _isDirty;
        private ExecutionPlatform _platform;
        private bool _isSaving;
        private IDisposable _viewDisposable;
        private Action<ExceptionResultObject?> _onError;
        private Func<TextSpan> _getSelection;
        private string _ilText;
        private bool _isInitialized;
        private bool _isLiveMode;
        private Timer _liveModeTimer;
        private ExecutionHostParameters _executionHostParameters;
        private PlatformVersion _platformVersion;

        public string Id { get; }
        public string BuildPath { get; }

        public string WorkingDirectory => Document != null
            ? Path.GetDirectoryName(Document.Path)
            : MainViewModel.DocumentRoot.Path;

        public IEnumerable<object> Results => _results;

        internal ObservableCollection<IResultObject> ResultsInternal
        {
            // ReSharper disable once UnusedMember.Local
            get => _results;
            private set
            {
                _results = value;
                OnPropertyChanged(nameof(Results));
            }
        }

        public IDelegateCommand ToggleLiveModeCommand { get; }

        public bool IsLiveMode
        {
            get => _isLiveMode;
            private set
            {
                if (!SetProperty(ref _isLiveMode, value)) return;
                RunCommand.RaiseCanExecuteChanged();

                if (value)
                {
                    // ReSharper disable once UnusedVariable
                    var task = Run();

                    if (_liveModeTimer == null)
                    {
                        _liveModeTimer = new Timer(o => _dispatcher.InvokeAsync(() =>
                        {
                            // ReSharper disable once UnusedVariable
                            var runTask = Run();
                        }), null, Timeout.Infinite, Timeout.Infinite);
                    }
                }
            }
        }

        public DocumentViewModel? Document { get; private set; }

        public string ILText
        {
            get => _ilText;
            private set => SetProperty(ref _ilText, value);
        }

        [ImportingConstructor]
#pragma warning disable CS8618 // Non-nullable field is uninitialized.
        public OpenDocumentViewModel(IServiceProvider serviceProvider, MainViewModelBase mainViewModel, ICommandProvider commands, IAppDispatcher appDispatcher, ITelemetryProvider telemetryProvider)
#pragma warning restore CS8618 // Non-nullable field is uninitialized.
        {
            Id = Guid.NewGuid().ToString();
            BuildPath = Path.Combine(Path.GetTempPath(), "RoslynPad", "Build", Id);
            var nuGetBuildPath = Path.Combine(BuildPath, "nuget");

            _telemetryProvider = telemetryProvider;

            try
            {
                Directory.CreateDirectory(nuGetBuildPath);
            }
            catch (Exception ex)
            {
                _telemetryProvider.ReportError(ex);
            }

            _serviceProvider = serviceProvider;
            MainViewModel = mainViewModel;
            CommandProvider = commands;

            NuGet = serviceProvider.GetService<NuGetDocumentViewModel>();
            NuGet.Id = Id;
            NuGet.BuildPath = nuGetBuildPath;
            NuGet.RestoreCompleted += OnNuGetRestoreCompleted;

            _dispatcher = appDispatcher;
            AvailablePlatforms = serviceProvider.GetService<IPlatformsFactory>()
                .GetExecutionPlatforms().ToImmutableArray();

            OpenBuildPathCommand = commands.Create(() => OpenBuildPath());
            SaveCommand = commands.CreateAsync(() => Save(promptSave: false));
            RunCommand = commands.CreateAsync(Run, () => !IsRunning && Platform != null);
            RestartHostCommand = commands.CreateAsync(RestartHost, () => Platform != null);
            FormatDocumentCommand = commands.CreateAsync(FormatDocument);
            CommentSelectionCommand = commands.CreateAsync(() => CommentUncommentSelection(CommentAction.Comment));
            UncommentSelectionCommand = commands.CreateAsync(() => CommentUncommentSelection(CommentAction.Uncomment));
            RenameSymbolCommand = commands.CreateAsync(RenameSymbol);
            ToggleLiveModeCommand = commands.Create(() => IsLiveMode = !IsLiveMode);

            ILText = DefaultILText;
        }

        private void OnNuGetRestoreCompleted(NuGetRestoreResult restoreResult)
        {
            var host = MainViewModel.RoslynHost;
            var document = host.GetDocument(DocumentId);
            if (document == null)
            {
                return;
            }

            var project = document.Project;

            bool useDesktopReferences = Platform?.IsDesktop == true;

            var nugetReferences = restoreResult.CompileReferences.Select(x => host.CreateMetadataReference(x));
            var references = useDesktopReferences ? MainViewModel.DesktopReferences.AddRange(nugetReferences) : nugetReferences;
            references = references.Concat(MainViewModel.RoslynHost.DefaultReferences);

            project = project.WithMetadataReferences(references);
            if (restoreResult.Analyzers.Count > 0)
            {
                project = project.WithAnalyzerReferences(GetAnalyzerReferences(restoreResult.Analyzers));
            }

            document = project.GetDocument(DocumentId);

            host.UpdateDocument(document);
            OnDocumentUpdated();

            // for desktop, add System*, including facade assemblies
            _executionHostParameters.FrameworkReferences = useDesktopReferences ? MainViewModel.DesktopReferences : ImmutableArray<MetadataReference>.Empty;
            // compile-time references from NuGet
            _executionHostParameters.NuGetCompileReferences = GetReferences(restoreResult.CompileReferences, host);
            // runtime references from NuGet
            _executionHostParameters.NuGetRuntimeReferences = GetReferences(restoreResult.RuntimeReferences, host);
            // reference directives & default references
            _executionHostParameters.DirectReferences = NuGet.LocalLibraryPaths;

            var task = _executionHost?.Update(_executionHostParameters);

            ImmutableArray<string> GetReferences(IEnumerable<string> references, Roslyn.RoslynHost host)
            {
                return GetReferencePaths(host.DefaultReferences).Concat(references).ToImmutableArray();
            }
        }

        private IEnumerable<AnalyzerReference> GetAnalyzerReferences(IList<string> analyzers)
        {
            var loader = MainViewModel.RoslynHost.GetService<IAnalyzerAssemblyLoader>();
            return analyzers.Select(a => new AnalyzerFileReference(a, loader));
        }

        private void OnDocumentUpdated()
        {
            DocumentUpdated?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler DocumentUpdated;

        public event Action ResultsAvailable;

        private void AddResult(object o)
        {
            AddResult(ResultObject.Create(o, DumpQuotas.Default));
        }

        private void AddResult(IResultObject o)
        {
            _dispatcher.InvokeAsync(() =>
            {
                ResultsInternal?.Add(o);
                ResultsAvailable?.Invoke();
            }, AppDispatcherPriority.Low);
        }

        private void ExecutionHostOnDump(ResultObject result)
        {
            AddResult(result);
        }

        private void ExecutionHostOnError(ExceptionResultObject errorResult)
        {
            _dispatcher.InvokeAsync(() =>
            {
                _onError?.Invoke(errorResult);
                if (errorResult != null)
                {
                    ResultsInternal.Add(errorResult);

                    ResultsAvailable?.Invoke();
                }
            }, AppDispatcherPriority.Low);
        }

        private void ExecutionHostOnCompilationErrors(IList<CompilationErrorResultObject> errors)
        {
            _dispatcher.InvokeAsync(() =>
            {
                foreach (var error in errors)
                {
                    ResultsInternal.Add(error);
                }

                ResultsAvailable?.Invoke();
            });
        }

        private void ExecutionHostOnDisassembled(string il)
        {
            ILText = il;
        }

        public void SetDocument(DocumentViewModel? document)
        {
            Document = document == null ? null : DocumentViewModel.FromPath(document.Path);

            IsDirty = document?.IsAutoSave == true;

            var roslynHost = MainViewModel.RoslynHost;

            _executionHostParameters = new ExecutionHostParameters(
                ImmutableArray<string>.Empty, // will be updated during NuGet restore
                ImmutableArray<string>.Empty,
                ImmutableArray<string>.Empty,
                ImmutableArray<MetadataReference>.Empty,
                roslynHost.DefaultImports,
                roslynHost.DisabledDiagnostics,
                WorkingDirectory,
                MainViewModel.NuGet.GlobalPackageFolder);
            _executionHost = new AssemblyExecutionHost(_executionHostParameters, BuildPath, Document?.Name ?? Id);

            _executionHost.Dumped += ExecutionHostOnDump;
            _executionHost.Error += ExecutionHostOnError;
            _executionHost.CompilationErrors += ExecutionHostOnCompilationErrors;
            _executionHost.Disassembled += ExecutionHostOnDisassembled;

            Platform = AvailablePlatforms.FirstOrDefault(p => p.Name == MainViewModel.Settings.DefaultPlatformName) ??
                       AvailablePlatforms.FirstOrDefault();
        }

        private IEnumerable<string> GetReferencePaths(IEnumerable<MetadataReference> references)
        {
            return references.OfType<PortableExecutableReference>().Select(x => x.FilePath);
        }

        private async Task RenameSymbol()
        {
            var host = MainViewModel.RoslynHost;
            var document = host.GetDocument(DocumentId);
            if (document == null)
            {
                return;
            }

            var symbol = await RenameHelper.GetRenameSymbol(document, _getSelection().Start).ConfigureAwait(true);
            if (symbol == null) return;

            var dialog = _serviceProvider.GetService<IRenameSymbolDialog>();
            dialog.Initialize(symbol.Name);
            await dialog.ShowAsync();
            if (dialog.ShouldRename)
            {
                var newSolution = await Renamer.RenameSymbolAsync(document.Project.Solution, symbol, dialog.SymbolName, null).ConfigureAwait(true);
                var newDocument = newSolution.GetDocument(DocumentId);
                // TODO: possibly update entire solution
                host.UpdateDocument(newDocument);
            }
            OnEditorFocus();
        }

        private enum CommentAction
        {
            Comment,
            Uncomment
        }

        private async Task CommentUncommentSelection(CommentAction action)
        {
            const string singleLineCommentString = "//";

            var document = MainViewModel.RoslynHost.GetDocument(DocumentId);
            if (document == null)
            {
                return;
            }

            var selection = _getSelection();
            var documentText = await document.GetTextAsync().ConfigureAwait(false);
            var changes = new List<TextChange>();
            var lines = documentText.Lines.SkipWhile(x => !x.Span.IntersectsWith(selection))
                .TakeWhile(x => x.Span.IntersectsWith(selection)).ToArray();

            if (action == CommentAction.Comment)
            {
                foreach (var line in lines)
                {
                    if (!string.IsNullOrWhiteSpace(documentText.GetSubText(line.Span).ToString()))
                    {
                        changes.Add(new TextChange(new TextSpan(line.Start, 0), singleLineCommentString));
                    }
                }
            }
            else if (action == CommentAction.Uncomment)
            {
                foreach (var line in lines)
                {
                    var text = documentText.GetSubText(line.Span).ToString();
                    if (text.TrimStart().StartsWith(singleLineCommentString, StringComparison.Ordinal))
                    {
                        changes.Add(new TextChange(new TextSpan(
                            line.Start + text.IndexOf(singleLineCommentString, StringComparison.Ordinal),
                            singleLineCommentString.Length), string.Empty));
                    }
                }
            }

            if (changes.Count == 0) return;

            MainViewModel.RoslynHost.UpdateDocument(document.WithText(documentText.WithChanges(changes)));
            if (action == CommentAction.Uncomment)
            {
                await FormatDocument().ConfigureAwait(false);
            }
        }

        private async Task FormatDocument()
        {
            var document = MainViewModel.RoslynHost.GetDocument(DocumentId);
            var formattedDocument = await Formatter.FormatAsync(document).ConfigureAwait(false);
            MainViewModel.RoslynHost.UpdateDocument(formattedDocument);
        }

        public IReadOnlyList<ExecutionPlatform> AvailablePlatforms { get; }

        public ExecutionPlatform Platform
        {
            get => _platform;
            set
            {
                if (_executionHost == null || value == null) throw new InvalidOperationException();

                if (SetProperty(ref _platform, value))
                {
                    _executionHost.Platform = value;
                    PlatformVersion = value.Versions.FirstOrDefault(p => p.FrameworkVersion.IndexOf("-", StringComparison.Ordinal) < 0) ??
                        value.Versions.FirstOrDefault();

                    if (_isInitialized && !value.HasVersions)
                    {
                        NuGet.SetTargetFramework(value.TargetFrameworkMoniker);
                        RestartHostCommand?.Execute();
                    }
                }
            }
        }

        public PlatformVersion PlatformVersion
        {
            get => _platformVersion;
            set
            {
                if (_executionHost == null) throw new InvalidOperationException();

                if (SetProperty(ref _platformVersion, value))
                {
                    if (value != null)
                    {
                        _executionHost.PlatformVersion = value;

                        if (_isInitialized)
                        {
                            NuGet.SetTargetFramework(value.TargetFrameworkMoniker, value.FrameworkVersion);
                            RestartHostCommand?.Execute();
                        }
                    }
                }
            }
        }

        private async Task RestartHost()
        {
            Reset();
            try
            {
                await Task.Run(() => _executionHost?.ResetAsync()).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _telemetryProvider.ReportError(e);
                throw;
            }
            finally
            {
                SetIsRunning(false);
            }
        }

        private void SetIsRunning(bool value)
        {
            _dispatcher.InvokeAsync(() => IsRunning = value);
        }

        public async Task AutoSave()
        {
            if (!IsDirty) return;
            if (Document == null)
            {
                var index = 1;
                string path;
                do
                {
                    path = Path.Combine(WorkingDirectory, DocumentViewModel.GetAutoSaveName("Program" + index++));
                } while (File.Exists(path));
                Document = DocumentViewModel.FromPath(path);
            }

            await SaveDocument(Document.GetAutoSavePath()).ConfigureAwait(false);
        }

        public void OpenBuildPath()
        {
            Task.Run(() => Process.Start(BuildPath));
        }

        public async Task<SaveResult> Save(bool promptSave)
        {
            if (_isSaving) return SaveResult.Cancel;
            if (!IsDirty && promptSave) return SaveResult.Save;

            _isSaving = true;
            try
            {
                var result = SaveResult.Save;
                if (Document == null || Document.IsAutoSaveOnly)
                {
                    var dialog = _serviceProvider.GetService<ISaveDocumentDialog>();
                    dialog.ShowDontSave = promptSave;
                    dialog.AllowNameEdit = true;
                    dialog.FilePathFactory = s => DocumentViewModel.GetDocumentPathFromName(WorkingDirectory, s);
                    await dialog.ShowAsync();
                    result = dialog.Result;
                    if (result == SaveResult.Save && dialog.DocumentName != null)
                    {
                        Document?.DeleteAutoSave();
                        Document = MainViewModel.AddDocument(dialog.DocumentName);
                        OnPropertyChanged(nameof(Title));
                    }
                }
                else if (promptSave)
                {
                    var dialog = _serviceProvider.GetService<ISaveDocumentDialog>();
                    dialog.ShowDontSave = true;
                    dialog.DocumentName = Document.Name;
                    await dialog.ShowAsync();
                    result = dialog.Result;
                }

                if (result == SaveResult.Save && Document != null)
                {
                    // ReSharper disable once PossibleNullReferenceException
                    await SaveDocument(Document.GetSavePath()).ConfigureAwait(true);
                    IsDirty = false;
                }

                if (result != SaveResult.Cancel)
                {
                    Document?.DeleteAutoSave();
                }

                return result;
            }
            finally
            {
                _isSaving = false;
            }
        }

        private async Task SaveDocument(string path)
        {
            if (DocumentId == null) return;

            var document = MainViewModel.RoslynHost.GetDocument(DocumentId);
            if (document == null)
            {
                return;
            }

            var text = await document.GetTextAsync().ConfigureAwait(false);
            using (var writer = File.CreateText(path))
            {
                for (int lineIndex = 0; lineIndex < text.Lines.Count - 1; ++lineIndex)
                {
                    var lineText = text.Lines[lineIndex].ToString();
                    await writer.WriteLineAsync(lineText).ConfigureAwait(false);
                }
                await writer.WriteAsync(text.Lines[text.Lines.Count - 1].ToString()).ConfigureAwait(false);
            }
        }

        internal void Initialize(DocumentId documentId,
            Action<ExceptionResultObject?> onError,
            Func<TextSpan> getSelection, IDisposable viewDisposable)
        {
            _viewDisposable = viewDisposable;
            _onError = onError;
            _getSelection = getSelection;
            DocumentId = documentId;
            _isInitialized = true;

            if (PlatformVersion != null)
            {
                NuGet.SetTargetFramework(PlatformVersion.TargetFrameworkMoniker, PlatformVersion.FrameworkVersion);
            }
            else
            {
                NuGet.SetTargetFramework(Platform.TargetFrameworkMoniker);
            }

            var task = UpdatePackages();
            RestartHostCommand?.Execute();
        }

        public DocumentId DocumentId { get; private set; }

        public MainViewModelBase MainViewModel { get; }
        public ICommandProvider CommandProvider { get; }

        public NuGetDocumentViewModel NuGet { get; }

        public string Title => Document != null && !Document.IsAutoSaveOnly ? Document.Name : "New";

        public IDelegateCommand OpenBuildPathCommand { get; }

        public IDelegateCommand SaveCommand { get; }

        public IDelegateCommand RunCommand { get; }

        public IDelegateCommand RestartHostCommand { get; }

        public IDelegateCommand FormatDocumentCommand { get; }

        public IDelegateCommand CommentSelectionCommand { get; }

        public IDelegateCommand UncommentSelectionCommand { get; }

        public IDelegateCommand RenameSymbolCommand { get; }

        public bool IsRunning
        {
            get => _isRunning; private set
            {
                if (SetProperty(ref _isRunning, value))
                {
                    _dispatcher.InvokeAsync(() => RunCommand.RaiseCanExecuteChanged());
                }
            }
        }

        private async Task Run()
        {
            if (IsRunning) return;

            Reset();

            await MainViewModel.AutoSaveOpenDocuments().ConfigureAwait(true);

            SetIsRunning(true);

            StartExec();

            if (!ShowIL)
            {
                ILText = DefaultILText;
            }

            var cancellationToken = _cts.Token;
            try
            {
                var code = await GetCode(cancellationToken).ConfigureAwait(true);
                if (_executionHost != null)
                {
                    await _executionHost.ExecuteAsync(code, ShowIL, OptimizationLevel).ConfigureAwait(true);
                }
            }
            catch (CompilationErrorException ex)
            {
                foreach (var diagnostic in ex.Diagnostics)
                {
                    ResultsInternal?.Add(ResultObject.Create(diagnostic, DumpQuotas.Default));
                }
            }
            catch (Exception ex)
            {
                AddResult(ex);
            }
            finally
            {
                SetIsRunning(false);
            }
        }

        private void StartExec()
        {
            ResultsInternal = new ObservableCollection<IResultObject>();
            _onError?.Invoke(null);
        }

        private OptimizationLevel OptimizationLevel => MainViewModel.Settings.OptimizeCompilation ? OptimizationLevel.Release : OptimizationLevel.Debug;

        private async Task UpdatePackages()
        {
            var document = MainViewModel.RoslynHost.GetDocument(DocumentId);
            if (document == null)
            {
                return;
            }

            var syntaxRoot = await document.GetSyntaxRootAsync().ConfigureAwait(true);
            var libraries = ParseReferences(syntaxRoot);

            var defaultReferences = MainViewModel.RoslynHost.DefaultReferences;
            if (defaultReferences.Length > 0)
            {
                if (libraries == null)
                {
                    libraries = new List<LibraryRef>();
                }

                libraries.AddRange(GetReferencePaths(defaultReferences).Select(p => new LibraryRef(p)));
            }

            NuGet.UpdateLibraries((IReadOnlyList<LibraryRef>?)libraries ?? Array.Empty<LibraryRef>());
        }

        private List<LibraryRef>? ParseReferences(SyntaxNode syntaxRoot)
        {
            const string NuGetPrefix = "nuget:";
            const string LegacyNuGetPrefix = "$NuGet\\";

            if (!(syntaxRoot is Microsoft.CodeAnalysis.CSharp.Syntax.CompilationUnitSyntax compilation))
            {
                return null;
            }

            List<LibraryRef>? libraries = null;

            foreach (var directive in compilation.GetReferenceDirectives())
            {
                var value = directive.File.ValueText;
                string? id, version;

                if (HasPrefix(NuGetPrefix, value))
                {
                    (id, version) = ParseNuGetReference(NuGetPrefix, value);
                }
                else if (HasPrefix(LegacyNuGetPrefix, value))
                {
                    (id, version) = ParseLegacyNuGetReference(value);
                    if (id == null)
                    {
                        continue;
                    }
                }
                else
                {
                    if (IsLocalReference(value))
                    {
                        if (libraries == null) libraries = new List<LibraryRef>();
                        libraries.Add(new LibraryRef(value));
                    }

                    continue;
                }

                VersionRange versionRange;
                if (version == string.Empty)
                {
                    versionRange = VersionRange.All;
                }
                else if (!VersionRange.TryParse(version, out versionRange))
                {
                    continue;
                }

                if (libraries == null) libraries = new List<LibraryRef>();
                libraries.Add(new LibraryRef(id, versionRange));
            }

            return libraries;

            // local functions

            bool HasPrefix(string prefix, string value)
            {
                return value.Length > prefix.Length &&
                       value.StartsWith(prefix, StringComparison.InvariantCultureIgnoreCase);
            }

            (string id, string version) ParseNuGetReference(string prefix, string value)
            {
                string id, version;

                var indexOfSlash = value.IndexOf('/');
                if (indexOfSlash >= 0)
                {
                    id = value.Substring(prefix.Length, indexOfSlash - prefix.Length);
                    version = indexOfSlash != value.Length - 1 ? value.Substring(indexOfSlash + 1) : string.Empty;
                }
                else
                {
                    id = value.Substring(prefix.Length);
                    version = string.Empty;
                }

                return (id, version);
            }

            (string? id, string? version) ParseLegacyNuGetReference(string value)
            {
                var split = value.Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
                if (split.Length >= 3)
                {
                    return (split[1], split[2]);
                }

                return (null, null);
            }

            bool IsLocalReference(string path)
            {
                switch (Path.GetExtension(path)?.ToLowerInvariant())
                {
                    // add a "project" reference if it's not a GAC reference
                    case ".dll":
                    case ".exe":
                    case ".winmd":
                        return true;
                }

                return false;
            }
        }

        private async Task<string> GetCode(CancellationToken cancellationToken)
        {
            var document = MainViewModel.RoslynHost.GetDocument(DocumentId);
            if (document == null)
            {
                return string.Empty;
            }

            return (await document.GetTextAsync(cancellationToken)
                .ConfigureAwait(false)).ToString();
        }

        private void Reset()
        {
            if (_cts != null)
            {
                _cts.Cancel();
                _cts.Dispose();
            }
            _cts = new CancellationTokenSource();
        }

        public async Task<string> LoadText()
        {
            if (Document == null)
            {
                return string.Empty;
            }
            using (var fileStream = File.OpenText(Document.Path))
            {
                return await fileStream.ReadToEndAsync().ConfigureAwait(false);
            }
        }

        public void Close()
        {
            _viewDisposable?.Dispose();
            _executionHost?.Dispose();
            _executionHost = null;
        }

        public bool IsDirty
        {
            get => _isDirty;
            private set => SetProperty(ref _isDirty, value);
        }

        public bool ShowIL { get; set; }

        public event EventHandler EditorFocus;

        private void OnEditorFocus()
        {
            EditorFocus?.Invoke(this, EventArgs.Empty);
        }

        public void OnTextChanged()
        {
            IsDirty = true;

            if (IsLiveMode)
            {
                _liveModeTimer?.Change(MainViewModel.Settings.LiveModeDelayMs, Timeout.Infinite);
            }

            var task = UpdatePackages();
        }
    }
}