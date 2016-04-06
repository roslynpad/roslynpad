using System.Collections.ObjectModel;
using System.Linq;
using RoslynPad.Roslyn;
using RoslynPad.Utilities;

namespace RoslynPad
{
    internal sealed class MainViewModel : NotificationObject
    {
        private OpenDocumentViewModel _currentOpenDocument;
        public const string NuGetPathVariableName = "$NuGet";

        public RoslynHost RoslynHost { get; }

        public MainViewModel()
        {
            NuGet = new NuGetViewModel();
            RoslynHost = new RoslynHost(new NuGetProvider(NuGet.GlobalPackageFolder, NuGetPathVariableName));
            Documents = DocumentViewModel.CreateRoot(this).Children;
            OpenDocuments = new ObservableCollection<OpenDocumentViewModel>();
            CreateNewDocument();
        }

        public NuGetViewModel NuGet { get; }

        public ObservableCollection<OpenDocumentViewModel> OpenDocuments { get; }

        public OpenDocumentViewModel CurrentOpenDocument
        {
            get { return _currentOpenDocument; }
            set { SetProperty(ref _currentOpenDocument, value); }
        }

        public ObservableCollection<DocumentViewModel> Documents { get; }

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
            OpenDocuments.Add(new OpenDocumentViewModel(this, null));
        }

        class NuGetProvider : INuGetProvider
        {
            public NuGetProvider(string pathToRepository, string pathVariableName)
            {
                PathToRepository = pathToRepository;
                PathVariableName = pathVariableName;
            }

            public string PathToRepository { get; }
            public string PathVariableName { get; }
        }

        public void CloseDocument(OpenDocumentViewModel content)
        {
            // TODO: Save
            // TODO: stop Roslyn services
            OpenDocuments.Remove(content);
        }
    }
}