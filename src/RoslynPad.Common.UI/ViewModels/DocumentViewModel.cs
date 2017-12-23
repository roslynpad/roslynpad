using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using RoslynPad.UI.Services;
using RoslynPad.UI.Utilities;
using RoslynPad.Utilities;

namespace RoslynPad.UI
{
    [DebuggerDisplay("{Name}:{IsFolder}")]
    public class DocumentViewModel : NotificationObject
    {
        private readonly IDisposable _documentFileWatcherDisposable;
        private readonly DocumentFileWatcher _documentFileWatcher;
        internal const string DefaultFileExtension = ".csx";
        internal const string AutoSaveSuffix = ".autosave";

        private ObservableCollection<DocumentViewModel> _children;
        private bool _isExpanded;
        private bool? _isAutoSaveOnly;
        private bool _isSearchMatch;
        private string _path;
        private string _name;

        private DocumentViewModel(string rootPath, bool isFolder, DocumentFileWatcher documentFileWatcher)
        {
            _documentFileWatcher = documentFileWatcher;
            _documentFileWatcherDisposable = _documentFileWatcher?.Subscribe(OnDocumentFileChanged);
            Path = rootPath;
            IsFolder = isFolder;
            Name = System.IO.Path.GetFileName(Path);
            IsAutoSave = Name.EndsWith(AutoSaveSuffix, StringComparison.OrdinalIgnoreCase);
            if (IsAutoSave)
            {
                Name = Name.Substring(0, Name.Length - AutoSaveSuffix.Length);
            }

            IsSearchMatch = true;
        }

        public string Path
        {
            get => _path;
            private set
            {
                var oldPath = _path;
                if (SetProperty (ref _path, value))
                {
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

        public static DocumentViewModel CreateRoot(string rootPath, DocumentFileWatcher documentFileWatcher)
        {
            IOUtilities.PerformIO(() => Directory.CreateDirectory(rootPath));
            return new DocumentViewModel(rootPath, true, documentFileWatcher);
        }

        public static DocumentViewModel FromPath(string path)
        {
            return new DocumentViewModel(path, false, null);
        }

        public DocumentViewModel CreateNew(string documentName)
        {
            if (!IsFolder) throw new InvalidOperationException("Parent must be a folder");

            var document = new DocumentViewModel(GetDocumentPathFromName(Path, documentName), false, _documentFileWatcher);
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

        public string Name
        {
            get => _name;
            set
            {
                if (!IsFolder)
                {
                    value = System.IO.Path.GetFileNameWithoutExtension(value);
                }

                SetProperty(ref _name, value);
            }
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
                                      !File.Exists(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Path), Name + DefaultFileExtension));
                }

                return _isAutoSaveOnly.Value;
            }
        }

        public bool IsChildrenInitialized => _children != null;

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
                .Select(x => new DocumentViewModel(x, true, _documentFileWatcher))
                .OrderBy(OrderByName)
                    .Concat(IOUtilities.EnumerateFiles(Path, "*" + DefaultFileExtension)
                        .Select(x => new DocumentViewModel(x, false, _documentFileWatcher))
                        .Where(x => !x.IsAutoSave)
                        .OrderBy(OrderByName)));
        }

        private static string OrderByName(DocumentViewModel x)
        {
            return Regex.Replace(x.Name, "[0-9]+", m => m.Value.PadLeft(100, '0'));
        }

        private void AddChild (DocumentViewModel documentViewModel)
        {
            Children.Add(documentViewModel);
            Children.Sort(d => d.OrderBy(dd => !dd.IsFolder).ThenBy(OrderByName));
        }

        public void OnDocumentFileChanged(DocumentFileChanged value)
        {
            if (!IsFolder && string.Equals(Path, value.Path, StringComparison.Ordinal))
            {
                return;
            }
            if (IsFolder && string.Equals(Path, value.Path, StringComparison.Ordinal) && System.IO.Path.GetDirectoryName(value.Path) != Path)
            {
                return;
            }

            switch (value.Type)
            {
            case DocumentFileChangeType.Created:
                OnDocumentCreated(value);
                break;
            case DocumentFileChangeType.Deleted:
                OnDocumentDeleted(value);
                break;
            case DocumentFileChangeType.Renamed:
                OnDocumentRenamed(value);
                break;
            }
        }

        private void OnDocumentRenamed(DocumentFileChanged value)
        {
            if (Path != value.Path)
                return; //Rename only applies to self

            Path = value.NewPath;
            Name = System.IO.Path.GetFileName(value.NewPath);
        }

        private void OnDocumentCreated(DocumentFileChanged value)
        {
            if (!IsFolder || !IsChildrenInitialized)
                return;

            if (Children.Any(d => string.Equals(d.Path, value.Path, StringComparison.Ordinal))) //if already added for some strange reason
                return;

            //We only add supported files
            var isFolder = Directory.Exists (value.Path);
            if (System.IO.Path.GetExtension(value.Path) != DefaultFileExtension && !isFolder)
                return;

            AddChild(new DocumentViewModel(value.Path, isFolder, _documentFileWatcher));
        }

        private void OnDocumentDeleted(DocumentFileChanged value)
        {
            if (value.Path == Path)
            {
                //Since this document was removed it no longer needs to watch it's status
                _documentFileWatcherDisposable.Dispose();
                return;
            }

            if (IsChildrenInitialized)
            {
                var child = Children.FirstOrDefault(c => string.Equals(c.Path, value.Path, StringComparison.Ordinal));
                Children.Remove(child);
            }
        }
    }
}