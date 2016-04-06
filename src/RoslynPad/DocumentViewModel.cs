using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using RoslynPad.Utilities;

namespace RoslynPad
{
    internal class DocumentViewModel : NotificationObject
    {
        private const string DefaultFileExtension = ".csx";

        private ObservableCollection<DocumentViewModel> _children;
        private bool _isExpanded;

        private DocumentViewModel(MainViewModel mainViewModel)
        {
            MainViewModel = mainViewModel;
            var defaultPath = GetDefaultPath();
            Directory.CreateDirectory(defaultPath);
            Path = defaultPath;
            IsFolder = true;
        }

        public string Path { get; }
        public MainViewModel MainViewModel { get; }
        public bool IsFolder { get; }
        public bool IsNew { get; set; }

        public static DocumentViewModel CreateRoot(MainViewModel mainViewModel)
        {
            return new DocumentViewModel(mainViewModel);
        }

        private static string GetDefaultPath()
        {
            return System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "RoslynPad");
        }

        private DocumentViewModel(MainViewModel mainViewModel, string path, bool isFolder, bool isNew = false)
        {
            MainViewModel = mainViewModel;
            Path = path;
            IsFolder = isFolder;
            IsNew = isNew;
            if (!isNew)
            {
                Name = isFolder ? System.IO.Path.GetFileName(Path) : System.IO.Path.GetFileNameWithoutExtension(Path);
            }
            OpenDocumentCommand  = new DelegateCommand((Action)Open);
        }

        public DocumentViewModel CreateNew()
        {
            if (!IsFolder) throw new InvalidOperationException("Parent must be a folder");
            return new DocumentViewModel(MainViewModel, Path, isFolder: false, isNew: true);
        }

        private void Open()
        {
            if (!IsFolder)
            {
                MainViewModel.OpenDocument(this);
            }
        }

        public bool IsExpanded
        {
            get { return _isExpanded; }
            set { SetProperty(ref _isExpanded, value); }
        }

        public string Name { get; set; }

        public ObservableCollection<DocumentViewModel> Children
        {
            get
            {
                if (IsFolder && _children == null) _children = ReadChildren();
                return _children;
            }
        }

        public DelegateCommand OpenDocumentCommand { get; }

        private ObservableCollection<DocumentViewModel> ReadChildren()
        {
            try
            {
                return new ObservableCollection<DocumentViewModel>(
                    Directory.EnumerateDirectories(Path).Select(x => new DocumentViewModel(MainViewModel, x, isFolder: true)).OrderBy(x => x.Name)
                        .Concat(Directory.EnumerateFiles(Path, "*" + DefaultFileExtension)
                            .Select(x => new DocumentViewModel(MainViewModel, x, isFolder: false)).OrderBy(x => x.Name)));
            }
            catch (Exception)
            {
                return new ObservableCollection<DocumentViewModel>();
            }
        }
    }
}