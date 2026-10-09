#nullable enable

namespace Microsoft.VisualStudio.Text.Editor;

using System.ComponentModel;
using System.Diagnostics;
using System.Text;

using Avalonia.Threading;

using Microsoft.VisualStudio.Text;

/// <summary>
/// The text a buffer's changes are measured against, for the minimap's marks of added and changed lines
/// (<see cref="MinimapOptions.ChangeHighlightId"/>): the buffer's file as committed at git <c>HEAD</c>, where the file
/// is tracked in a git work tree and git can be run; otherwise the text last saved. A host that opens and saves files
/// names the file (<see cref="FilePath"/>) and reports its saves (<see cref="MarkSaved()"/>); a buffer with an
/// <see cref="ITextDocument"/> does both by itself. Until a save is reported, the buffer's text when its baseline was
/// first asked for counts as saved. The committed text is read, and watched for commits and checkouts, only while
/// something listens to <see cref="Changed"/>; <see cref="Dispose"/> ends that for a buffer that is closed.
/// </summary>
public sealed class TextChangeBaseline : IDisposable
{
    private static readonly TimeSpan s_settle = TimeSpan.FromMilliseconds(500);

    private readonly ITextBuffer _buffer;
    private readonly ITextDocument? _document;
    private string? _filePath;
    private string _saved;
    private string? _committed;
    private string? _gitDirectory;
    private FileSystemWatcher? _watcher;
    private DispatcherTimer? _settle;
    private EventHandler? _changed;
    private int _reads;

    private TextChangeBaseline(ITextBuffer buffer)
    {
        _buffer = buffer;
        _saved = buffer.CurrentSnapshot.GetText();
        if (buffer.Properties.TryGetProperty(typeof(ITextDocument), out ITextDocument document))
        {
            _document = document;
            _filePath = document.FilePath;
            document.FileActionOccurred += OnFileActionOccurred;
        }
    }

    /// <summary>Raised on the UI thread when the text the changes are measured against is another.</summary>
    public event EventHandler? Changed
    {
        add
        {
            bool first = _changed is null;
            _changed += value;
            if (first && _changed is not null)
            {
                ReadCommitted();
            }
        }

        remove
        {
            _changed -= value;
            if (_changed is null)
            {
                StopWatching();
            }
        }
    }

    /// <summary>The file the buffer holds, whose committed text the changes are measured against; null for none.</summary>
    public string? FilePath
    {
        get => _filePath;
        set
        {
            if (!string.Equals(value, _filePath, StringComparison.OrdinalIgnoreCase))
            {
                _filePath = value;
                if (_changed is not null)
                {
                    ReadCommitted();
                }
            }
        }
    }

    /// <summary>Whether the changes are measured against the file as committed rather than as last saved.</summary>
    public bool IsCommitted => _committed is not null;

    /// <summary>The text the changes are measured against: as committed where there is a commit, else as last saved.</summary>
    public string Text => _committed ?? _saved;

