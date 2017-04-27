using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Data;
using RoslynPad.Utilities;

namespace RoslynPad.UI
{
    public class DocumentViewModel : NotificationObject
    {
        internal const string DefaultFileExtension = ".csx";
        internal const string AutoSaveSuffix = ".autosave";

        private ObservableCollection<DocumentViewModel> _children;
        private bool _isExpanded;
        private bool? _isAutoSaveOnly;
        private bool _isSearchMatch;
        private ICollectionView _childrenView;

        private DocumentViewModel(string rootPath)
        {
            Path = rootPath;
            IOUtilities.PerformIO(() => Directory.CreateDirectory(Path));
            IsFolder = true;
            IsSearchMatch = true;
        }

        private DocumentViewModel(string path, bool isFolder)
        {
            Path = path;
            IsFolder = isFolder;
            Name = isFolder ? System.IO.Path.GetFileName(Path) : System.IO.Path.GetFileNameWithoutExtension(Path);
            // ReSharper disable once PossibleNullReferenceException
            IsAutoSave = Name.EndsWith(AutoSaveSuffix, StringComparison.OrdinalIgnoreCase);
            if (IsAutoSave)
            {
                Name = Name.Substring(0, Name.Length - AutoSaveSuffix.Length);
            }

            IsSearchMatch = true;
        }

        public string Path { get; set; }

        public bool IsFolder { get; }

        public string GetSavePath()
        {
            return IsAutoSave
                // ReSharper disable once AssignNullToNotNullAttribute
                ? System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Path), Name + DefaultFileExtension)
                : Path;
        }

        public string GetAutoSavePath()
        {
            return IsAutoSave ?
                Path
                // ReSharper disable once AssignNullToNotNullAttribute
                : System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Path), GetAutoSaveName(Name));
        }

        public static string GetAutoSaveName(string name)
        {
            return name + AutoSaveSuffix + DefaultFileExtension;
        }

        public static DocumentViewModel CreateRoot(string rootPath)
        {
            return new DocumentViewModel(rootPath);
        }

        public static DocumentViewModel CreateAutoSave(string path)
        {
            return new DocumentViewModel(path, isFolder: false);
        }

        public DocumentViewModel CreateNew(string documentName)
        {
            if (!IsFolder) throw new InvalidOperationException("Parent must be a folder");

            var document = new DocumentViewModel(GetDocumentPathFromName(Path, documentName), isFolder: false);

            var insertAfter = Children.FirstOrDefault(x => string.Compare(document.Path, x.Path, StringComparison.OrdinalIgnoreCase) >= 0);
            Children.Insert(insertAfter == null ? 0 : Children.IndexOf(insertAfter) + 1, document);
            return document;
        }

        public static string GetDocumentPathFromName(string path, string name)
        {
            if (!name.EndsWith(DefaultFileExtension, StringComparison.OrdinalIgnoreCase))
            {
                name += DefaultFileExtension;
            }

            return System.IO.Path.Combine(path, name);
        }

        public void DeleteAutoSave()
        {
            if (IsAutoSave)
            {
                IOUtilities.PerformIO(() => File.Delete(Path));
            }
            else
            {
                var autoSavePath = GetAutoSavePath();
                if (File.Exists(autoSavePath))
                {
                    IOUtilities.PerformIO(() => File.Delete(autoSavePath));
                }
            }
        }

        public bool IsExpanded
        {
            get => _isExpanded;
            set => SetProperty(ref _isExpanded, value);
        }

        public string Name { get; }

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

        public ICollectionView ChildrenView
        {
            get
            {
                if (_childrenView == null)
                {
                    var childrenView = new ListCollectionView(Children);
                    childrenView.LiveFilteringProperties.Add(nameof(IsSearchMatch));
                    childrenView.IsLiveFiltering = true;
                    childrenView.Filter = o => ((DocumentViewModel)o).IsSearchMatch;
                    _childrenView = childrenView;
                }

                return _childrenView;
            }
        }

        public ObservableCollection<DocumentViewModel> Children
        {
            get
            {
                if (IsFolder && _children == null)
                {
                    _children = ReadChildren();
                }

                return _children;
            }
        }

        public bool IsSearchMatch
        {
            get => _isSearchMatch;
            internal set => SetProperty(ref _isSearchMatch, value);
        }

        private ObservableCollection<DocumentViewModel> ReadChildren()
        {
            return new ObservableCollection<DocumentViewModel>(
                IOUtilities.EnumerateDirectories(Path)
                .Select(x => new DocumentViewModel(x, isFolder: true))
                .OrderBy(OrderByName)
                    .Concat(IOUtilities.EnumerateFiles(Path, "*" + DefaultFileExtension)
                        .Select(x => new DocumentViewModel(x, isFolder: false))
                        .Where(x => !x.IsAutoSave)
                        .OrderBy(OrderByName)));
        }

        private static string OrderByName(DocumentViewModel x)
        {
            return Regex.Replace(x.Name, "[0-9]+", m => m.Value.PadLeft(100, '0'));
        }
    }
}