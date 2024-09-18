using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Composition;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NuGet.Packaging;
using RoslynPad.Build;
using RoslynPad.Roslyn.Rename;
using RoslynPad.Utilities;

namespace RoslynPad.UI;

[Export]
public class OpenDocumentViewModel : NotificationObject, IDisposable
{
    private const string DefaultDocumentName = "New";
    private const string RegularFileExtension = ".cs";
    private const string ScriptFileExtension = ".csx";
    private const string DefaultILText = "// Run to view IL";

    private readonly IServiceProvider _serviceProvider;
    private readonly IAppDispatcher _dispatcher;
    private readonly ITelemetryProvider _telemetryProvider;
    private readonly ILogger<OpenDocumentViewModel> _logger;
    private readonly IPlatformsFactory _platformsFactory;
    private readonly ObservableCollection<IResultObject> _results;
    private readonly List<RestoreResultObject> _restoreResults;

    private ExecutionHost? _executionHost;
    private ExecutionHostParameters? _executionHostParameters;
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
    private SourceCodeKind? _sourceCodeKind;
    private string? _selectedText;

    public string Id { get; }
    public string BuildPath { get; }

    public string WorkingDirectory => Document != null
        ? Path.GetDirectoryName(Document.Path)!
        : MainViewModel.DocumentRoot.Path;

    public string? SelectedText
    {
        get => _selectedText;
        set => SetProperty(ref _selectedText, value);
    }

    public IEnumerable<IResultObject> Results => _results;

    public IDelegateCommand ToggleLiveModeCommand { get; }
    public IDelegateCommand SetDefaultPlatformCommand { get; }

    public bool IsLiveMode
    {
        get => _isLiveMode;
        private set
        {
            if (!SetProperty(ref _isLiveMode, value)) return;
            RunCommand.RaiseCanExecuteChanged();

            if (value)
            {
                _ = RunAsync();

                _liveModeTimer ??= new Timer(o => _dispatcher.InvokeAsync(() =>
                {
                    _ = RunAsync();
                }), state: null, Timeout.Infinite, Timeout.Infinite);
            }
        }
    }

    public SourceCodeKind SourceCodeKind
    {
        get
        {
            if (_sourceCodeKind is not null)
            {
                return _sourceCodeKind.Value;
            }

            var isScript = Path.GetExtension(Document?.Name)?.Equals(ScriptFileExtension, StringComparison.OrdinalIgnoreCase);
            return isScript is null
                ? throw new InvalidOperationException("Document not initialized")
                : (_sourceCodeKind ??= isScript == true ? SourceCodeKind.Script : SourceCodeKind.Regular);
        }
        set => _sourceCodeKind = value;
    }

    private string GetFileExtension() =>
        SourceCodeKind == SourceCodeKind.Script ? ScriptFileExtension : RegularFileExtension;

    public DocumentViewModel? Document
    {
        get => _document;
        private set => SetProperty(ref _document, value);
    }

    public string ILText
    {
        get => _ilText ?? string.Empty;
        private set => SetProperty(ref _ilText, value);
    }

    [ImportingConstructor]
    public OpenDocumentViewModel(IServiceProvider serviceProvider, MainViewModel mainViewModel, ICommandProvider commands, IAppDispatcher appDispatcher, ITelemetryProvider telemetryProvider, ILogger<OpenDocumentViewModel> logger)
    {
        Id = Guid.NewGuid().ToString("n");
        BuildPath = Path.Combine(Path.GetTempPath(), "roslynpad", "build", Id);
        Directory.CreateDirectory(BuildPath);

        _telemetryProvider = telemetryProvider;
        _logger = logger;
        _platformsFactory = serviceProvider.GetRequiredService<IPlatformsFactory>();
        _serviceProvider = serviceProvider;
        _results = [];
        _restoreResults = [];

        MainViewModel = mainViewModel;
        CommandProvider = commands;

        NuGet = serviceProvider.GetRequiredService<NuGetDocumentViewModel>();

        _restoreSuccessful = true; // initially set to true so we can immediately start running and wait for restore
        _dispatcher = appDispatcher;

        OpenBuildPathCommand = commands.Create(OpenBuildPath);
        SaveCommand = commands.CreateAsync(() => SaveAsync(promptSave: false));
        RunCommand = commands.CreateAsync(RunAsync, () => !IsRunning && RestoreSuccessful && Platform != null);
        TerminateCommand = commands.CreateAsync(TerminateAsync, () => Platform != null);
        FormatDocumentCommand = commands.CreateAsync(FormatDocumentAsync);
        CommentSelectionCommand = commands.CreateAsync(() => CommentUncommentSelectionAsync(CommentAction.Comment));
        UncommentSelectionCommand = commands.CreateAsync(() => CommentUncommentSelectionAsync(CommentAction.Uncomment));
        RenameSymbolCommand = commands.CreateAsync(RenameSymbolAsync);
        ToggleLiveModeCommand = commands.Create(() => IsLiveMode = !IsLiveMode);
        SetDefaultPlatformCommand = commands.Create(SetDefaultPlatform);

        ILText = DefaultILText;

        InitializePlatforms();
    }

