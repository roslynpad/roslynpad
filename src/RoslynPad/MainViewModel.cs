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
using Microsoft.ApplicationInsights;
using RoslynPad.Host;
using RoslynPad.Roslyn;
using RoslynPad.Utilities;

namespace RoslynPad
{
    internal sealed class MainViewModel : NotificationObject
    {
        private static readonly Version _currentVersion = new Version(0, 8);

        private const string ApplicationInsightsInstrumentationKey = "86551688-26d9-4124-8376-3f7ddcf84b8e";
        public const string NuGetPathVariableName = "$NuGet";

        private readonly TelemetryClient _telemetryClient;

        private OpenDocumentViewModel _currentOpenDocument;
        private Exception _lastError;
        private bool _hasUpdate;
        private double _editorFontSize;

        public DocumentViewModel DocumentRoot { get; }
        public NuGetConfiguration NuGetConfiguration { get; }
        public RoslynHost RoslynHost { get; }

        public MainViewModel()
        {
            _telemetryClient = new TelemetryClient { InstrumentationKey = ApplicationInsightsInstrumentationKey };
            _telemetryClient.Context.Component.Version = _currentVersion.ToString();
#if DEBUG
            _telemetryClient.Context.Properties["DEBUG"] = "1";
#endif
            if (SendTelemetry)
            {
                _telemetryClient.TrackEvent(TelemetryEventNames.Start);
            }

            Application.Current.DispatcherUnhandledException += (o, e) => OnUnhandledDispatcherException(e);
            AppDomain.CurrentDomain.UnhandledException += (o, e) => OnUnhandledException((Exception)e.ExceptionObject, flushSync: true);
            TaskScheduler.UnobservedTaskException += (o, e) => OnUnhandledException(e.Exception);

            NuGet = new NuGetViewModel();
            NuGetConfiguration = new NuGetConfiguration(NuGet.GlobalPackageFolder, NuGetPathVariableName);
            RoslynHost = new RoslynHost(NuGetConfiguration);
            ChildProcessManager = new ChildProcessManager();

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
                using (var stream = Application.GetResourceStream(new Uri("pack://application:,,,/RoslynPad;component/Resources/Samples.zip")).Stream)
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
            if (document.Document?.IsAutoSave == true)
            {
                File.Delete(document.Document.Path);
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

        private void OnUnhandledException(Exception exception, bool flushSync = false)
        {
            if (exception is OperationCanceledException) return;
            TrackException(exception, flushSync);
        }

        private void OnUnhandledDispatcherException(DispatcherUnhandledExceptionEventArgs args)
        {
            var exception = args.Exception;
            if (exception is OperationCanceledException)
            {
                args.Handled = true;
                return;
            }
            TrackException(exception);
            LastError = exception;
            args.Handled = true;
        }

        public async Task OnExit()
        {
            await AutoSaveOpenDocuments().ConfigureAwait(false);
            _telemetryClient.Flush();
        }

        private void TrackException(Exception exception, bool flushSync = false)
        {
            // ReSharper disable once RedundantLogicalConditionalExpressionOperand
            if (SendTelemetry && ApplicationInsightsInstrumentationKey != null)
            {
                var typeLoadException = exception as ReflectionTypeLoadException;
                if (typeLoadException != null)
                {
                    exception = new AggregateException(exception.Message, typeLoadException.LoaderExceptions);
                }
                _telemetryClient.TrackException(exception);
                if (flushSync)
                {
                    _telemetryClient.Flush();
                }
            }
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

        public ChildProcessManager ChildProcessManager { get; }

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
            return Task.Run(() =>
            {
                _telemetryClient.TrackEvent(TelemetryEventNames.Feedback, new Dictionary<string, string>
                {
                    ["Content"] = feedbackText,
                    ["Email"] = email
                });
                _telemetryClient.Flush();
            });
        }
    }

    internal static class TelemetryEventNames
    {
        public const string Start = "Start";
        public const string Feedback = "Feedback";
    }
}