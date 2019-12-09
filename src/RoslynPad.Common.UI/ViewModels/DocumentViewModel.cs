using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using RoslynPad.Utilities;

namespace RoslynPad.UI
{
    [DebuggerDisplay("{Name}:{IsFolder}")]
    public class DocumentViewModel : NotificationObject
    {
        internal const string DefaultFileExtension = ".csx";
        internal const string AutoSaveSuffix = ".autosave";

        private bool _isExpanded;
        private bool? _isAutoSaveOnly;
        private bool _isSearchMatch;
        private string _path;
        private string _name;
        private string? _orderByName;
        private string _originalName;

#pragma warning disable CS8618 // Non-nullable field is uninitialized.
        private DocumentViewModel(string rootPath, bool isFolder)
#pragma warning restore CS8618 // Non-nullable field is uninitialized.
        {
            Path = rootPath;
            IsFolder = isFolder;
            IsAutoSave = Name.EndsWith(AutoSaveSuffix, StringComparison.OrdinalIgnoreCase);
            if (IsAutoSave)
            {
                Name = Name.Substring(0, Name.Length - AutoSaveSuffix.Length);
            }

            IsSearchMatch = true;
        }

        public void ChangePath(string newPath)
        {
            Path = newPath;
            _orderByName = null;
        }

        public string Path
        {
            get => _path;
            private set
            {
                var oldPath = _path;
                if (SetProperty(ref _path, value))
                {
                    OriginalName = System.IO.Path.GetFileName(value);
                    UpdateChildPaths(oldPath);
                }
            }
        }

        private void UpdateChildPaths(string newPath)
        {
            if (!IsFolder || !IsChildrenInitialized)
                return;

            foreach (var child in Children)
            {
                child.Path = child.Path.Replace(_path, newPath);
            }
        }

        public bool IsFolder { get; }

        public string GetSavePath()
        {
            return IsAutoSave
                // ReSharper disable once AssignNullToNotNullAttribute
                ? System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Path)!, Name + DefaultFileExtension)
                : Path;
        }

        public string GetAutoSavePath()
        {
            return IsAutoSave ?
                Path
                // ReSharper disable once AssignNullToNotNullAttribute
                : System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Path)!, GetAutoSaveName(Name));
        }

        public static string GetAutoSaveName(string name)
        {
            return name + AutoSaveSuffix + DefaultFileExtension;
        }

        public static DocumentViewModel CreateRoot(string rootPath)
        {
            IOUtilities.PerformIO(() => Directory.CreateDirectory(rootPath));
            return new DocumentViewModel(rootPath, true);
        }

        public static DocumentViewModel FromPath(string path)
        {
            return new DocumentViewModel(path, isFolder: Directory.Exists(path));
        }

        public DocumentViewModel CreateNew(string documentName)
        {
            if (!IsFolder) throw new InvalidOperationException("Parent must be a folder");

            var document = new DocumentViewModel(GetDocumentPathFromName(Path, documentName), false);
            AddChild(document);
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

        internal string OriginalName
        {
            get => _originalName;
            private set
            {
                _originalName = value;
                Name = IsFolder ? value : System.IO.Path.GetFileNameWithoutExtension(value);
            }
        }

        public string Name
        {
            get => _name;
            private set => SetProperty(ref _name, value);
        }

        public bool IsAutoSave { get; }

        public bool IsAutoSaveOnly
        {
            get
            {
                if (_isAutoSaveOnly == null)
                {
                    _isAutoSaveOnly = IsAutoSave &&
                                      // ReSharper disable once AssignNullToNotNullAttribute
                                      !File.Exists(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Path)!, Name + DefaultFileExtension));
                }

                return _isAutoSaveOnly.Value;
            }
        }

        public bool IsChildrenInitialized => InternalChildren != null;

        internal DocumentCollection InternalChildren { get; private set; }

        public ObservableCollection<DocumentViewModel> Children
        {
            get
            {
                if (IsFolder && InternalChildren == null)
                {
                    InternalChildren = ReadChildren();
                }

                return InternalChildren;
            }
        }

        public bool IsSearchMatch
        {
            get => _isSearchMatch;
            internal set => SetProperty(ref _isSearchMatch, value);
        }

        private DocumentCollection ReadChildren()
        {
            return new DocumentCollection(
                IOUtilities.EnumerateDirectories(Path)
                .Select(x => new DocumentViewModel(x, isFolder: true))
                .OrderBy(x => x.OrderByName)
                    .Concat(IOUtilities.EnumerateFiles(Path, "*" + DefaultFileExtension)
                        .Select(x => new DocumentViewModel(x, isFolder: false))
                        .Where(x => !x.IsAutoSave)
                        .OrderBy(x => x.OrderByName)));
        }

        private string OrderByName
        {
            get
            {
                if (_orderByName == null)
                {
                    _orderByName = Regex.Replace(Name, "[0-9]+", m => m.Value.PadLeft(100, '0'));
                }

                return _orderByName;
            }
        }

        internal void AddChild(DocumentViewModel documentViewModel)
        {
            var insertIndex = Children.IndexOf(d => d.IsFolder == documentViewModel.IsFolder &&
                                                    string.Compare(documentViewModel.OrderByName, d.OrderByName,
                                                        StringComparison.CurrentCulture) <= 0);
            if (insertIndex < 0)
            {
                insertIndex = documentViewModel.IsFolder ? Children.IndexOf(c => !c.IsFolder) : Children.Count;

                if (insertIndex < 0)
                {
                    insertIndex = 0;
                }
            }

            Children.Insert(insertIndex, documentViewModel);
        }
    }
}