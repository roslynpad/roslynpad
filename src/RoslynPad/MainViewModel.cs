using System;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Microsoft.ApplicationInsights;
using RoslynPad.Roslyn;
using RoslynPad.Utilities;

namespace RoslynPad
{
    internal sealed class MainViewModel : NotificationObject
    {
        private const string ApplicationInsightsInstrumentationKey = "86551688-26d9-4124-8376-3f7ddcf84b8e";
        public const string NuGetPathVariableName = "$NuGet";

        private readonly Lazy<TelemetryClient> _client;
        public DocumentViewModel DocumentRoot { get; }
        private Exception _lastError;
        private OpenDocumentViewModel _currentOpenDocument;
        public INuGetProvider NuGetProvider { get; }

        public RoslynHost RoslynHost { get; }

        public MainViewModel()
        {
            NuGet = new NuGetViewModel();
            NuGetProvider = new NuGetProviderImpl(NuGet.GlobalPackageFolder, NuGetPathVariableName);
            RoslynHost = new RoslynHost(NuGetProvider);

            DocumentRoot = CreateDocumentRoot();
            Documents = DocumentRoot.Children;
            OpenDocuments = new ObservableCollection<OpenDocumentViewModel>();
            NewDocumentCommand = new DelegateCommand((Action)CreateNewDocument);
            CloseCurrentDocumentCommand = new DelegateCommand((Action)CloseCurrentDocument);
            ClearErrorCommand = new DelegateCommand(() => LastError = null);
            CreateNewDocument();

            _client = new Lazy<TelemetryClient>(() => new TelemetryClient { InstrumentationKey = ApplicationInsightsInstrumentationKey });
            Application.Current.DispatcherUnhandledException += (o, e) => OnUnhandledDispatcherException(e);
            Application.Current.Exit += (o, e) => OnExit();
            AppDomain.CurrentDomain.UnhandledException += (o, e) => OnUnhandledException((Exception)e.ExceptionObject, flushSync: true);
            TaskScheduler.UnobservedTaskException += (o, e) => OnUnhandledException(e.Exception);
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

        public void CloseDocument(OpenDocumentViewModel document)
        {
            // TODO: Save
            RoslynHost.CloseDocument(document.DocumentId);
            OpenDocuments.Remove(document);
            document.Close();
        }

        private void CloseCurrentDocument()
        {
            if (CurrentOpenDocument != null)
            {
                CloseDocument(CurrentOpenDocument);
            }
        }

        private void OnUnhandledException(Exception exception, bool flushSync = false)
        {
            TrackException(exception, flushSync);
        }

        private void OnUnhandledDispatcherException(DispatcherUnhandledExceptionEventArgs args)
        {
            TrackException(args.Exception);
            LastError = args.Exception;
            args.Handled = true;
        }

        private void OnExit()
        {
            if (_client.IsValueCreated)
            {
                _client.Value.Flush();
            }
        }

        private void TrackException(Exception exception, bool flushSync = false)
        {
            // ReSharper disable once RedundantLogicalConditionalExpressionOperand
            if (SendErrors && ApplicationInsightsInstrumentationKey != null)
            {
                _client.Value.TrackException(exception);
                if (flushSync)
                {
                    _client.Value.Flush();
                }
                // TODO: check why this freezes the UI
                //else
                //{
                //    Task.Run(() => _client.Value.Flush());
                //}
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

        public bool SendErrors
        {
            get { return Properties.Settings.Default.SendErrors; }
            set
            {
                Properties.Settings.Default.SendErrors = value;
                Properties.Settings.Default.Save();
                OnPropertyChanged(nameof(SendErrors));
            }
        }

        [Serializable]
        class NuGetProviderImpl : INuGetProvider
        {
            public NuGetProviderImpl(string pathToRepository, string pathVariableName)
            {
                PathToRepository = pathToRepository;
                PathVariableName = pathVariableName;
            }

            public string PathToRepository { get; }
            public string PathVariableName { get; }
        }

        public DocumentViewModel AddDocument(string documentName)
        {
            return DocumentRoot.CreateNew(documentName);
        }
    }
}