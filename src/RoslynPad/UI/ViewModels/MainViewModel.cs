using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Styling;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using System.Composition;
using RoslynPad.Build;
using RoslynPad.Roslyn;
using RoslynPad.Themes;
using RoslynPad.Utilities;

namespace RoslynPad.UI;

[Export(typeof(MainViewModel)), Shared]
public class MainViewModel : NotificationObject, IDisposable
{
    private static readonly Version s_currentVersion = Assembly.GetEntryAssembly()?.GetName().Version ?? new Version();

    private readonly IServiceProvider _serviceProvider;
    private readonly IErrorReporter _errorReporter;
    private readonly ICommandProvider _commands;
    private readonly DocumentFileWatcher _documentFileWatcher;
    private readonly string _editorConfigPath;
    private readonly VsCodeThemeReader _themeManager;
    private readonly HomeViewModel _home = new();
    private double _editorFontSize;
    private DocumentViewModel _documentRoot;
    private DocumentWatcher? _documentWatcher;
    private bool? _isSystemDarkTheme;

    public IApplicationSettingsValues Settings { get; }

    public DocumentViewModel DocumentRoot
    {
        get => _documentRoot;
        private set => SetProperty(ref _documentRoot, value);
    }

    public RoslynHost RoslynHost
    {
        get => field.NotNull();
        private set;
    }

    public bool IsInitialized
    {
        get;
        private set
        {
            SetProperty(ref field, value);
            OnPropertyChanged(nameof(HasNoOpenDocuments));
        }
    }

    [method: ImportingConstructor]
    public MainViewModel(IServiceProvider serviceProvider, IErrorReporter errorReporter, ICommandProvider commands, IApplicationSettings settings, NuGetViewModel nugetViewModel, DocumentFileWatcher documentFileWatcher)
    {
        _serviceProvider = serviceProvider;
        _errorReporter = errorReporter;
        _commands = commands;
        _documentFileWatcher = documentFileWatcher;
        _themeManager = new VsCodeThemeReader();

        settings.LoadDefault();
        _editorConfigPath = Path.Combine(settings.GetDefaultDocumentPath(), ".editorconfig");
        Settings = settings.Values;

        _errorReporter.Initialize(s_currentVersion.ToString(), settings);
        _errorReporter.LastErrorChanged += () =>
        {
            OnPropertyChanged(nameof(LastError));
            OnPropertyChanged(nameof(HasError));
        };

        NuGet = nugetViewModel;

        NewDocumentCommand = commands.Create<SourceCodeKind>(CreateNewDocument);
        OpenFileCommand = commands.CreateAsync(OpenFile);
        CloseCurrentDocumentCommand = commands.CreateAsync(CloseCurrentDocument);
        CloseDocumentCommand = commands.CreateAsync<OpenDocumentViewModel>(CloseDocument);
        ClearErrorCommand = commands.Create(_errorReporter.ClearLastError);
        ReportProblemCommand = commands.Create(ReportProblem);
        EditUserDocumentPathCommand = commands.CreateAsync(EditUserDocumentPath);
        ToggleOptimizationCommand = commands.Create(() => Settings.OptimizeCompilation = !Settings.OptimizeCompilation);
        ClearRestoreCacheCommand = commands.Create(ClearRestoreCache);
        OpenSettingsCommand = commands.Create(OpenSettings);
        OpenSecretsCommand = commands.Create(OpenSecrets);

        _editorFontSize = Settings.EditorFontSize;

        _documentRoot = CreateDocumentRoot();

        OpenDocuments = [];
        OpenDocuments.CollectionChanged += (sender, args) => OnPropertyChanged(nameof(HasNoOpenDocuments));
    }

    private void ClearRestoreCache()
    {
        IOUtilities.PerformIO(() => Directory.Delete(Path.Combine(Path.GetTempPath(), "roslynpad", "restore"), recursive: true));
    }

