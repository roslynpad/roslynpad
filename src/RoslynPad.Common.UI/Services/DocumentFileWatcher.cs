using System;
using System.Collections.Generic;
using System.Composition;
using System.IO;
using RoslynPad.UI.Utilities;

namespace RoslynPad.UI
{
    public enum DocumentFileChangeType
    {
        Created,
        Deleted,
        Renamed
    }

    public class DocumentFileChanged
    {
        public DocumentFileChanged(DocumentFileChangeType type, string path, string newPath = null)
        {
            Type = type;
            Path = path;
            NewPath = newPath;
        }
        public DocumentFileChangeType Type { get; }
        public string Path { get; }
        public string NewPath { get; }
    }

    [Export]
    public class DocumentFileWatcher : IDisposable, IObservable<DocumentFileChanged>
    {
        private readonly IAppDispatcher _appDispatcher;
        private readonly FileSystemWatcher _fileSystemWatcher;
        private readonly List<IObserver<DocumentFileChanged>> _observers;

        [ImportingConstructor]
        public DocumentFileWatcher(IAppDispatcher appDispatcher)
        {
            _appDispatcher = appDispatcher;
            _observers = new List<IObserver<DocumentFileChanged>>();
            _fileSystemWatcher = new FileSystemWatcher();
            _fileSystemWatcher.Created += OnChanged;
            _fileSystemWatcher.Renamed += OnRenamed;
            _fileSystemWatcher.Deleted += OnChanged;
            _fileSystemWatcher.IncludeSubdirectories = true;
        }
        
        public string Path
        {
            get => _fileSystemWatcher.Path;
            set
            {
                var exists = Directory.Exists(value);
                if (exists)
                {
                    _fileSystemWatcher.Path = value;
                    _fileSystemWatcher.EnableRaisingEvents = true;
                }
                else
                {
                    _fileSystemWatcher.EnableRaisingEvents = false;
                }

                _observers.Clear(); // Most likely root has changed
            }
        }

        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            Publish(new DocumentFileChanged(ToDocumentFileChangeType(e.ChangeType), e.FullPath));
        }

        private DocumentFileChangeType ToDocumentFileChangeType(WatcherChangeTypes changeType)
        {
            switch (changeType)
            {
                case WatcherChangeTypes.Created:
                    return DocumentFileChangeType.Created;
                case WatcherChangeTypes.Deleted:
                    return DocumentFileChangeType.Deleted;
                case WatcherChangeTypes.Renamed:
                    return DocumentFileChangeType.Renamed;
                default:
                    throw new ArgumentOutOfRangeException(nameof(changeType), changeType, null);
            }
        }

        private void OnRenamed(object sender, RenamedEventArgs e)
        {
            Publish(new DocumentFileChanged(ToDocumentFileChangeType(e.ChangeType), e.OldFullPath, e.FullPath));
        }

        private void Publish(DocumentFileChanged documentFileChanged)
        {
            foreach (var observer in _observers.ToArray())
            {
                _appDispatcher.InvokeAsync(() => observer.OnNext(documentFileChanged));
            }
        }

        public void Dispose()
        {
            _fileSystemWatcher?.Dispose();
        }

        public IDisposable Subscribe(IObserver<DocumentFileChanged> observer)
        {
            if (!_observers.Contains(observer))
                _observers.Add(observer);

            return new Disposer(() => _observers.Remove(observer));
        }
    }
}
