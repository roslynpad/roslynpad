using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Composition;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Practices.ServiceLocation;
using RoslynPad.Roslyn;
using RoslynPad.Utilities;
using NuGet;
using HttpClient = System.Net.Http.HttpClient;

namespace RoslynPad.UI
{
    [Export, Shared]
    public sealed class MainViewModel : NotificationObject
    {
        private readonly IServiceLocator _serviceLocator;
        private readonly ITelemetryProvider _telemetryProvider;
        private static readonly Version _currentVersion = new Version(0, 11);
        private static readonly string _currentVersionVariant = "";

        public const string NuGetPathVariableName = "$NuGet";

        private OpenDocumentViewModel _currentOpenDocument;
        private bool _hasUpdate;
        private double _editorFontSize;

        public DocumentViewModel DocumentRoot { get; private set; }
        public NuGetConfiguration NuGetConfiguration { get; }
        public RoslynHost RoslynHost { get; }

        [ImportingConstructor]
        public MainViewModel(IServiceLocator serviceLocator, ITelemetryProvider telemetryProvider, ICommandProvider commands, NuGetViewModel nugetViewModel)
        {
            _serviceLocator = serviceLocator;
            _telemetryProvider = telemetryProvider;
            _telemetryProvider.Initialize(_currentVersion.ToString());
            _telemetryProvider.LastErrorChanged += () => OnPropertyChanged(nameof(LastError));

            NuGet = nugetViewModel;
            NuGetConfiguration = new NuGetConfiguration(NuGet.GlobalPackageFolder, NuGetPathVariableName);
            RoslynHost = new RoslynHost(NuGetConfiguration, new[]
            {
                // TODO: xplat
                Assembly.Load("RoslynPad.Roslyn.Windows"),
                Assembly.Load("RoslynPad.Editor.Windows")
            });

            NewDocumentCommand = commands.Create(CreateNewDocument);
            CloseCurrentDocumentCommand = commands.CreateAsync(CloseCurrentDocument);
            ClearErrorCommand = commands.Create(() => _telemetryProvider.ClearLastError());
            ReportProblemCommand = commands.Create(ReportProblem);
            EditUserDocumentPathCommand = commands.Create(EditUserDocumentPath);

            _editorFontSize = Properties.Settings.Default.EditorFontSize;

            DocumentRoot = CreateDocumentRoot();

            OpenDocuments = new ObservableCollection<OpenDocumentViewModel>();
            OpenDocuments.CollectionChanged += (sender, args) => OnPropertyChanged(nameof(HasNoOpenDocuments));
        }

        public void Initialize()
        {
            OpenAutoSavedDocuments();

            if (HasCachedUpdate())
            {
                HasUpdate = true;
            }
            else
            {
                Task.Run(CheckForUpdates);
            }
        }

        private void OpenAutoSavedDocuments()
        {
            OpenDocuments.AddRange(LoadAutoSavedDocuments(DocumentRoot.Path));

            if (HasNoOpenDocuments)
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
            return EnumerateFilesWithCatch(root, DocumentViewModel.GetAutoSaveName("*")).Select(x =>
                GetOpenDocumentViewModel(DocumentViewModel.CreateAutoSave(x)));
        }

        private OpenDocumentViewModel GetOpenDocumentViewModel(DocumentViewModel documentViewModel)
        {
            var d = _serviceLocator.GetInstance<OpenDocumentViewModel>();
            d.SetDocument(documentViewModel);
            return d;
        }

        public string WindowTitle
        {
            get
            {
                var title = "RoslynPad " + _currentVersion;
                if (!string.IsNullOrEmpty(_currentVersionVariant))
                {
                    title += "-" + _currentVersionVariant;
                }
                return title;
            }
        }

        private void ReportProblem()
        {
            var dialog = _serviceLocator.GetInstance<IReportProblemDialog>();
            dialog.Show();
        }

        private static IEnumerable<string> EnumerateFilesWithCatch(string path, string searchPattern)
        {
            IEnumerable<string> files;
            try
            {
                files = Directory.EnumerateFiles(path, searchPattern);
            }
            catch (Exception)
            {
                // TODO: log this
                return Array.Empty<string>();
            }

            foreach (var directory in Directory.EnumerateDirectories(path))
            {
                files = files.Concat(EnumerateFilesWithCatch(directory, searchPattern));
            }

            return files;
        }

        public bool HasUpdate
        {
            get { return _hasUpdate; }
            private set { SetProperty(ref _hasUpdate, value); }
        }

