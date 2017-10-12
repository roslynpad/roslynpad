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
        private readonly DocumentWatcher _documentWatcher;
        internal const string DefaultFileExtension = ".csx";
        internal const string AutoSaveSuffix = ".autosave";

        private ObservableCollection<DocumentViewModel> _children;
        private bool _isExpanded;
        private bool? _isAutoSaveOnly;
        private bool _isSearchMatch;
        private string _path;
        private string _name;

        private DocumentViewModel(string rootPath, DocumentWatcher documentWatcher)
        {
            _documentWatcher = documentWatcher;
            documentWatcher.Subscribe (rootPath, OnDirectoryChanged);
            Name = System.IO.Path.GetFileName(rootPath);
            Path = rootPath;
            IOUtilities.PerformIO(() => Directory.CreateDirectory(Path));
            IsFolder = true;
            IsSearchMatch = true;
        }

        private DocumentViewModel(string filePath)
        {
            Path = filePath;
            IsFolder = false;
            Name = System.IO.Path.GetFileNameWithoutExtension(Path);
            // ReSharper disable once PossibleNullReferenceException
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
            set
            {
                SetProperty (ref _path, value);
                if (IsFolder)
                {
                    //TODO: update subitems
                }
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

        public static DocumentViewModel CreateRoot(string rootPath, DocumentWatcher documentWatcher)
        {
            return new DocumentViewModel(rootPath, documentWatcher);
        }

        public static DocumentViewModel FromPath(string path)
        {
            return new DocumentViewModel(path);
        }

        public DocumentViewModel CreateNew(string documentName)
        {
            if (!IsFolder) throw new InvalidOperationException("Parent must be a folder");

            var document = new DocumentViewModel(GetDocumentPathFromName(Path, documentName));
            Children.Add(document);
            Children.Sort(SortPredicate);
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
            set => SetProperty(ref _name, value);
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
                .Select(x => new DocumentViewModel(x, _documentWatcher))
                .OrderBy(OrderByName)
                    .Concat(IOUtilities.EnumerateFiles(Path, "*" + DefaultFileExtension)
                        .Select(x => new DocumentViewModel(x))
                        .Where(x => !x.IsAutoSave)
                        .OrderBy(OrderByName)));
        }

        private static string OrderByName(DocumentViewModel x)
        {
            return Regex.Replace(x.Name, "[0-9]+", m => m.Value.PadLeft(100, '0'));
        }

        private static Func<IEnumerable<DocumentViewModel>, IOrderedEnumerable<DocumentViewModel>> SortPredicate =>
            d => d.OrderBy (dd => !dd.IsFolder).ThenBy (OrderByName);

        private void OnDirectoryChanged(DocumentWatcher.DocumentWatcherArgs args)
        {
            switch (args.ChangeType)
            {
            case DocumentWatcher.ChangeType.Created:
            {
                if (_children.Any (d => d.Path == args.Path))
                    return;
                if (IOUtilities.IsDirectory(args.Path))
                {
                    _children.Add(new DocumentViewModel(args.Path, _documentWatcher));
                }
                else if(System.IO.Path.GetExtension(args.Path) == ".csx")
                {
                    _children.Add(new DocumentViewModel(args.Path));
                }
                _children.Sort(SortPredicate);
            }
                break;
            case DocumentWatcher.ChangeType.Deleted:
            {
                var child = _children.Single(c => c.Path == args.Path);
                _children.Remove(child);
            }
                break;
            case DocumentWatcher.ChangeType.Renamed:
            {
                var child = _children.Single(c => c.Path == args.OldPath);
                child.Name = args.Name;
                child.Path = args.Path;
            }
                break;
            }
        }
    }
}