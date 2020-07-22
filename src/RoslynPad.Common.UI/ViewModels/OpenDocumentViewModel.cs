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
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Text;
using NuGet.Versioning;
using RoslynPad.Build;
using RoslynPad.NuGet;
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
        private readonly IPlatformsFactory _platformsFactory;
        private readonly IExecutionHost _executionHost;
        private readonly ObservableCollection<IResultObject> _results;
        private readonly ExecutionHostParameters _executionHostParameters;
        private CancellationTokenSource? _restoreCts;
        private CancellationTokenSource? _runCts;
        private bool _isRunning;
        private bool _isDirty;
        private ExecutionPlatform? _platform;
        private bool _isSaving;
        private IDisposable? _viewDisposable;
        private Action<ExceptionResultObject?>? _onError;
        private Func<TextSpan>? _getSelection;
        private string? _ilText;
        private bool _isInitialized;
        private bool _isLiveMode;
        private Timer? _liveModeTimer;
        private DocumentViewModel? _document;
        private bool _isRestoring;
        private IReadOnlyList<ExecutionPlatform>? _availablePlatforms;
        private DocumentId? _documentId;
        private bool _restoreSuccessful;
        private double? _reportedProgress;

        public string Id { get; }
        public string BuildPath { get; }

        public string WorkingDirectory => Document != null
            ? Path.GetDirectoryName(Document.Path)!
            : MainViewModel.DocumentRoot.Path;

        public IEnumerable<object> Results => _results;
        internal IEnumerable<IResultObject> ResultsInternal => _results;

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
                    _ = Run();

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

        public DocumentViewModel? Document
        {
            get => _document;
            private set
            {
                if (_document != value)
                {
                    _document = value;

                    if (_executionHost != null && value != null)
                    {
                        _executionHost.Name = value.Name;
                    }
                }
            }
        }

        public string ILText
        {
            get => _ilText ?? string.Empty;
            private set => SetProperty(ref _ilText, value);
        }

        [ImportingConstructor]
        public OpenDocumentViewModel(IServiceProvider serviceProvider, MainViewModelBase mainViewModel, ICommandProvider commands, IAppDispatcher appDispatcher, ITelemetryProvider telemetryProvider)
        {
            Id = Guid.NewGuid().ToString("n");
            BuildPath = Path.Combine(Path.GetTempPath(), "roslynpad", "build", Id);
            Directory.CreateDirectory(BuildPath);

            _telemetryProvider = telemetryProvider;
            _platformsFactory = serviceProvider.GetService<IPlatformsFactory>();
            _serviceProvider = serviceProvider;
            _results = new ObservableCollection<IResultObject>();

            MainViewModel = mainViewModel;
            CommandProvider = commands;

            NuGet = serviceProvider.GetService<NuGetDocumentViewModel>();

            _restoreSuccessful = true; // initially set to true so we can immediately start running and wait for restore
            _dispatcher = appDispatcher;
            _platformsFactory.Changed += InitializePlatforms;

            OpenBuildPathCommand = commands.Create(() => OpenBuildPath());
            SaveCommand = commands.CreateAsync(() => Save(promptSave: false));
            RunCommand = commands.CreateAsync(Run, () => !IsRunning && RestoreSuccessful && Platform != null);
            RestartHostCommand = commands.CreateAsync(RestartHost, () => Platform != null);
            FormatDocumentCommand = commands.CreateAsync(FormatDocument);
            CommentSelectionCommand = commands.CreateAsync(() => CommentUncommentSelection(CommentAction.Comment));
            UncommentSelectionCommand = commands.CreateAsync(() => CommentUncommentSelection(CommentAction.Uncomment));
            RenameSymbolCommand = commands.CreateAsync(RenameSymbol);
            ToggleLiveModeCommand = commands.Create(() => IsLiveMode = !IsLiveMode);

            ILText = DefaultILText;

            var roslynHost = MainViewModel.RoslynHost;

            _executionHostParameters = new ExecutionHostParameters(
                BuildPath,
                serviceProvider.GetService<NuGetViewModel>().ConfigPath,
                roslynHost.DefaultImports,
                roslynHost.DisabledDiagnostics,
                WorkingDirectory);
            _executionHost = new ExecutionHost(_executionHostParameters, roslynHost);

            _executionHost.Dumped += ExecutionHostOnDump;
            _executionHost.Error += ExecutionHostOnError;
            _executionHost.ReadInput += ExecutionHostOnInputRequest;
            _executionHost.CompilationErrors += ExecutionHostOnCompilationErrors;
            _executionHost.Disassembled += ExecutionHostOnDisassembled;
            _executionHost.RestoreStarted += OnRestoreStarted;
            _executionHost.RestoreCompleted += OnRestoreCompleted;
            _executionHost.RestoreMessage += AddResult;
            _executionHost.ProgressChanged += p => ReportedProgress = p.Progress;

            InitializePlatforms();
        }

        private void InitializePlatforms()
        {
            AvailablePlatforms = _platformsFactory.GetExecutionPlatforms().ToImmutableArray();
            _executionHost.DotNetExecutable = _platformsFactory.DotNetExecutable;
        }

        private void OnRestoreStarted()
        {
            IsRestoring = true;
        }

        private void OnRestoreCompleted(RestoreResult restoreResult)
        {
            IsRestoring = false;

            ClearResults(t => t is RestoreResultObject);

            if (restoreResult.Success)
            {
                var host = MainViewModel.RoslynHost;
                var document = host.GetDocument(DocumentId);
                if (document == null)
                {
                    return;
                }

                var project = document.Project;

                project = project
                    .WithMetadataReferences(_executionHost.MetadataReferences)
                    .WithAnalyzerReferences(_executionHost.Analyzers);

                document = project.GetDocument(DocumentId);

                host.UpdateDocument(document!);
                OnDocumentUpdated();
            }
            else
            {
                foreach (var error in restoreResult.Errors)
                {
                    AddResult(new RestoreResultObject(error, "Error"));
                }
            }

            RestoreSuccessful = restoreResult.Success;
        }

        public bool IsRestoring
        {
            get => _isRestoring;
            private set => SetProperty(ref _isRestoring, value);
        }

        public bool RestoreSuccessful
        {
            get => _restoreSuccessful;
            private set
            {
                if (SetProperty(ref _restoreSuccessful, value))
                {
                    _dispatcher.InvokeAsync(() => RunCommand.RaiseCanExecuteChanged());
                }
            }
        }

        private void OnDocumentUpdated()
        {
            DocumentUpdated?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler? DocumentUpdated;

        public event Action? ReadInput;

        public event Action? ResultsAvailable;

        private void AddResult(object o)
        {
            AddResult(ResultObject.Create(o, DumpQuotas.Default));
        }

        private void AddResult(IResultObject o)
        {
            _dispatcher.InvokeAsync(() =>
            {
                _results.Add(o);
                ResultsAvailable?.Invoke();
            }, AppDispatcherPriority.Low);
        }

        private void ExecutionHostOnInputRequest()
        {
            _dispatcher.InvokeAsync(() =>
            {
                ReadInput?.Invoke();
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
                    _results.Add(errorResult);

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
                    _results.Add(error);
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

            _executionHost.Name = Document?.Name ?? "Untitled";
        }

        public void SendInput(string input)
        {
            _ = _executionHost?.SendInputAsync(input);
        }

        private IEnumerable<string> GetReferencePaths(IEnumerable<MetadataReference> references)
        {
            return references.OfType<PortableExecutableReference>().Select(x => x.FilePath).Where(x => x != null)!;
        }

        private async Task RenameSymbol()
        {
            var host = MainViewModel.RoslynHost;
            var document = host.GetDocument(DocumentId);
            if (document == null || _getSelection == null)
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
                var newSolution = await Renamer.RenameSymbolAsync(document.Project.Solution, symbol, dialog.SymbolName ?? string.Empty,
                    document.Project.Solution.Options).ConfigureAwait(true);
                var newDocument = newSolution.GetDocument(DocumentId);
                // TODO: possibly update entire solution
                host.UpdateDocument(newDocument!);
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

            if (_getSelection == null)
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
            if (action == CommentAction.Uncomment && MainViewModel.Settings.FormatDocumentOnComment)
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

        public IReadOnlyList<ExecutionPlatform> AvailablePlatforms
        {
            get => _availablePlatforms ?? throw new ArgumentNullException(nameof(_availablePlatforms));
            private set => SetProperty(ref _availablePlatforms, value);
        }

        public ExecutionPlatform? Platform
        {
            get => _platform;
            set
            {
                if (value == null) throw new InvalidOperationException();

                if (SetProperty(ref _platform, value))
                {
                    _executionHost.Platform = value;

                    RunCommand.RaiseCanExecuteChanged();
                    RestartHostCommand.RaiseCanExecuteChanged();

                    if (_isInitialized)
                    {
                        RestartHostCommand.Execute();
                    }
                }
            }
        }

        private async Task RestartHost()
        {
            Reset();
            try
            {
                await Task.Run(() => _executionHost?.TerminateAsync()).ConfigureAwait(false);
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

            var document = Document;

            if (document == null)
            {
                var index = 1;
                string path;

                do
                {
                    path = Path.Combine(WorkingDirectory, DocumentViewModel.GetAutoSaveName("Program" + index++));
                }
                while (File.Exists(path));

                document = DocumentViewModel.FromPath(path);
            }

            Document = document;

            await SaveDocument(Document.GetAutoSavePath()).ConfigureAwait(false);
        }

        public void OpenBuildPath()
        {
            Task.Run(() =>
            {
                try
                {
                    Process.Start(new ProcessStartInfo(new Uri("file://" + BuildPath).ToString()) { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    _telemetryProvider.ReportError(ex);
                }
            });
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
            if (!_isInitialized) return;

            var document = MainViewModel.RoslynHost.GetDocument(DocumentId);
            if (document == null)
            {
                return;
            }

            var text = await document.GetTextAsync().ConfigureAwait(false);

            using var writer = File.CreateText(path);
            for (int lineIndex = 0; lineIndex < text.Lines.Count - 1; ++lineIndex)
            {
                var lineText = text.Lines[lineIndex].ToString();
                await writer.WriteLineAsync(lineText).ConfigureAwait(false);
            }

            await writer.WriteAsync(text.Lines[text.Lines.Count - 1].ToString()).ConfigureAwait(false);
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

            Platform = AvailablePlatforms.FirstOrDefault(p => p.Name == MainViewModel.Settings.DefaultPlatformName) ??
                       AvailablePlatforms.FirstOrDefault();

            UpdatePackages();

            RestartHostCommand?.Execute();
        }

        public DocumentId DocumentId
        {
            get => _documentId ?? throw new ArgumentNullException(nameof(_documentId));
            private set => _documentId = value;
        }

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
            get => _isRunning;
            private set
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

            ReportedProgress = null;

            Reset();

            await MainViewModel.AutoSaveOpenDocuments().ConfigureAwait(true);

            SetIsRunning(true);

            StartExec();

            if (!ShowIL)
            {
                ILText = DefaultILText;
            }

            var cancellationToken = _runCts!.Token;
            try
            {
                var code = await GetCode(cancellationToken).ConfigureAwait(true);
                if (_executionHost != null)
                {
                    // Make sure the execution working directory matches the current script path
                    // which may have changed since we loaded.
                    if (_executionHostParameters.WorkingDirectory != WorkingDirectory)
                        _executionHostParameters.WorkingDirectory = WorkingDirectory;

                    await _executionHost.ExecuteAsync(code, ShowIL, OptimizationLevel).ConfigureAwait(true);
                }
            }
            catch (CompilationErrorException ex)
            {
                foreach (var diagnostic in ex.Diagnostics)
                {
                    _results.Add(ResultObject.Create(diagnostic, DumpQuotas.Default));
                }
            }
            catch (Exception ex)
            {
                AddResult(ex);
            }
            finally
            {
                SetIsRunning(false);
                ReportedProgress = null;
            }
        }

        private void StartExec()
        {
            ClearResults(t => !(t is RestoreResultObject));

            _onError?.Invoke(null);
        }

        private void ClearResults(Func<IResultObject, bool> filter)
        {
            _dispatcher.InvokeAsync(() =>
            {
                foreach (var result in _results.Where(filter).ToArray())
                {
                    _results.Remove(result);
                }
            });
        }

        private OptimizationLevel OptimizationLevel => MainViewModel.Settings.OptimizeCompilation ? OptimizationLevel.Release : OptimizationLevel.Debug;

        private void UpdatePackages()
        {
            _restoreCts?.Cancel();
            _restoreCts = new CancellationTokenSource();
            _ = UpdatePackagesAsync(_restoreCts.Token);

            async Task UpdatePackagesAsync(CancellationToken cancellationToken)
            {
                var document = MainViewModel.RoslynHost.GetDocument(DocumentId);
                if (document == null)
                {
                    return;
                }

                var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var libraries = ParseReferences(syntaxRoot!);

                var defaultReferences = MainViewModel.RoslynHost.DefaultReferences;
                if (defaultReferences.Length > 0)
                {
                    libraries.AddRange(GetReferencePaths(defaultReferences).Select(p => LibraryRef.Reference(p)));
                }

                _executionHost.UpdateLibraries(libraries);
            }
        }

        private List<LibraryRef> ParseReferences(SyntaxNode syntaxRoot)
        {
            const string NuGetPrefix = "nuget:";
            const string LegacyNuGetPrefix = "$NuGet\\";
            const string FxPrefix = "framework:";

            var libraries = new List<LibraryRef>();

            if (!(syntaxRoot is Microsoft.CodeAnalysis.CSharp.Syntax.CompilationUnitSyntax compilation))
            {
                return libraries;
            }

            foreach (var directive in compilation.GetReferenceDirectives())
            {
                var value = directive.File.ValueText;
                string? id, version;

                if (HasPrefix(FxPrefix, value))
                {
                    libraries.Add(LibraryRef.FrameworkReference(
                        value.Substring(FxPrefix.Length, value.Length - FxPrefix.Length)));
                    continue;
                }

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
                    libraries.Add(LibraryRef.Reference(value));

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

                libraries.Add(LibraryRef.PackageReference(id, version ?? string.Empty));
            }

            return libraries;

            // local functions

            static bool HasPrefix(string prefix, string value)
            {
                return value.Length > prefix.Length &&
                       value.StartsWith(prefix, StringComparison.InvariantCultureIgnoreCase);
            }

            static (string id, string version) ParseNuGetReference(string prefix, string value)
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

            static (string? id, string? version) ParseLegacyNuGetReference(string value)
            {
                var split = value.Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
                if (split.Length >= 3)
                {
                    return (split[1], split[2]);
                }

                return (null, null);
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
            if (_runCts != null)
            {
                _runCts.Cancel();
                _runCts.Dispose();
            }
            _runCts = new CancellationTokenSource();
        }

        public async Task<string> LoadText()
        {
            if (Document == null)
            {
                return string.Empty;
            }

            using var fileStream = File.OpenText(Document.Path);
            return await fileStream.ReadToEndAsync().ConfigureAwait(false);
        }

        public void Close()
        {
            _viewDisposable?.Dispose();
        }

        public bool IsDirty
        {
            get => _isDirty;
            private set => SetProperty(ref _isDirty, value);
        }

        public double? ReportedProgress
        {
            get => _reportedProgress;
            private set
            {
                if (_reportedProgress != value)
                {
                    SetProperty(ref _reportedProgress, value);
                    OnPropertyChanged(nameof(HasReportedProgress));
                }
            }
        }

        public bool HasReportedProgress => ReportedProgress.HasValue;

        public bool ShowIL { get; set; }

        public event EventHandler? EditorFocus;

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

            UpdatePackages();
        }
    }
}