    [MemberNotNull(nameof(_executionHost))]
    private void InitializeExecutionHost()
    {
        var roslynHost = MainViewModel.RoslynHost;

        _executionHostParameters = new ExecutionHostParameters(
            BuildPath,
            _serviceProvider.GetRequiredService<NuGetViewModel>().ConfigPath,
            roslynHost.DefaultImports,
            roslynHost.DisabledDiagnostics,
            WorkingDirectory,
            SourceCodeKind);

        _executionHost = new ExecutionHost(_executionHostParameters, roslynHost, _logger)
        {
            Name = Document?.Name ?? "Untitled",
            DocumentId = DocumentId,
            Platform = Platform.NotNull(),
            DotNetExecutable = _platformsFactory.DotNetExecutable
        };

        _executionHost.Dumped += ExecutionHostOnDump;
        _executionHost.Error += ExecutionHostOnError;
        _executionHost.ReadInput += ExecutionHostOnInputRequest;
        _executionHost.CompilationErrors += ExecutionHostOnCompilationErrors;
        _executionHost.Disassembled += ExecutionHostOnDisassembled;
        _executionHost.RestoreStarted += OnRestoreStarted;
        _executionHost.RestoreCompleted += OnRestoreCompleted;
        _executionHost.ProgressChanged += p => ReportedProgress = p.Progress;
    }

    private void SetDefaultPlatform()
    {
        if (Platform is not null)
        {
            MainViewModel.Settings.DefaultPlatformName = Platform.ToString();
        }
    }

    private void InitializePlatforms()
    {
        AvailablePlatforms = _platformsFactory.GetExecutionPlatforms();
    }

    private void OnRestoreStarted() => IsRestoring = true;