    /// <summary>The baseline of <paramref name="buffer"/>, made the first time it is asked for.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="buffer"/> is null.</exception>
    public static TextChangeBaseline For(ITextBuffer buffer)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        return buffer.Properties.GetOrCreateSingletonProperty(typeof(TextChangeBaseline), () => new TextChangeBaseline(buffer));
    }

    /// <summary>Takes the buffer's text as what its file now holds, and reads the committed text again.</summary>
    public void MarkSaved() => MarkSaved(_buffer.CurrentSnapshot.GetText());

    /// <summary>
    /// Takes <paramref name="text"/> as what the buffer's file now holds, and reads the committed text again: a save may
    /// come after a commit.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> is null.</exception>
    public void MarkSaved(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        bool changed = !IsCommitted && !string.Equals(text, _saved, StringComparison.Ordinal);
        _saved = text;
        if (_changed is not null)
        {
            ReadCommitted();
        }

        if (changed)
        {
            _changed?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>Stops watching the file's repository and lets go of its document; nothing is raised any more.</summary>
    public void Dispose()
    {
        _changed = null;
        StopWatching();
        if (_document is not null)
        {
            _document.FileActionOccurred -= OnFileActionOccurred;
        }
    }

    private void ReadCommitted()
    {
        int read = ++_reads;
        string? path = _filePath;
        if (string.IsNullOrEmpty(path))
        {
            TakeCommitted(read, default);
            return;
        }

        _ = Task.Run(() => GitHead.Read(path)).ContinueWith(
            task => Dispatcher.UIThread.Post(() => TakeCommitted(read, task.IsCompletedSuccessfully ? task.Result : default)),
            TaskScheduler.Default);
    }

    private void TakeCommitted(int read, (string? Text, string? GitDirectory) head)
    {
        // A newer read is on its way, or nothing listens any more.
        if (read != _reads || _changed is null)
        {
            return;
        }

        bool changed = !string.Equals(head.Text, _committed, StringComparison.Ordinal);
        _committed = head.Text;
        if (!string.Equals(head.GitDirectory, _gitDirectory, StringComparison.OrdinalIgnoreCase))
        {
            _gitDirectory = head.GitDirectory;
            Watch();
        }

        if (changed)
        {
            _changed?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Watches the repository's git directory: a commit, a checkout or a reset rewrites a file at its top, and the
    /// committed text is read again once the writes have settled.
    /// </summary>
    private void Watch()
    {
        _watcher?.Dispose();
        _watcher = null;
        if (_gitDirectory is null)
        {
            return;
        }

        try
        {
            _watcher = new FileSystemWatcher(_gitDirectory)
            {
                IncludeSubdirectories = false,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
            };
            _watcher.Changed += OnGitDirectoryChanged;
            _watcher.Created += OnGitDirectoryChanged;
            _watcher.Renamed += OnGitDirectoryChanged;
            _watcher.EnableRaisingEvents = true;
        }
        catch (Exception ex) when (ex is ArgumentException or IOException or PlatformNotSupportedException)
        {
            // Unwatched, the committed text is still read again on every reported save.
            _watcher?.Dispose();
            _watcher = null;
        }
    }

    private void StopWatching()
    {
        _reads++;
        _settle?.Stop();
        _watcher?.Dispose();
        _watcher = null;
        _gitDirectory = null;
    }

    private void OnGitDirectoryChanged(object sender, FileSystemEventArgs e)
    {
        if (e.Name is { } name && !name.EndsWith(".lock", StringComparison.OrdinalIgnoreCase))
        {
            Dispatcher.UIThread.Post(Settle);
        }
    }

    private void Settle()
    {
        if (_settle is null)
        {
            _settle = new DispatcherTimer { Interval = s_settle };
            _settle.Tick += (_, _) =>
            {
                _settle.Stop();
                if (_changed is not null)
                {
                    ReadCommitted();
                }
            };
        }

        _settle.Stop();
        _settle.Start();
    }

    private void OnFileActionOccurred(object? sender, TextDocumentFileActionEventArgs e)
    {
        if ((e.FileActionType & FileActionTypes.DocumentRenamed) != 0)
        {
            FilePath = e.FilePath;
        }

        if ((e.FileActionType & (FileActionTypes.ContentSavedToDisk | FileActionTypes.ContentLoadedFromDisk)) != 0)
        {
            MarkSaved();
        }
    }
}

/// <summary>Reads a file's committed text from git.</summary>
internal static class GitHead
{
    private const int TimeoutMilliseconds = 10_000;

    /// <summary>
    /// The text of <paramref name="path"/> at <c>HEAD</c> and the repository's git directory; both null where the file
    /// is in no work tree, is not in <c>HEAD</c>, or git cannot be run.
    /// </summary>
    public static (string? Text, string? GitDirectory) Read(string path)
    {
        try
        {
            string full = Path.GetFullPath(path);
            if (Path.GetDirectoryName(full) is not { } directory || !Directory.Exists(directory)
                || Run(directory, "rev-parse", "--show-toplevel", "--absolute-git-dir") is not { } located)
            {
                return default;
            }

            string[] parts = located.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length < 2)
            {
                return default;
            }

            string relative = Path.GetRelativePath(parts[0], full).Replace('\\', '/');
            if (relative.StartsWith("../", StringComparison.Ordinal) || Path.IsPathRooted(relative))
            {
                return default;
            }

            // The blob as stored, without the filters a checkout would run.
            string? text = Run(parts[0], "cat-file", "blob", "HEAD:" + relative);
            return text is null ? default : (text, Path.GetFullPath(parts[1]));
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return default;
        }
    }

    private static string? Run(string workingDirectory, params string[] arguments)
    {
        var start = new ProcessStartInfo("git")
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
        };
        foreach (string argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }

        using var process = Process.Start(start);
        if (process is null)
        {
            return null;
        }

        // Drained alongside, so a git that says much on its error stream cannot stall on a full pipe.
        var errors = process.StandardError.ReadToEndAsync();
        string output = process.StandardOutput.ReadToEnd();
        if (!process.WaitForExit(TimeoutMilliseconds))
        {
            process.Kill(entireProcessTree: true);
            return null;
        }

        errors.Wait(TimeoutMilliseconds);
        return process.ExitCode == 0 ? output : null;
    }
}