    public void InitializeTheme()
    {
        var customThemePath = Settings.CustomThemePath;
        if (customThemePath is not null && !File.Exists(customThemePath))
        {
            _errorReporter.ReportError(new FileNotFoundException("The configured custom theme file was not found.", customThemePath));
            customThemePath = null;
        }

        UseSystemTheme = customThemePath is null && Settings.BuiltInTheme == BuiltInTheme.System;

        var theme = customThemePath is null ? GetBuiltinThemePath(Settings.BuiltInTheme) : (path: customThemePath, type: Settings.CustomThemeType.GetValueOrDefault());
        if (!LoadTheme(theme.path, theme.type) && customThemePath is not null)
        {
            var fallbackTheme = GetBuiltinThemePath(Settings.BuiltInTheme);
            LoadTheme(fallbackTheme.path, fallbackTheme.type);
        }

        if (UseSystemTheme)
        {
            ListenToSystemThemeChanges(() =>
            {
                var buitInTheme = GetBuiltinThemePath(BuiltInTheme.System);
                LoadTheme(buitInTheme.path, buitInTheme.type);
            });
        }

        (string? path, ThemeType type) GetBuiltinThemePath(BuiltInTheme builtInTheme)
        {
            if (builtInTheme == BuiltInTheme.System)
            {
                var isSystemDarkTheme = IsSystemDarkTheme();
                if (isSystemDarkTheme == _isSystemDarkTheme)
                {
                    return default;
                }

                builtInTheme = isSystemDarkTheme ? BuiltInTheme.Dark : BuiltInTheme.Light;
                _isSystemDarkTheme = isSystemDarkTheme;
            }

            (string file, ThemeType type) theme = builtInTheme switch
            {
                BuiltInTheme.Light => ("2026-light.json", ThemeType.Light),
                BuiltInTheme.Dark => ("2026-dark.json", ThemeType.Dark),
                _ => throw new ArgumentOutOfRangeException(nameof(builtInTheme)),
            };

            return (GetOsSpecificThemePath(theme.file), theme.type);
        }

        static string GetOsSpecificThemePath(string path) =>
            RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
            ? Path.Combine(AppContext.BaseDirectory, "..", "Resources", "Themes", path)
            : Path.Combine(AppContext.BaseDirectory, "Themes", path);

        bool LoadTheme(string? themeFile, ThemeType type)
        {
            if (themeFile is null)
            {
                return true;
            }

            try
            {
                ThemeType = type;
                Theme = _themeManager.ReadThemeAsync(themeFile, type).GetAwaiter().GetResult();
                return true;
            }
            catch (Exception e)
            {
                _errorReporter.ReportError(e);
                return false;
            }
        }
    }

    private static void ListenToSystemThemeChanges(Action onChange)
    {
        if (Application.Current is { } app)
        {
            app.ActualThemeVariantChanged += (_, _) =>
            {
                if (app.RequestedThemeVariant is null)
                {
                    onChange();
                }
            };
        }
    }

    public async Task Initialize()
    {
        if (IsInitialized) return;

        try
        {
            await InitializeInternal().ConfigureAwait(true);

            IsInitialized = true;
        }
        catch (Exception e)
        {
            _errorReporter.ReportError(e);
        }
    }

    protected virtual ImmutableArray<Assembly> CompositionAssemblies => [typeof(MainViewModel).Assembly];

    private async Task InitializeInternal()
    {
        RoslynHost = await Task.Run(() => new RoslynHost(CompositionAssemblies,
            RoslynHostReferences.Empty.With(imports: Settings.DefaultUsings ?? []),
            disabledDiagnostics: ["CS1701", "CS1702", "CS7011", "CS8097"],
            analyzerConfigFiles: [_editorConfigPath]))
            .ConfigureAwait(true);

        OpenDocumentFromCommandLine();
        await OpenAutoSavedDocuments().ConfigureAwait(true);

        if (HasCachedUpdate())
        {
            HasUpdate = true;
        }
        else
        {
            var task = Task.Run(CheckForUpdates);
        }
    }

    private void OpenDocumentFromCommandLine()
    {
        string[] args = Environment.GetCommandLineArgs();

        if (args.Length > 1)
        {
            string filePath = args[1];

            if (File.Exists(filePath))
            {
                var document = DocumentViewModel.FromPath(filePath);
                OpenDocument(document);
            }
        }
    }

    private async Task OpenAutoSavedDocuments()
    {
        var documents = await Task.Run(() => LoadAutoSavedDocuments(DocumentRoot.Path)).ConfigureAwait(true);

        OpenDocuments.AddRange(documents);

        if (OpenDocuments.Count == 0)
        {
            CreateNewDocument();
        }
        else
        {
            ActiveContent = OpenDocuments[0];
        }
    }

