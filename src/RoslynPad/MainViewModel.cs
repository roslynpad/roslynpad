using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Microsoft.HockeyApp;
using RoslynPad.Roslyn;
using RoslynPad.Utilities;

namespace RoslynPad
{
    internal sealed class MainViewModel : NotificationObject
    {
        private static readonly Version _currentVersion = new Version(0, 11);
        private static readonly string _currentVersionVariant = "";

        private const string HockeyAppId = "8655168826d9412483763f7ddcf84b8e";
        public const string NuGetPathVariableName = "$NuGet";

        private OpenDocumentViewModel _currentOpenDocument;
        private Exception _lastError;
        private bool _hasUpdate;
        private double _editorFontSize;

        public DocumentViewModel DocumentRoot { get; }
        public NuGetConfiguration NuGetConfiguration { get; }
        public RoslynHost RoslynHost { get; }

        public MainViewModel()
        {
            var hockeyClient = (HockeyClient)HockeyClient.Current;
            if (SendTelemetry)
            {
                hockeyClient.Configure(HockeyAppId)
                    .RegisterCustomDispatcherUnhandledExceptionLogic(OnUnhandledDispatcherException)
                    .UnregisterDefaultUnobservedTaskExceptionHandler();

                var platformHelper = (HockeyPlatformHelperWPF)hockeyClient.PlatformHelper;
                platformHelper.AppVersion = _currentVersion.ToString();

                hockeyClient.TrackEvent(TelemetryEventNames.Start);
            }
            else
            {
                Application.Current.DispatcherUnhandledException += (sender, args) => OnUnhandledDispatcherException(args);

                var platformHelper = new HockeyPlatformHelperWPF { AppVersion = _currentVersion.ToString() };
                hockeyClient.PlatformHelper = platformHelper;
                hockeyClient.AppIdentifier = HockeyAppId;
            }

            NuGet = new NuGetViewModel();
            NuGetConfiguration = new NuGetConfiguration(NuGet.GlobalPackageFolder, NuGetPathVariableName);
            RoslynHost = new RoslynHost(NuGetConfiguration, new[] { Assembly.Load("RoslynPad.RoslynEditor") });

            NewDocumentCommand = new DelegateCommand((Action)CreateNewDocument);
            CloseCurrentDocumentCommand = new DelegateCommand(CloseCurrentDocument);
            ClearErrorCommand = new DelegateCommand(() => LastError = null);
            ReportProblemCommand = new DelegateCommand((Action)ReportProblem);

            _editorFontSize = Properties.Settings.Default.EditorFontSize;

            DocumentRoot = CreateDocumentRoot();
            Documents = DocumentRoot.Children;
            OpenDocuments = new ObservableCollection<OpenDocumentViewModel>(LoadAutoSaves(DocumentRoot.Path));
            OpenDocuments.CollectionChanged += (sender, args) => OnPropertyChanged(nameof(HasNoOpenDocuments));
            if (HasNoOpenDocuments)
            {
                CreateNewDocument();
            }
            else
            {
                CurrentOpenDocument = OpenDocuments[0];
            }

            if (HasCachedUpdate())
            {
                HasUpdate = true;
            }
            else
            {
                Task.Run(CheckForUpdates);
            }
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
            var dialog = new ReportProblemDialog(this);
            dialog.Show();
        }

        public IEnumerable<OpenDocumentViewModel> LoadAutoSaves(string root)
        {
            return Directory.EnumerateFiles(root, DocumentViewModel.GetAutoSaveName("*"), SearchOption.AllDirectories)
                .Select(x => new OpenDocumentViewModel(this, DocumentViewModel.CreateAutoSave(this, x)));
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
            var root = DocumentViewModel.CreateRoot(this);
            if (!Directory.Exists(Path.Combine(root.Path, "Samples")))
            {
                // ReSharper disable once PossibleNullReferenceException
                using (var stream = Application.GetResourceStream(
                    new Uri("pack://application:,,,/RoslynPad;component/Resources/Samples.zip")).Stream)
                using (var archive = new ZipArchive(stream))
                {
                    archive.ExtractToDirectory(root.Path);
                }
            }
            return root;
        }

        public NuGetViewModel NuGet { get; }

        public ObservableCollection<OpenDocumentViewModel> OpenDocuments { get; }

        public OpenDocumentViewModel CurrentOpenDocument
        {
            get { return _currentOpenDocument; }
            set { SetProperty(ref _currentOpenDocument, value); }
        }

        public ObservableCollection<DocumentViewModel> Documents { get; }

        public DelegateCommand NewDocumentCommand { get; }

        public DelegateCommand CloseCurrentDocumentCommand { get; }

        public void OpenDocument(DocumentViewModel document)
        {
            var openDocument = OpenDocuments.FirstOrDefault(x => x.Document == document);
            if (openDocument == null)
            {
                openDocument = new OpenDocumentViewModel(this, document);
                OpenDocuments.Add(openDocument);
            }
            CurrentOpenDocument = openDocument;
        }

        public void CreateNewDocument()
        {
            var openDocument = new OpenDocumentViewModel(this, null);
            OpenDocuments.Add(openDocument);
            CurrentOpenDocument = openDocument;
        }

        public async Task CloseDocument(OpenDocumentViewModel document)
        {
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
            if (CurrentOpenDocument != null)
            {
                await CloseDocument(CurrentOpenDocument).ConfigureAwait(false);
            }
        }

        private void OnUnhandledDispatcherException(DispatcherUnhandledExceptionEventArgs args)
        {
            var exception = args.Exception;
            if (exception is OperationCanceledException)
            {
                args.Handled = true;
                return;
            }
            LastError = exception;
            args.Handled = true;
        }

        public async Task OnExit()
        {
            await AutoSaveOpenDocuments().ConfigureAwait(false);
        }

        public Exception LastError
        {
            get { return _lastError; }
            private set
            {
                SetProperty(ref _lastError, value);
                OnPropertyChanged(nameof(HasError));
            }
        }

        public bool HasError => LastError != null;

        public DelegateCommand ClearErrorCommand { get; }

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

        public DelegateCommand ReportProblemCommand { get; private set; }

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

        public Task SubmitFeedback(string feedbackText, string email)
        {
            return Task.Run(async () =>
            {
                var feedback = HockeyClient.Current.CreateFeedbackThread();
                await feedback.PostFeedbackMessageAsync(feedbackText, email).ConfigureAwait(false);
            });
        }
    }

    internal static class TelemetryEventNames
    {
        public const string Start = "Start";
    }
}