        private static bool HasCachedUpdate()
        {
            Version latestVersion;
            return Version.TryParse(Properties.Settings.Default.LatestVersion, out latestVersion) &&
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
            Version latestVersion;
            if (Version.TryParse(latestVersionString, out latestVersion))
            {
                if (latestVersion > _currentVersion)
                {
                    HasUpdate = true;
                }
                Properties.Settings.Default.LatestVersion = latestVersionString;
                Properties.Settings.Default.Save();
            }
        }

        private DocumentViewModel CreateDocumentRoot()
        {
            var root = DocumentViewModel.CreateRoot(GetUserDocumentPath());

            // TODO: virtual samples
            //if (!Directory.Exists(Path.Combine(root.Path, "Samples")))
            //{
            //    // ReSharper disable once PossibleNullReferenceException
            //    using (var stream = Application.GetResourceStream(
            //        new Uri("pack://application:,,,/RoslynPad;component/Resources/Samples.zip")).Stream)
            //    using (var archive = new ZipArchive(stream))
            //    {
            //        archive.ExtractToDirectory(root.Path);
            //    }
            //}

            Documents = root.Children;

            return root;
        }


        internal static string GetUserDocumentPath()
        {
            var userDefinedPath = Properties.Settings.Default.DocumentPath;
            return !string.IsNullOrEmpty(userDefinedPath) && Directory.Exists(userDefinedPath)
                ? userDefinedPath
                : GetDefaultDocumentPath();
        }

        private static string GetDefaultDocumentPath()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "RoslynPad");
        }

        public void EditUserDocumentPath()
        {
            var dialog = _serviceLocator.GetInstance<IFolderBrowserDialog>();
            dialog.ShowEditBox = true;
            dialog.SelectedPath = GetUserDocumentPath();

            if (dialog.Show() == true)
            {
                string documentPath = dialog.SelectedPath;
                if (!DocumentRoot.Path.Equals(documentPath, StringComparison.OrdinalIgnoreCase))
                {
                    Properties.Settings.Default.DocumentPath = documentPath;
                    Properties.Settings.Default.Save();

                    DocumentRoot = CreateDocumentRoot();
                }
            }
        }

        public NuGetViewModel NuGet { get; }

        public ObservableCollection<OpenDocumentViewModel> OpenDocuments { get; }

        public OpenDocumentViewModel CurrentOpenDocument
        {
            get { return _currentOpenDocument; }
            set { SetProperty(ref _currentOpenDocument, value); }
        }

        private ObservableCollection<DocumentViewModel> _documents;

        public ObservableCollection<DocumentViewModel> Documents
        {
            get { return _documents; }
            internal set { SetProperty(ref _documents, value); }
        }

        public IActionCommand NewDocumentCommand { get; }

        public IActionCommand EditUserDocumentPathCommand { get; }

        public IActionCommand CloseCurrentDocumentCommand { get; }

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

            // ReSharper disable once PossibleNullReferenceException
            var autoSavePath = document.Document?.GetAutoSavePath();
            if (autoSavePath != null && File.Exists(autoSavePath))
            {
                File.Delete(autoSavePath);
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

        public Exception LastError => _telemetryProvider.LastError;

        public bool HasError => LastError != null;

        public IActionCommand ClearErrorCommand { get; }

        public bool SendTelemetry
        {
            get { return Properties.Settings.Default.SendErrors; }
            set
            {
                Properties.Settings.Default.SendErrors = value;
                Properties.Settings.Default.Save();
                OnPropertyChanged(nameof(SendTelemetry));
            }
        }

        public bool HasNoOpenDocuments => OpenDocuments.Count == 0;

        public IActionCommand ReportProblemCommand { get; private set; }

        public double MinimumEditorFontSize => 8;
        public double MaximumEditorFontSize => 72;

        public double EditorFontSize
        {
            get { return _editorFontSize; }
            set
            {
                if (value < MinimumEditorFontSize || value > MaximumEditorFontSize) return;

                if (SetProperty(ref _editorFontSize, value))
                {
                    Properties.Settings.Default.EditorFontSize = value;
                    Properties.Settings.Default.Save();
                    EditorFontSizeChanged?.Invoke(value);
                }
            }
        }

        public event Action<double> EditorFontSizeChanged;

        public DocumentViewModel AddDocument(string documentName)
        {
            return DocumentRoot.CreateNew(documentName);
        }
    }
}