    private IEnumerable<OpenDocumentViewModel> LoadAutoSavedDocuments(string root)
    {
        return IOUtilities.EnumerateFilesRecursive(root, $"*{DocumentViewModel.AutoSaveSuffix}.*")
            .Select(DocumentViewModel.FromPath)
            .Where(IsRelevantDocument)
            .Select(GetOpenDocumentViewModel);
    }

    private OpenDocumentViewModel GetOpenDocumentViewModel(DocumentViewModel? documentViewModel = null)
    {
        var d = _serviceProvider.GetRequiredService<OpenDocumentViewModel>();
        d.SetDocument(documentViewModel);
        return d;
    }

    private static bool IsSystemDarkTheme() => Application.Current?.ActualThemeVariant == ThemeVariant.Dark;

    public string WindowTitle
    {
        get
        {
            var currentVersion = s_currentVersion switch
            {
                { Minor: <= 0, Build: <= 0 } => s_currentVersion.Major.ToString(CultureInfo.InvariantCulture),
                { Build: <= 0 } => $"{s_currentVersion.Major}.{s_currentVersion.Minor}",
                _ => s_currentVersion.ToString()
            };
            return "RoslynPad " + currentVersion;
        }
    }

    private static void ReportProblem()
    {
        _ = Task.Run(() => Process.Start(
            new ProcessStartInfo
            {
                FileName = "https://github.com/aelij/RoslynPad/issues",
                UseShellExecute = true,
            }));
    }

    public bool HasUpdate
    {
        get; private set => SetProperty(ref field, value);
    }

    private bool HasCachedUpdate()
    {
        return Version.TryParse(Settings.LatestVersion, out var latestVersion) &&
               latestVersion > s_currentVersion;
    }

    private async Task CheckForUpdates()
    {
        string latestVersionString;
        using (var client = new System.Net.Http.HttpClient())
        {
            try
            {
                latestVersionString = await client.GetStringAsync("https://roslynpad.net/latest").ConfigureAwait(false);
            }
            catch
            {
                return;
            }
        }

        if (Version.TryParse(latestVersionString, out var latestVersion))
        {
            if (latestVersion > s_currentVersion)
            {
                HasUpdate = true;
            }
            Settings.LatestVersion = latestVersionString;
        }
    }

    [MemberNotNull(nameof(_documentWatcher))]
    private DocumentViewModel CreateDocumentRoot()
    {
        _documentWatcher?.Dispose();
        var root = DocumentViewModel.CreateRoot(Settings.EffectiveDocumentPath);
        _documentWatcher = new DocumentWatcher(_documentFileWatcher, root);
        return root;
    }

    public async Task EditUserDocumentPath()
    {
        var dialog = _serviceProvider.GetRequiredService<IFolderBrowserDialog>();
        dialog.ShowEditBox = true;
        dialog.SelectedPath = Settings.EffectiveDocumentPath;

        if (await dialog.ShowAsync().ConfigureAwait(true) == true)
        {
            string documentPath = dialog.SelectedPath;
            if (!DocumentRoot.Path.Equals(documentPath, StringComparison.OrdinalIgnoreCase))
            {
                Settings.DocumentPath = documentPath;

                DocumentRoot = CreateDocumentRoot();
            }
        }
    }

    public NuGetViewModel NuGet { get; }

    public ObservableCollection<IDocumentContent> OpenDocuments { get; }

    public OpenDocumentViewModel? CurrentOpenDocument
    {
        get;
        private set => SetProperty(ref field, value);
    }

