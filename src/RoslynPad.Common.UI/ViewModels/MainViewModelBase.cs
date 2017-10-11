using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using RoslynPad.Roslyn;
using RoslynPad.Utilities;
using NuGet.Packaging;
using HttpClient = System.Net.Http.HttpClient;

namespace RoslynPad.UI
{
    public class MainViewModelBase : NotificationObject
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ITelemetryProvider _telemetryProvider;
        private readonly ICommandProvider _commands;
        private static readonly Version _currentVersion = new Version(13, 2);
        private static readonly string _currentVersionVariant = "";

        public const string NuGetPathVariableName = "$NuGet";
        private const string ConfigFileName = "RoslynPad.json";

        private OpenDocumentViewModel _currentOpenDocument;
        private bool _hasUpdate;
        private double _editorFontSize;
        private string _searchText;
        private bool _isWithinSearchResults;
        private string _documentPath;
        private bool _isInitialized;
        private DocumentViewModel _documentViewModel;

        public IApplicationSettings Settings { get; }
        public DocumentViewModel DocumentRoot
        {
            get => _documentViewModel;
            private set => SetProperty (ref _documentViewModel, value);
        }
        public NuGetConfiguration NuGetConfiguration { get; }
        public RoslynHost RoslynHost { get; private set; }

        public bool IsInitialized
        {
            get { return _isInitialized; }
            private set
            {
                SetProperty(ref _isInitialized, value);
                OnPropertyChanged(nameof(HasNoOpenDocuments));
            }
        }

        public MainViewModelBase(IServiceProvider serviceProvider, ITelemetryProvider telemetryProvider, ICommandProvider commands, IApplicationSettings settings, NuGetViewModel nugetViewModel)
        {
            _serviceProvider = serviceProvider;
            _telemetryProvider = telemetryProvider;
            _commands = commands;

            settings.LoadFrom(Path.Combine(GetDefaultDocumentPath(), ConfigFileName));
            Settings = settings;

            _telemetryProvider.Initialize(_currentVersion.ToString(), settings);
            _telemetryProvider.LastErrorChanged += () =>
            {
                OnPropertyChanged(nameof(LastError));
                OnPropertyChanged(nameof(HasError));
            };

            NuGet = nugetViewModel;
            NuGetConfiguration = new NuGetConfiguration(NuGet.GlobalPackageFolder, NuGetPathVariableName);

            NewDocumentCommand = commands.Create(CreateNewDocument);
            OpenFileCommand = commands.CreateAsync(OpenFile);
            CloseCurrentDocumentCommand = commands.CreateAsync(CloseCurrentDocument);
            ClearErrorCommand = commands.Create(() => _telemetryProvider.ClearLastError());
            ReportProblemCommand = commands.Create(ReportProblem);
            EditUserDocumentPathCommand = commands.Create(EditUserDocumentPath);
            RefreshUserDocumentsCommand = commands.Create (RefreshUserDocumends);
            ToggleOptimizationCommand = commands.Create(() => settings.OptimizeCompilation = !settings.OptimizeCompilation);

            _editorFontSize = Settings.EditorFontSize;

            DocumentRoot = CreateDocumentRoot();

            OpenDocuments = new ObservableCollection<OpenDocumentViewModel>();
            OpenDocuments.CollectionChanged += (sender, args) => OnPropertyChanged(nameof(HasNoOpenDocuments));
        }

