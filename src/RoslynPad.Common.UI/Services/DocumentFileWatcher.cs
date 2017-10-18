using System;
using System.Collections.Generic;
using System.Composition;
using System.IO;
using RoslynPad.Utilities;

namespace RoslynPad.UI.Services
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

    [Export, Shared]
    public class DocumentFileWatcher : IDisposable, IObservable<DocumentFileChanged>
    {
        private readonly IAppDispatcher _appDispatcher;
        private readonly FileSystemWatcher _fileSystemWatcher;
        private readonly List<IObserver<DocumentFileChanged>> _observers = new List<IObserver<DocumentFileChanged>>();

        private class Unsubscriber<T> : IDisposable
        {
            private readonly List<IObserver<T>> _observers;
            private readonly IObserver<T> _observer;

            public Unsubscriber(List<IObserver<T>> observers, IObserver<T> observer)
            {
                _observers = observers;
                _observer = observer;
            }
            public void Dispose() => _observers.Remove(_observer);
        }

        [ImportingConstructor]
        public DocumentFileWatcher(IAppDispatcher appDispatcher)
        {
            _appDispatcher = appDispatcher;
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
                _fileSystemWatcher.Path = value;
                _fileSystemWatcher.EnableRaisingEvents = IOUtilities.IsDirectory(_fileSystemWatcher.Path);
            }
        }

        private void OnChanged(object sender, FileSystemEventArgs e) => Publish(new DocumentFileChanged(ToDocumentFileChangeType(e.ChangeType), e.FullPath));

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

        private void OnRenamed(object sender, RenamedEventArgs e) => Publish(new DocumentFileChanged(ToDocumentFileChangeType(e.ChangeType), e.OldFullPath, e.FullPath));

        private void Publish(DocumentFileChanged documentFileChanged)
        {
            foreach (var observer in _observers)
            {
                _appDispatcher.InvokeAsync(() => observer.OnNext(documentFileChanged));
            }
        }

        public void Dispose()
        {
            _fileSystemWatcher?.Dispose();
            GC.SuppressFinalize(this);
        }

        public IDisposable Subscribe(IObserver<DocumentFileChanged> observer)
        {
            if (!_observers.Contains(observer))
                _observers.Add(observer);

            return new Unsubscriber<DocumentFileChanged>(_observers, observer);
        }

        ~DocumentFileWatcher() => Dispose();
    }
}
