using System.Composition;
using RoslynPad.UI.Utilities;

namespace RoslynPad.UI;

public enum DocumentFileChangeType
{
    Created,
    Deleted,
    Renamed
}

public class DocumentFileChanged(DocumentFileChangeType type, string path, string? newPath = null)
{
    public DocumentFileChangeType Type { get; } = type;
    public string Path { get; } = path;
    public string? NewPath { get; } = newPath;
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
        _observers = [];
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

    private void OnChanged(object? sender, FileSystemEventArgs e)
    {
        Publish(new DocumentFileChanged(ToDocumentFileChangeType(e.ChangeType), e.FullPath));
    }

    private DocumentFileChangeType ToDocumentFileChangeType(WatcherChangeTypes changeType)
    {
        return changeType switch
        {
            WatcherChangeTypes.Created => DocumentFileChangeType.Created,
            WatcherChangeTypes.Deleted => DocumentFileChangeType.Deleted,
            WatcherChangeTypes.Renamed => DocumentFileChangeType.Renamed,
            _ => throw new ArgumentOutOfRangeException(nameof(changeType), changeType, null),
        };
    }

    private void OnRenamed(object? sender, RenamedEventArgs e)
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
