using System;
using System.Collections.Generic;
using System.Composition;
using System.IO;
using System.Linq;

namespace RoslynPad.UI.Services
{
    [Export, Shared]
    public class DocumentWatcher : IDisposable
    {
        public enum ChangeType
        {
            Created,
            Deleted,
            Renamed
        }

        public class DocumentWatcherArgs
        {
            public DocumentWatcherArgs (ChangeType changeType, string path, string name, string oldPath = null, string oldName = null)
            {
                ChangeType = changeType;
                Path = path;
                Name = name;
                OldPath = oldPath;
                OldName = oldName;
            }

            public string OldName { get; }

            public string OldPath { get; }

            public string Name { get; }

            public ChangeType ChangeType { get; }

            public string Path { get; }
        }

        private class Subscriber
        {
            public WeakReference<object> Object { get; set; }
            public Action<DocumentWatcherArgs> Action { get; set; }
        }
        private readonly IAppDispatcher _appDispatcher;
        private readonly Dictionary<string, List<Subscriber>> _subscribers = new Dictionary<string, List<Subscriber>>();
        private readonly FileSystemWatcher _fileSystemWatcher;

        [ImportingConstructor]
        public DocumentWatcher(IAppDispatcher appDispatcher)
        {
            _appDispatcher = appDispatcher;
            _fileSystemWatcher = new FileSystemWatcher();
            _fileSystemWatcher.Created += OnDocumentsChanged;
            _fileSystemWatcher.Renamed += OnDocumentRenamed;
            _fileSystemWatcher.Deleted += OnDocumentsChanged;
            _fileSystemWatcher.IncludeSubdirectories = true;
        }

        private void OnDocumentRenamed (object sender, RenamedEventArgs e)
        {
            foreach (var subscriber in GetSubscribers(e.OldFullPath))
            {
                _appDispatcher.InvokeAsync(() => subscriber.Action.Invoke(new DocumentWatcherArgs(ToChangeType(e.ChangeType), e.FullPath, e.Name, e.OldFullPath, e.OldName)));
            }
            CleanDeadSubscribers ();
        }

        private void CleanDeadSubscribers ()
        {
            foreach (var path in _subscribers.Keys)
            {
                var subs = _subscribers [path];
                var deadsubs = subs.Where (s => !s.Object.TryGetTarget (out var _)).ToList ();
                foreach (var subscriber in deadsubs)
                {
                    subs.Remove(subscriber);
                }
                //TODO: remove empty key
            }
        }

        private IEnumerable<Subscriber> GetSubscribers (string path)
        {
            if (!_subscribers.TryGetValue (path, out var subscribers))
                return Enumerable.Empty<Subscriber>();
            return subscribers.Where (s => s.Object.TryGetTarget (out var _));
        }

        private void OnDocumentsChanged(object sender, FileSystemEventArgs e)
        {
            var path = Path.GetDirectoryName (e.FullPath);
            foreach (var subscriber in GetSubscribers(path))
            {
                _appDispatcher.InvokeAsync(() => subscriber.Action.Invoke(new DocumentWatcherArgs(ToChangeType(e.ChangeType), e.FullPath, e.Name)));
            }
            CleanDeadSubscribers();
        }

        private ChangeType ToChangeType (WatcherChangeTypes eChangeType)
        {
            switch (eChangeType)
            {
            case WatcherChangeTypes.Created:
                return ChangeType.Created;
            case WatcherChangeTypes.Deleted:
                return ChangeType.Deleted;
            case WatcherChangeTypes.Renamed:
                return ChangeType.Renamed;
            default:
                throw new ArgumentOutOfRangeException (nameof(eChangeType), eChangeType, null);
            }
        }

        public void Subscribe(string path, Action<DocumentWatcherArgs> action)
        {
            if (!_subscribers.ContainsKey (path))
            {
                _subscribers.Add(path, new List<Subscriber> ());
            }
            var subscribers = _subscribers [path];
            subscribers.Add(new Subscriber
            {
                Action = action,
                Object = new WeakReference<object> (action.Target) //TODO: static actions
            });

            if(string.IsNullOrWhiteSpace(_fileSystemWatcher.Path) || Path.GetFullPath(_fileSystemWatcher.Path).StartsWith(Path.GetFullPath(path)))
                _fileSystemWatcher.Path = path;
            _fileSystemWatcher.EnableRaisingEvents = true;
        }

        public void Dispose()
        {
            _fileSystemWatcher?.Dispose();
            GC.SuppressFinalize(this);
        }

        ~DocumentWatcher()
        {
            Dispose();
        }
    }
}