        private void RefreshUserDocumends ()
        {
            DocumentRoot = CreateDocumentRoot ();
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
                _telemetryProvider.ReportError(e);
            }
        }

        protected virtual IEnumerable<Assembly> CompositionAssemblies => Array.Empty<Assembly>();

        private async Task InitializeInternal()
        {
            RoslynHost = await Task.Run(() => new RoslynHost(NuGetConfiguration, CompositionAssemblies,
                RoslynHostReferences.Default.With(typeNamespaceImports: new[] { typeof(Runtime.ObjectExtensions) })))
                .ConfigureAwait(true);

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
                CurrentOpenDocument = OpenDocuments[0];
            }
        }

        private IEnumerable<OpenDocumentViewModel> LoadAutoSavedDocuments(string root)
        {
            return IOUtilities.EnumerateFilesRecursive(root, DocumentViewModel.GetAutoSaveName("*")).Select(x =>
                GetOpenDocumentViewModel(DocumentViewModel.FromPath(x)));
        }

        private OpenDocumentViewModel GetOpenDocumentViewModel(DocumentViewModel documentViewModel)
        {
            var d = _serviceProvider.GetService<OpenDocumentViewModel>();
            d.SetDocument(documentViewModel);
            return d;
        }

        public string WindowTitle
        {
            get
            {
                var currentVersion = _currentVersion.Minor <= 0 && _currentVersion.Build <= 0
                    ? _currentVersion.Major.ToString()
                    : _currentVersion.ToString();
                var title = "RoslynPad " + currentVersion;
                if (!string.IsNullOrEmpty(_currentVersionVariant))
                {
                    title += "-" + _currentVersionVariant;
                }
                return title;
            }
        }

        private static void ReportProblem()
        {
            Task.Run(() => Process.Start("https://github.com/aelij/RoslynPad/issues"));
        }

        public bool HasUpdate
        {
            get => _hasUpdate; private set => SetProperty(ref _hasUpdate, value);
        }

        private bool HasCachedUpdate()
        {
            return Version.TryParse(Settings.LatestVersion, out var latestVersion) &&
                   latestVersion > _currentVersion;
        }

        private async Task CheckForUpdates()
        {
            string latestVersionString;
            using (var client = new HttpClient())
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
                if (latestVersion > _currentVersion)
                {
                    HasUpdate = true;
                }
                Settings.LatestVersion = latestVersionString;
            }
        }

        private DocumentViewModel CreateDocumentRoot()
        {
            var root = DocumentViewModel.CreateRoot(GetUserDocumentPath());

            return root;
        }

        private string GetUserDocumentPath()
        {
            if (_documentPath == null)
            {

                var userDefinedPath = Settings.DocumentPath;
                _documentPath = !string.IsNullOrEmpty(userDefinedPath) && Directory.Exists(userDefinedPath)
                    ? userDefinedPath
                    : GetDefaultDocumentPath();
            }

            return _documentPath;
        }

        private string GetDefaultDocumentPath()
        {
            string documentsPath = null;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                const int myDocuments = 5;
                var stringBuilder = new StringBuilder(260);
                var result = SHGetFolderPath(IntPtr.Zero, myDocuments, IntPtr.Zero, 0, stringBuilder);
                if (result >= 0)
                {
                    documentsPath = stringBuilder.ToString();
                }
            }
            else // Unix or Mac
            {
                documentsPath = Environment.GetEnvironmentVariable("HOME");
            }

            if (string.IsNullOrEmpty(documentsPath))
            {
                documentsPath = "/";
                _telemetryProvider.ReportError(new InvalidOperationException("Unable to locate the user documents folder; Using root"));
            }

            return Path.Combine(documentsPath, "RoslynPad");
        }

        [DllImport("shell32.dll", BestFitMapping = false, CharSet = CharSet.Unicode)]
        private static extern int SHGetFolderPath(IntPtr hwndOwner, int nFolder, IntPtr hToken, int dwFlags, [Out] StringBuilder lpszPath);

        public void EditUserDocumentPath()
        {
            var dialog = _serviceProvider.GetService<IFolderBrowserDialog>();
            dialog.ShowEditBox = true;
            dialog.SelectedPath = GetUserDocumentPath();

            if (dialog.Show() == true)
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

        public ObservableCollection<OpenDocumentViewModel> OpenDocuments { get; }

        public OpenDocumentViewModel CurrentOpenDocument
        {
            get => _currentOpenDocument;
            set
            {
                if (value == null) return; // prevent binding from clearing the value
                SetProperty(ref _currentOpenDocument, value);
            }
        }

        private void ClearCurrentOpenDocument()
        {
            if (_currentOpenDocument == null) return;
            _currentOpenDocument = null;
            OnPropertyChanged(nameof(CurrentOpenDocument));
        }

        public IDelegateCommand NewDocumentCommand { get; }

        public IDelegateCommand OpenFileCommand { get; }

        public IDelegateCommand EditUserDocumentPathCommand { get; }

        public IDelegateCommand RefreshUserDocumentsCommand { get; }

        public IDelegateCommand CloseCurrentDocumentCommand { get; }

        public IDelegateCommand ToggleOptimizationCommand { get; }

        public void OpenDocument(DocumentViewModel document)
        {
            if (document.IsFolder) return;

            var openDocument = OpenDocuments.FirstOrDefault(x => x.Document == document);
            if (openDocument == null)
            {
                openDocument = GetOpenDocumentViewModel(document);
                OpenDocuments.Add(openDocument);
            }
            CurrentOpenDocument = openDocument;
        }

        public async Task OpenFile()
        {
            if (!IsInitialized) return;

            var dialog = _serviceProvider.GetService<IOpenFileDialog>();
            dialog.Filter = new FileDialogFilter("C# Scripts", "csx");
            if (!await dialog.ShowAsync().ConfigureAwait(true))
            {
                return;
            }

            var document = DocumentViewModel.FromPath(dialog.FileName);
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

        public void CreateNewDocument()
        {
            var openDocument = GetOpenDocumentViewModel(null);
            OpenDocuments.Add(openDocument);
            CurrentOpenDocument = openDocument;
        }

        public async Task CloseDocument(OpenDocumentViewModel document)
        {
            if (document == null)
            {
                return;
            }

            var result = await document.Save(promptSave: true).ConfigureAwait(true);
            if (result == SaveResult.Cancel)
            {
                return;
            }

            RoslynHost.CloseDocument(document.DocumentId);
            OpenDocuments.Remove(document);
            document.Close();
        }

        public async Task AutoSaveOpenDocuments()
        {
            foreach (var document in OpenDocuments)
            {
                await document.AutoSave().ConfigureAwait(false);
            }
        }

        private async Task CloseCurrentDocument()
        {
            if (CurrentOpenDocument == null) return;
            await CloseDocument(CurrentOpenDocument).ConfigureAwait(false);
            if (!OpenDocuments.Any())
            {
                ClearCurrentOpenDocument();
            }
        }

        public async Task CloseAllDocuments()
        {
            // can't modify the collection while enumerating it.
            var openDocs = new ObservableCollection<OpenDocumentViewModel>(OpenDocuments);
            foreach (var document in openDocs)
            {
                await CloseDocument(document).ConfigureAwait(false);
            }
        }

        public async Task OnExit()
        {
            await AutoSaveOpenDocuments().ConfigureAwait(false);
        }

        public Exception LastError
        {
            get
            {
                var exception = _telemetryProvider.LastError;
                var aggregateException = exception as AggregateException;
                return aggregateException?.Flatten() ?? exception;
            }
        }

        public bool HasError => LastError != null;

        public IDelegateCommand ClearErrorCommand { get; }

        public bool SendTelemetry
        {
            get => Settings.SendErrors; set
            {
                Settings.SendErrors = value;
                OnPropertyChanged(nameof(SendTelemetry));
            }
        }

        public bool HasNoOpenDocuments => IsInitialized && OpenDocuments.Count == 0;

        public IDelegateCommand ReportProblemCommand { get; }

        public double MinimumEditorFontSize => 8;
        public double MaximumEditorFontSize => 72;

        public double EditorFontSize
        {
            get => _editorFontSize; set
            {
                if (value < MinimumEditorFontSize || value > MaximumEditorFontSize) return;

                if (SetProperty(ref _editorFontSize, value))
                {
                    Settings.EditorFontSize = value;
                    EditorFontSizeChanged?.Invoke(value);
                }
            }
        }

        public event Action<double> EditorFontSizeChanged;

        public DocumentViewModel AddDocument(string documentName)
        {
            return DocumentRoot.CreateNew(documentName);
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value) && Settings.SearchWhileTyping)
                {
                    SearchCommand.Execute();
                }
                OnPropertyChanged(nameof(CanClearSearch));
            }
        }

        public bool IsWithinSearchResults
        {
            get => _isWithinSearchResults;
            private set
            {
                SetProperty(ref _isWithinSearchResults, value);
                OnPropertyChanged(nameof(CanClearSearch));
            }
        }

        public bool CanClearSearch => IsWithinSearchResults || !string.IsNullOrEmpty(SearchText);

        public IDelegateCommand SearchCommand => _commands.CreateAsync(Search);

        #region Search

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

            Regex regex = null;
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
                if (SearchDocumentName(document))
                {
                    document.IsSearchMatch = true;
                }
                else
                {
                    await SearchInFile(document, regex).ConfigureAwait(false);
                }
            }
        }

        private bool SearchDocumentName(DocumentViewModel document)
        {
            return document.Name.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private Regex CreateSearchRegex()
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

        private async Task SearchInFile(DocumentViewModel document, Regex regex)
        {
            // a regex can span many lines so we need to load the entire file;
            // otherwise, search line-by-line

            if (regex != null)
            {
                var documentText = await IOUtilities.ReadAllTextAsync(document.Path).ConfigureAwait(false);
                try
                {
                    document.IsSearchMatch = regex.IsMatch(documentText);
                }
                catch (RegexMatchTimeoutException)
                {
                    document.IsSearchMatch = false;
                }
            }
            else
            {
                // need IAsyncEnumerable here, but for now just push it to the thread-pool
                await Task.Run(() =>
                {
                    var lines = IOUtilities.ReadLines(document.Path);
                    document.IsSearchMatch = lines.Any(line =>
                        line.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) >= 0);
                }).ConfigureAwait(false);
            }
        }

        private static IEnumerable<DocumentViewModel> GetAllDocumentsForSearch(DocumentViewModel root)
        {
            foreach (var document in root.Children)
            {
                if (document.IsFolder)
                {
                    foreach (var childDocument in GetAllDocumentsForSearch(document))
                    {
                        yield return childDocument;
                    }

                    // TODO: I'm lazy :)
                    document.IsSearchMatch = document.Children.Any(c => c.IsSearchMatch);
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

        #endregion
    }
}