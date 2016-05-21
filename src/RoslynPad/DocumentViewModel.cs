using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using RoslynPad.Utilities;

namespace RoslynPad
{
    internal class DocumentViewModel : NotificationObject
    {
        internal const string DefaultFileExtension = ".csx";
        internal const string AutoSaveSuffix = ".autosave";

        private ObservableCollection<DocumentViewModel> _children;
        private bool _isExpanded;
        private bool? _isAutoSaveOnly;

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

        public static string GetAutoSaveName(string name)
        {
            return name + AutoSaveSuffix + DefaultFileExtension;
        }

        public static DocumentViewModel CreateRoot(MainViewModel mainViewModel)
        {
            return new DocumentViewModel(mainViewModel);
        }

        public static DocumentViewModel CreateAutoSave(MainViewModel mainViewModel, string path)
        {
            return new DocumentViewModel(mainViewModel, path, isFolder: false);
        }

        private static string GetDefaultPath()
        {
            return System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "RoslynPad");
        }

        private DocumentViewModel(MainViewModel mainViewModel, string path, bool isFolder)
        {
            MainViewModel = mainViewModel;
            Path = path;
            IsFolder = isFolder;
            Name = isFolder ? System.IO.Path.GetFileName(Path) : System.IO.Path.GetFileNameWithoutExtension(Path);
            // ReSharper disable once PossibleNullReferenceException
            IsAutoSave = Name.EndsWith(AutoSaveSuffix, StringComparison.OrdinalIgnoreCase);
            if (IsAutoSave)
            {
                Name = Name.Substring(0, Name.Length - AutoSaveSuffix.Length);
            }
            OpenDocumentCommand = new DelegateCommand((Action)Open);
        }

        public static string GetDocumentPathFromName(string path, string name)
        {
            if (!name.EndsWith(DefaultFileExtension, StringComparison.OrdinalIgnoreCase))
            {
                name += DefaultFileExtension;
            }

            return System.IO.Path.Combine(path, name);
        }

        public DocumentViewModel CreateNew(string documentName)
        {
            if (!IsFolder) throw new InvalidOperationException("Parent must be a folder");

            var document = new DocumentViewModel(MainViewModel, GetDocumentPathFromName(Path, documentName), isFolder: false);

            var insertAfter = Children.FirstOrDefault(x => string.Compare(document.Path, x.Path, StringComparison.OrdinalIgnoreCase) >= 0);
            Children.Insert(insertAfter == null ? 0 : Children.IndexOf(insertAfter) + 1, document);
            return document;
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

        public bool IsAutoSave { get; }

        public bool IsAutoSaveOnly
        {
            get
            {
                if (_isAutoSaveOnly == null)
                {
                    _isAutoSaveOnly = IsAutoSave &&
                                      // ReSharper disable once AssignNullToNotNullAttribute
                                      !File.Exists(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Path), Name + DefaultFileExtension));
                }
                return _isAutoSaveOnly.Value;
            }
        }

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
                    Directory.EnumerateDirectories(Path)
                    .Select(x => new DocumentViewModel(MainViewModel, x, isFolder: true))
                    .OrderBy(OrderByName)
                        .Concat(Directory.EnumerateFiles(Path, "*" + DefaultFileExtension)
                            .Select(x => new DocumentViewModel(MainViewModel, x, isFolder: false))
                            .Where(x => !x.IsAutoSave)
                            .OrderBy(OrderByName)));
            }
            catch (Exception)
            {
                return new ObservableCollection<DocumentViewModel>();
            }
        }

        private static string OrderByName(DocumentViewModel x)
        {
            return Regex.Replace(x.Name, "[0-9]+", m => m.Value.PadLeft(100, '0'));
        }
    }
}