    private void OnRestoreCompleted(RestoreResult restoreResult)
    {
        if (_executionHost is null)
        {
            return;
        }

        IsRestoring = false;

        lock (_results)
        {
            _restoreResults.Clear();
            ClearResults();
        }

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
                AddRestoreResult(new RestoreResultObject(error, "Error"));
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
                _dispatcher.InvokeAsync(RunCommand.RaiseCanExecuteChanged);
            }
        }
    }

    private void OnDocumentUpdated() => DocumentUpdated?.Invoke(this, EventArgs.Empty);

    public event EventHandler? DocumentUpdated;

    public event Action? ReadInput;

    public event Action? ResultsAvailable;

    private void AddResult(IResultObject o)
    {
        lock (_results)
        {
            _results.Add(o);
        }

        ResultsAvailable?.Invoke();
    }

    private void AddRestoreResult(RestoreResultObject o)
    {
        lock (_results)
        {
            _restoreResults.Add(o);
            AddResult(o);
        }
    }

    private void ExecutionHostOnInputRequest() => _dispatcher.InvokeAsync(() =>
    {
        ReadInput?.Invoke();
    }, AppDispatcherPriority.Low);

    private void ExecutionHostOnDump(ResultObject result) => AddResult(result);

    private void ExecutionHostOnError(ExceptionResultObject errorResult) => _dispatcher.InvokeAsync(() =>
    {
        _onError?.Invoke(errorResult);
        if (errorResult != null)
        {
            AddResult(errorResult);
        }
    }, AppDispatcherPriority.Low);

    private void ExecutionHostOnCompilationErrors(IList<CompilationErrorResultObject> errors)
    {
        foreach (var error in errors)
        {
            AddResult(error);
        }
    }

    private void ExecutionHostOnDisassembled(string il) => ILText = il;

    public void SetDocument(DocumentViewModel? document)
    {
        Document = document == null ? null : DocumentViewModel.FromPath(document.Path);

        IsDirty = document?.IsAutoSave == true;
    }

    public void SendInput(string input) => _ = _executionHost?.SendInputAsync(input);

    private async Task RenameSymbolAsync()
    {
        var host = MainViewModel.RoslynHost;
        var document = host.GetDocument(DocumentId);
        if (document == null || _getSelection == null)
        {
            return;
        }

        var symbol = await RenameHelper.GetRenameSymbol(document, _getSelection().Start).ConfigureAwait(true);
        if (symbol == null) return;

        var dialog = _serviceProvider.GetRequiredService<IRenameSymbolDialog>();
        dialog.Initialize(symbol.Name);
        await dialog.ShowAsync().ConfigureAwait(true);
        if (dialog.ShouldRename)
        {
            var newSolution = await Renamer.RenameSymbolAsync(document.Project.Solution, symbol, new SymbolRenameOptions(), dialog.SymbolName ?? string.Empty).ConfigureAwait(true);
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

    private async Task CommentUncommentSelectionAsync(CommentAction action)
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
            await FormatDocumentAsync().ConfigureAwait(false);
        }
    }

    private async Task FormatDocumentAsync()
    {
        var document = MainViewModel.RoslynHost.GetDocument(DocumentId);
        var formattedDocument = await Formatter.FormatAsync(document!).ConfigureAwait(false);
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
                if (_executionHost is not null)
                {
                    _executionHost.Platform = value;
                }

                UpdatePackages();

                RunCommand.RaiseCanExecuteChanged();
                TerminateCommand.RaiseCanExecuteChanged();

                if (_isInitialized)
                {
                    TerminateCommand.Execute();
                }
            }
        }
    }

    private async Task TerminateAsync()
    {
        ResetCancellation();
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

    private void SetIsRunning(bool value) => _dispatcher.InvokeAsync(() => IsRunning = value);

    public async Task AutoSaveAsync()
    {
        if (!IsDirty) return;

        var document = Document;

        if (document == null)
        {
            var index = 1;
            string path;

            do
            {
                path = Path.Combine(WorkingDirectory, DocumentViewModel.GetAutoSaveName(("Program" + index++) + GetFileExtension()));
            }
            while (File.Exists(path));

            document = DocumentViewModel.FromPath(path);
        }

        Document = document;

        await SaveDocumentAsync(Document.GetAutoSavePath()).ConfigureAwait(false);
    }

    public void OpenBuildPath() => _ = Task.Run(() =>
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

    public async Task<SaveResult> SaveAsync(bool promptSave)
    {
        if (_isSaving) return SaveResult.Cancel;
        if (!IsDirty && promptSave) return SaveResult.Save;

        _isSaving = true;
        try
        {
            var result = SaveResult.Save;
            if (Document == null || Document.IsAutoSaveOnly)
            {
                var dialog = _serviceProvider.GetRequiredService<ISaveDocumentDialog>();
                dialog.ShowDoNotSave = promptSave;
                dialog.AllowNameEdit = true;
                dialog.FilePathFactory = name => DocumentViewModel.GetDocumentPathFromName(WorkingDirectory, name);
                await dialog.ShowAsync().ConfigureAwait(true);
                result = dialog.Result;
                if (result == SaveResult.Save && dialog.DocumentName != null)
                {
                    Document?.DeleteAutoSave();
                    Document = MainViewModel.AddDocument(dialog.DocumentName + GetFileExtension());
                    OnPropertyChanged(nameof(Title));
                }
            }
            else if (promptSave)
            {
                var dialog = _serviceProvider.GetRequiredService<ISaveDocumentDialog>();
                dialog.ShowDoNotSave = true;
                dialog.DocumentName = Document.Name;
                await dialog.ShowAsync().ConfigureAwait(true);
                result = dialog.Result;
            }

            if (result == SaveResult.Save && Document != null)
            {
                await SaveDocumentAsync(Document.GetSavePath()).ConfigureAwait(true);
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

    private async Task SaveDocumentAsync(string path)
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

        Platform = AvailablePlatforms.FirstOrDefault(p => p.ToString() == MainViewModel.Settings.DefaultPlatformName) ??
                   AvailablePlatforms.FirstOrDefault();

        InitializeExecutionHost();

        _isInitialized = true;

        UpdatePackages();

        TerminateCommand?.Execute();
    }

    public DocumentId DocumentId
    {
        get => _documentId ?? throw new ArgumentNullException(nameof(_documentId));
        private set => _documentId = value;
    }

    public bool HasDocumentId => _documentId is not null;

    public MainViewModel MainViewModel { get; }
    public ICommandProvider CommandProvider { get; }
    public NuGetDocumentViewModel NuGet { get; }
    public string Title => Document != null && !Document.IsAutoSaveOnly ? Document.Name : DefaultDocumentName + GetFileExtension();
    public IDelegateCommand OpenBuildPathCommand { get; }
    public IDelegateCommand SaveCommand { get; }
    public IDelegateCommand RunCommand { get; }
    public IDelegateCommand TerminateCommand { get; }
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
                _dispatcher.InvokeAsync(RunCommand.RaiseCanExecuteChanged);
            }
        }
    }

    private async Task RunAsync()
    {
        if (IsRunning || _executionHost is null || _executionHostParameters is null)
        {
            return;
        }

        ReportedProgress = null;

        var cancellationToken = ResetCancellation();

        await MainViewModel.AutoSaveOpenDocuments().ConfigureAwait(true);

        var documentPath = IsDirty ? Document?.GetAutoSavePath() : Document?.Path;
        if (documentPath is null)
        {
            return;
        }

        SetIsRunning(true);

        StartExec();

        if (!ShowIL)
        {
            ILText = DefaultILText;
        }

        try
        {
            if (_executionHost is not null && _executionHostParameters is not null)
            {
                // Make sure the execution working directory matches the current script path
                // which may have changed since we loaded.
                if (_executionHostParameters.WorkingDirectory != WorkingDirectory)
                    _executionHostParameters.WorkingDirectory = WorkingDirectory;

                await _executionHost.ExecuteAsync(documentPath, ShowIL, OptimizationLevel, cancellationToken).ConfigureAwait(true);
            }
        }
        catch (CompilationErrorException ex)
        {
            foreach (var diagnostic in ex.Diagnostics)
            {
                var startLinePosition = diagnostic.Location.GetLineSpan().StartLinePosition;
                AddResult(CompilationErrorResultObject.Create(diagnostic.Severity.ToString(), diagnostic.Id, diagnostic.GetMessage(CultureInfo.InvariantCulture), startLinePosition.Line, startLinePosition.Character));
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            AddResult(new ExceptionResultObject { Value = ex.ToString() });
        }
        finally
        {
            SetIsRunning(false);
            ReportedProgress = null;
        }
    }

    private void StartExec()
    {
        ClearResults();

        _onError?.Invoke(null);
    }

    private void ClearResults()
    {
        lock (_results)
        {
            _results.Clear();
            _results.AddRange(_restoreResults);
        }
    }

    private OptimizationLevel OptimizationLevel => MainViewModel.Settings.OptimizeCompilation ? OptimizationLevel.Release : OptimizationLevel.Debug;

    private void UpdatePackages(bool alwaysRestore = true) =>
        _ = _executionHost?.UpdateReferencesAsync(alwaysRestore);

    private async Task<string> GetCodeAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(SelectedText))
        {
            return SelectedText;
        }

        var document = MainViewModel.RoslynHost.GetDocument(DocumentId);
        if (document == null)
        {
            return string.Empty;
        }

        return (await document.GetTextAsync(cancellationToken)
            .ConfigureAwait(false)).ToString();
    }

    private CancellationToken ResetCancellation()
    {
        if (_runCts != null)
        {
            _runCts.Cancel();
            _runCts.Dispose();
        }

        var runCts = new CancellationTokenSource();
        _runCts = runCts;
        return runCts.Token;
    }

    public async Task<string> LoadTextAsync()
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

        UpdatePackages(alwaysRestore: false);
    }

    public void Dispose()
    {
        _runCts?.Dispose();
    }

    public event Action<(int line, int column)>? EditorChangeLocation;

    public void TryJumpToLine(IResultWithLineNumber result)
    {
        if (result.LineNumber is { } lineNumber)
        {
            EditorChangeLocation?.Invoke((lineNumber, result.Column));
        }
    }
}
