using System.Collections.ObjectModel;
using RoslynPad.Roslyn;
using RoslynPad.Utilities;

namespace RoslynPad
{
    internal sealed class MainViewModel : NotificationObject
    {
        public const string NuGetPathVariableName = "$NuGet";

        public RoslynHost RoslynHost { get; }

        public MainViewModel()
        {
            NuGet = new NuGetViewModel();
            RoslynHost = new RoslynHost(new NuGetProvider(NuGet.GlobalPackageFolder, NuGetPathVariableName));
            Documents = DocumentViewModel.CreateRoot(this).Children;
            OpenDocuments = new ObservableCollection<OpenDocumentViewModel>();
        }

        public NuGetViewModel NuGet { get; }

        public ObservableCollection<OpenDocumentViewModel> OpenDocuments { get; }

        public ObservableCollection<DocumentViewModel> Documents { get; }

        public void OpenDocument(DocumentViewModel document)
        {
            OpenDocuments.Add(new OpenDocumentViewModel(document));
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
    }
}