    public IDocumentContent? ActiveContent
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                if (value is OpenDocumentViewModel openDoc)
                {
                    CurrentOpenDocument = openDoc;
                }
            }
        }
    }

    public IDelegateCommand<SourceCodeKind> NewDocumentCommand { get; }

    public IDelegateCommand OpenFileCommand { get; }

    public IDelegateCommand EditUserDocumentPathCommand { get; }

    public IDelegateCommand CloseCurrentDocumentCommand { get; }

    public IDelegateCommand<OpenDocumentViewModel> CloseDocumentCommand { get; }

    public IDelegateCommand ToggleOptimizationCommand { get; }

    public IDelegateCommand ClearRestoreCacheCommand { get; }

    public IDelegateCommand OpenSettingsCommand { get; }

    public IDelegateCommand OpenSecretsCommand { get; }

    protected virtual void OpenSettings()
    {
        var existing = OpenDocuments.OfType<SettingsViewModel>().FirstOrDefault();
        if (existing != null)
        {
            ActiveContent = existing;
            return;
        }

        var settings = new SettingsViewModel(Settings);
        AddOpenDocument(settings);
        ActiveContent = settings;
    }

    private void OpenSecrets()
    {
        var existing = OpenDocuments.OfType<SecretsViewModel>().FirstOrDefault();
        if (existing != null)
        {
            existing.Refresh();
            ActiveContent = existing;
            return;
        }

        var secrets = new SecretsViewModel(_commands, _serviceProvider.GetRequiredService<IClipboardService>());
        AddOpenDocument(secrets);
        ActiveContent = secrets;
    }

    public void OpenDocument(DocumentViewModel document)
    {
        if (document.IsFolder) return;

        var openDocument = OpenDocuments.OfType<OpenDocumentViewModel>().FirstOrDefault(x => x.Document?.Path != null && string.Equals(x.Document.Path, document.Path, StringComparison.Ordinal));
        if (openDocument == null)
        {
            openDocument = GetOpenDocumentViewModel(document);
            AddOpenDocument(openDocument);
        }

        ActiveContent = openDocument;
    }

    public async Task OpenFile()
    {
        if (!IsInitialized) return;

        var dialog = _serviceProvider.GetRequiredService<IOpenFileDialog>();
        dialog.Filter = new FileDialogFilter("C# Files", "*.cs", "*.csx");
        var fileNames = await dialog.ShowAsync().ConfigureAwait(true);
        if (fileNames is not { Length: > 0 })
        {
            return;
        }

        // make sure we use the normalized path, in case the user used the wrong capitalization on Windows
        var filePath = IOUtilities.NormalizeFilePath(fileNames.First());
        var document = DocumentViewModel.FromPath(filePath);
        if (!document.IsAutoSave)
        {
            var autoSavePath = document.GetAutoSavePath();
            if (File.Exists(autoSavePath))
            {
                document = DocumentViewModel.FromPath(autoSavePath);
            }
        }

        OpenDocument(document);
    }

    public void CreateNewDocument(SourceCodeKind kind = SourceCodeKind.Regular)
    {
        var openDocument = GetOpenDocumentViewModel();
        openDocument.SourceCodeKind = kind;
        AddOpenDocument(openDocument);
        ActiveContent = openDocument;
    }

    // The Home tab exists only as the empty state, so it is removed whenever a real
    // document is opened and restored once the last one closes.
    private void AddOpenDocument(IDocumentContent content)
    {
        OpenDocuments.Remove(_home);
        OpenDocuments.Add(content);
    }

    private void RemoveOpenDocument(IDocumentContent content)
    {
        OpenDocuments.Remove(content);

        if (!OpenDocuments.Any(d => d is not HomeViewModel))
        {
            OpenDocuments.Add(_home);
            CurrentOpenDocument = null;
            ActiveContent = _home;
        }
    }

    public async Task CloseDocument(OpenDocumentViewModel? document)
    {
        if (document == null)
        {
            return;
        }

        var result = await document.SaveAsync(promptSave: true).ConfigureAwait(true);
        if (result == SaveResult.Cancel)
        {
            return;
        }

        if (document.HasDocumentId)
        {
            RoslynHost?.CloseDocument(document.DocumentId);
        }

        RemoveOpenDocument(document);
        document.Close();
    }

    public async Task AutoSaveOpenDocuments()
    {
        foreach (var document in OpenDocuments.OfType<OpenDocumentViewModel>())
        {
            await document.AutoSaveAsync().ConfigureAwait(false);
        }
    }

    public async Task CloseTab(IDocumentContent content)
    {
        if (content is OpenDocumentViewModel document)
        {
            await CloseDocument(document).ConfigureAwait(true);
        }
        else
        {
            RemoveOpenDocument(content);
        }
    }

    private async Task CloseCurrentDocument()
    {
        if (ActiveContent == null) return;
        await CloseTab(ActiveContent).ConfigureAwait(false);
        if (!OpenDocuments.OfType<OpenDocumentViewModel>().Any())
        {
            CurrentOpenDocument = null;
        }
    }

    public async Task CloseAllDocuments()
    {
        // can't modify the collection while enumerating it.
        var openDocs = OpenDocuments.ToList();
        foreach (var tab in openDocs)
        {
            await CloseTab(tab).ConfigureAwait(false);
        }
    }

    public async Task OnExit()
    {
        await AutoSaveOpenDocuments().ConfigureAwait(false);
        IOUtilities.PerformIO(() => Directory.Delete(Path.Combine(Path.GetTempPath(), "roslynpad", "build"), recursive: true));
    }

    public Exception? LastError
    {
        get
        {
            var exception = _errorReporter.LastError;
            var aggregateException = exception as AggregateException;
            return aggregateException?.Flatten() ?? exception;
        }
    }

    public bool HasError => LastError != null;

    public IDelegateCommand ClearErrorCommand { get; }

    public bool HasNoOpenDocuments => IsInitialized && !OpenDocuments.Any(d => d is not HomeViewModel);

    public IDelegateCommand ReportProblemCommand { get; }

    public const double MinimumFontSize = 8;
    public const double MaximumFontSize = 72;

    public static bool IsValidFontSize(double value) => value >= MinimumFontSize && value <= MaximumFontSize;

    public double EditorFontSize
    {
        get => _editorFontSize;
        set
        {
            if (!IsValidFontSize(value))
            {
                return;
            }

            if (SetProperty(ref _editorFontSize, value))
            {
                Settings.EditorFontSize = value;
                EditorFontSizeChanged?.Invoke(value);
            }
        }
    }

    public event Action<double>? EditorFontSizeChanged;

    public DocumentViewModel AddDocument(string documentName)
    {
        return DocumentRoot.CreateNew(documentName);
    }

    public async Task RenameDocument(DocumentViewModel document)
    {
        if (document.IsFolder) return;

        var dialog = _serviceProvider.GetRequiredService<IRenameDocumentDialog>();
        dialog.Initialize(Path.GetFileNameWithoutExtension(document.Name));
        await dialog.ShowAsync().ConfigureAwait(true);

        if (!dialog.ShouldRename || string.IsNullOrWhiteSpace(dialog.DocumentName))
        {
            return;
        }

        var extension = Path.GetExtension(document.Name);
        var newName = dialog.DocumentName + extension;
        var directory = Path.GetDirectoryName(document.Path)!;
        var newPath = Path.Combine(directory, newName);

        if (string.Equals(document.Path, newPath, StringComparison.Ordinal))
        {
            return;
        }

        if (File.Exists(newPath))
        {
            return;
        }

        // Rename autosave file if it exists
        var autoSavePath = document.GetAutoSavePath();
        var newAutoSavePath = Path.Combine(directory, DocumentViewModel.GetAutoSaveName(newName));
        if (File.Exists(autoSavePath))
        {
            IOUtilities.PerformIO(() => File.Move(autoSavePath, newAutoSavePath));
        }

        IOUtilities.PerformIO(() => File.Move(document.Path, newPath));
    }

    public async Task SaveDocumentAs(DocumentViewModel document)
    {
        if (document.IsFolder) return;

        var directory = Path.GetDirectoryName(document.Path)!;

        var dialog = _serviceProvider.GetRequiredService<ISaveDocumentDialog>();
        dialog.ShowDoNotSave = false;
        dialog.AllowNameEdit = true;
        dialog.DocumentName = Path.GetFileNameWithoutExtension(document.Name);
        dialog.FilePathFactory = name => DocumentViewModel.GetDocumentPathFromName(directory, name + Path.GetExtension(document.Name));
        await dialog.ShowAsync().ConfigureAwait(true);

        if (dialog.Result != SaveResult.Save || string.IsNullOrWhiteSpace(dialog.DocumentName))
        {
            return;
        }

        var extension = Path.GetExtension(document.Name);
        var newPath = Path.Combine(directory, dialog.DocumentName + extension);

        // Check if the document is currently open and has unsaved changes
        var openDoc = OpenDocuments.OfType<OpenDocumentViewModel>().FirstOrDefault(x => x.Document?.Path != null && string.Equals(x.Document.Path, document.Path, StringComparison.Ordinal));
        if (openDoc != null && openDoc.IsDirty && openDoc.HasDocumentId)
        {
            // Save the current content to the new path
            var roslynDocument = RoslynHost.GetDocument(openDoc.DocumentId);
            if (roslynDocument != null)
            {
                var text = await roslynDocument.GetTextAsync().ConfigureAwait(false);
                using var writer = File.CreateText(newPath);
                for (int lineIndex = 0; lineIndex < text.Lines.Count - 1; ++lineIndex)
                {
                    await writer.WriteLineAsync(text.Lines[lineIndex].ToString()).ConfigureAwait(false);
                }
                await writer.WriteAsync(text.Lines[text.Lines.Count - 1].ToString()).ConfigureAwait(false);
            }
        }
        else
        {
            // Copy the file on disk
            IOUtilities.PerformIO(() => File.Copy(document.Path, newPath, overwrite: true));
        }
    }

    public void OpenDocumentInExplorer(DocumentViewModel document)
    {
        string path;
        if (document.IsFolder)
        {
            path = document.Path;
        }
        else
        {
            path = Path.GetDirectoryName(document.Path) ?? document.Path;
        }

        _ = Task.Run(() => Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true }));
    }

    public string? SearchText
    {
        get;
        set
        {
            if (SetProperty(ref field, value) && Settings.SearchWhileTyping)
            {
                SearchCommand.Execute();
            }
            OnPropertyChanged(nameof(CanClearSearch));
        }
    }

    public bool IsWithinSearchResults
    {
        get;
        private set
        {
            SetProperty(ref field, value);
            OnPropertyChanged(nameof(CanClearSearch));
        }
    }

    public bool CanClearSearch => IsWithinSearchResults || !string.IsNullOrEmpty(SearchText);

    public IDelegateCommand SearchCommand => _commands.CreateAsync(Search);

    private async Task Search()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            ClearSearch();
            return;
        }

        if (!SearchFileContents)
        {
            IsWithinSearchResults = true;

            foreach (var document in GetAllDocumentsForSearch(DocumentRoot))
            {
                document.IsSearchMatch = SearchDocumentName(document);
            }

            return;
        }

        Regex? regex = null;
        if (SearchUsingRegex)
        {
            regex = CreateSearchRegex();

            if (regex == null)
            {
                return;
            }
        }

        IsWithinSearchResults = true;

        foreach (var document in GetAllDocumentsForSearch(DocumentRoot))
        {
            // assign on the UI thread - the document tree binds to IsSearchMatch
            document.IsSearchMatch = SearchDocumentName(document) ||
                await SearchInFile(document, regex).ConfigureAwait(true);
        }

        bool SearchDocumentName(DocumentViewModel document)
        {
            return document.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase);
        }

        Regex? CreateSearchRegex()
        {
            try
            {
                var regex = new Regex(SearchText, RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnoreCase, TimeSpan.FromSeconds(5));

                ClearError(nameof(SearchText), "Regex");

                return regex;
            }
            catch (ArgumentException)
            {
                SetError(nameof(SearchText), "Regex", "Invalid regular expression");

                return null;
            }
        }

        async Task<bool> SearchInFile(DocumentViewModel document, Regex? regex)
        {
            // a regex can span many lines so we need to load the entire file;
            // otherwise, search line-by-line

            if (regex != null)
            {
                var documentText = await IOUtilities.ReadAllTextAsync(document.Path).ConfigureAwait(false);
                try
                {
                    return regex.IsMatch(documentText);
                }
                catch (RegexMatchTimeoutException)
                {
                    return false;
                }
            }

            // need IAsyncEnumerable here, but for now just push it to the thread-pool
            return await Task.Run(() =>
            {
                var lines = IOUtilities.ReadLines(document.Path);
                return lines.Any(line =>
                    line.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
            }).ConfigureAwait(false);
        }
    }

    private static IEnumerable<DocumentViewModel> GetAllDocumentsForSearch(DocumentViewModel root)
    {
        var children = root.Children;
        if (children is null)
        {
            yield break;
        }

        foreach (var document in children)
        {
            if (document.IsFolder)
            {
                foreach (var childDocument in GetAllDocumentsForSearch(document))
                {
                    yield return childDocument;
                }

                // TODO: I'm lazy :)
                document.IsSearchMatch = document.Children?.Any(c => c.IsSearchMatch) == true;
            }
            else
            {
                yield return document;
            }
        }
    }

    public bool SearchFileContents
    {
        get => Settings.SearchFileContents;
        set
        {
            Settings.SearchFileContents = value;
            if (!value)
            {
                SearchUsingRegex = false;
            }
            OnPropertyChanged();
        }
    }

    public bool SearchUsingRegex
    {
        get => Settings.SearchUsingRegex;
        set
        {
            Settings.SearchUsingRegex = value;
            if (value)
            {
                SearchFileContents = true;
            }
            OnPropertyChanged();
        }
    }

    public IDelegateCommand ClearSearchCommand => _commands.Create(ClearSearch);

    public bool UseSystemTheme { get; private set; }

    public ThemeType ThemeType { get; private set; }

    public Theme Theme
    {
        get => field.NotNull();
        private set
        {
            field = value;
            ThemeChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public event EventHandler<EventArgs>? ThemeChanged;

    public event Action? ResultsAvailable;

    internal void OnResultsAvailable() => ResultsAvailable?.Invoke();

    private void ClearSearch()
    {
        SearchText = null;
        IsWithinSearchResults = false;
        ClearErrors(nameof(SearchText));

        foreach (var document in GetAllDocumentsForSearch(DocumentRoot))
        {
            document.IsSearchMatch = true;
        }
    }

    private class DocumentWatcher : IDisposable
    {
        private static readonly char[] s_pathSeparators = [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar];

        private readonly DocumentViewModel _documentRoot;
        private readonly IDisposable _subscription;

        public DocumentWatcher(DocumentFileWatcher watcher, DocumentViewModel documentRoot)
        {
            _documentRoot = documentRoot;
            watcher.Path = documentRoot.Path;
            _subscription = watcher.Subscribe(OnDocumentFileChanged);
        }

        public void Dispose() => _subscription.Dispose();

        private void OnDocumentFileChanged(DocumentFileChanged data)
        {
            var pathParts = data.Path.Substring(_documentRoot.Path.Length)
                .Split(s_pathSeparators, StringSplitOptions.RemoveEmptyEntries);

            DocumentViewModel? current = _documentRoot;

            for (var index = 0; index < pathParts.Length; index++)
            {
                if (!current.IsChildrenInitialized)
                {
                    break;
                }

                var part = pathParts[index];
                var isLast = index == pathParts.Length - 1;

                var parent = current;
                current = current.InternalChildren[part];

                // the current part is not in the tree
                if (current is null)
                {
                    if (data.Type != DocumentFileChangeType.Deleted)
                    {
                        var currentPath = isLast && data.Type == DocumentFileChangeType.Renamed
                            ? data.NewPath
                            : Path.Combine(_documentRoot.Path, Path.Combine(pathParts.Take(index + 1).ToArray()));

                        var newDocument = DocumentViewModel.FromPath(currentPath!);
                        if (!newDocument.IsAutoSave &&
                            IsRelevantDocument(newDocument))
                        {
                            parent.AddChild(newDocument);
                        }
                    }

                    break;
                }

                // it's the last part - the actual file
                if (isLast)
                {
                    switch (data.Type)
                    {
                        case DocumentFileChangeType.Renamed:
                            if (data.NewPath != null)
                            {
                                current.ChangePath(data.NewPath);
                                // move it to the correct place
                                parent.InternalChildren.Remove(current);
                                if (IsRelevantDocument(current))
                                {
                                    parent.AddChild(current);
                                }
                            }
                            break;
                        case DocumentFileChangeType.Deleted:
                            parent.InternalChildren.Remove(current);
                            break;
                    }
                }
            }
        }
    }

    private static bool IsRelevantDocument(DocumentViewModel document)
    {
        return document.IsFolder ||
            DocumentViewModel.RelevantFileExtensions.Contains(Path.GetExtension(document.Name));
    }

    public void Dispose()
    {
        _documentFileWatcher?.Dispose();
    }
}
