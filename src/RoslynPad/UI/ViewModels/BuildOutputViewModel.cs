using System.Text;
using RoslynPad.Build;

namespace RoslynPad.UI;

/// <summary>
/// Backing store for the build output pane: one append-only text document per build phase,
/// with the selection auto-switching to the phase that is actively producing output.
/// </summary>
public class BuildOutputViewModel : NotificationObject
{
    private readonly IAppDispatcher _dispatcher;

    internal BuildOutputViewModel(IAppDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
        Restore = new BuildOutputDocument("Restore");
        Compile = new BuildOutputDocument("Compile");
        Documents = [Restore, Compile];
        SelectedDocument = Restore;
    }

    public BuildOutputDocument Restore { get; }
    public BuildOutputDocument Compile { get; }
    public IReadOnlyList<BuildOutputDocument> Documents { get; }

    public BuildOutputDocument SelectedDocument
    {
        get;
        set => SetProperty(ref field, value);
    }

    /// <summary>
    /// The sink factory handed to the execution host; called on background threads when a
    /// build phase starts producing output. A phase that actually runs steals the selection;
    /// a cached restore replay does not (the interesting output is the compile that follows).
    /// </summary>
    internal TextWriter CreateWriter(BuildOutputSource source, bool cached)
    {
        var document = source == BuildOutputSource.Restore ? Restore : Compile;
        document.Reset();
        _dispatcher.InvokeAsync(() =>
        {
            document.IsCached = cached;
            if (!cached)
            {
                SelectedDocument = document;
            }
        });
        return document.CreateWriter();
    }
}

/// <summary>
/// An append-only text document. Writes arrive on background threads; consumers observe
/// <see cref="Changed"/> (raised on the writing thread, coalescing expected) and pull the
/// new text with <see cref="ReadFrom"/>.
/// </summary>
public sealed class BuildOutputDocument(string name) : NotificationObject
{
    private readonly StringBuilder _text = new();
    private readonly Lock _sync = new();
    private int _generation = 1;

    public string Name { get; } = name;

    public string DisplayName => IsCached ? $"{Name} (cached)" : Name;

    public bool IsCached
    {
        get;
        internal set
        {
            if (SetProperty(ref field, value))
            {
                OnPropertyChanged(nameof(DisplayName));
            }
        }
    }

    /// <summary>Raised after the text changes, possibly on a background thread.</summary>
    public event Action? Changed;

    public readonly record struct ReadPosition(int Generation, int Offset);

    /// <summary>
    /// Returns the text appended since <paramref name="position"/>, or the full text with
    /// <c>Restarted</c> set when the document was reset (or the position is unknown).
    /// </summary>
    public (string Text, bool Restarted, ReadPosition Position) ReadFrom(ReadPosition position)
    {
        lock (_sync)
        {
            var restarted = position.Generation != _generation;
            var offset = restarted ? 0 : Math.Min(position.Offset, _text.Length);
            return (_text.ToString(offset, _text.Length - offset), restarted, new ReadPosition(_generation, _text.Length));
        }
    }

    internal void Reset()
    {
        lock (_sync)
        {
            _text.Clear();
            _generation++;
        }

        Changed?.Invoke();
    }

    internal TextWriter CreateWriter() => new DocumentWriter(this);

    private void Append(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return;
        }

        lock (_sync)
        {
            _text.Append(value);
        }

        Changed?.Invoke();
    }

    private sealed class DocumentWriter(BuildOutputDocument document) : TextWriter
    {
        public override Encoding Encoding => Encoding.Unicode;

        public override void Write(char value) => document.Append(value.ToString());

        public override void Write(string? value) => document.Append(value);

        public override void WriteLine(string? value) => document.Append(value + "\n");

        public override Task WriteLineAsync(string? value)
        {
            WriteLine(value);
            return Task.CompletedTask;
        }
    }
}
