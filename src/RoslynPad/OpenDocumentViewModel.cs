using RoslynPad.Utilities;

namespace RoslynPad
{
    internal class OpenDocumentViewModel : NotificationObject
    {
        private readonly DocumentViewModel _document;

        public OpenDocumentViewModel(DocumentViewModel document)
        {
            _document = document;
        }

        public NuGetViewModel NuGet => _document.MainViewModel.NuGet;

        public MainViewModel MainViewModel => _document.MainViewModel;

        public string Title => _document.Name;
    }
}