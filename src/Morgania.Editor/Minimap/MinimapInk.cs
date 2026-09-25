#nullable enable

namespace Microsoft.VisualStudio.Text.Editor.Implementation;

using System.Text;
using System.Text.RegularExpressions;

using Avalonia.Media;
using Avalonia.Threading;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;

/// <summary>A run of columns <c>[Start, End)</c> drawn in one colour.</summary>
internal readonly record struct InkRun(int Start, int End, Color Color);

/// <summary>A stretch <c>[Start, End)</c> of a line's characters and the brush its classification gives it; null for the text colour.</summary>
internal readonly record struct ColoredSpan(int Start, int End, IBrush? Brush);

/// <summary>
/// What the minimap draws for one line: its characters with tabs expanded to columns (whitespace as blanks), the runs of
/// inked columns, and the label of the marker the line carries.
/// </summary>
internal sealed class LineInk(string columns, InkRun[] runs, string? markerLabel)
{
    public string Columns { get; } = columns;

    public InkRun[] Runs { get; } = runs;

    public string? MarkerLabel { get; } = markerLabel;
}

/// <summary>
/// The ink of the visual snapshot's lines, built on demand from the view's classification and kept per line: an edit
/// drops only the lines it touched, a classification change the lines it names, a new format map all of them.
/// </summary>
internal sealed class MinimapInkSource : IDisposable
{
    /// <summary>The most columns a line is inked to; the minimap is never wider than this many at its smallest scale.</summary>
    public const int MaxColumns = 800;

    /// <summary>The most characters of a line that are read, whatever their tabs.</summary>
    private const int MaxCharacters = 2000;

    private static readonly Color s_fallbackText = Color.FromRgb(0x9C, 0x9C, 0x9C);

    private static readonly string[] s_commentClosings = ["*/", "-->", "*)"];

    private readonly IWpfTextView _view;
    private readonly IClassifier _classifier;
    private readonly IClassificationFormatMap _formatMap;
    private readonly ITextBuffer _visualBuffer;
    private readonly List<LineInk?> _lines = [];
    private ITextSnapshot? _snapshot;
    private int _tabSize = 4;
    private bool _syntaxHighlight = true;
    private Regex? _markers;
    private string? _markersPattern;
    private bool _isDisposed;

    public MinimapInkSource(IWpfTextView view, IClassifier classifier, IClassificationFormatMap formatMap)
    {
        _view = view;
        _classifier = classifier;
        _formatMap = formatMap;
        _visualBuffer = view.TextViewModel.VisualBuffer;
        _visualBuffer.Changed += OnVisualBufferChanged;
        _classifier.ClassificationChanged += OnClassificationChanged;
        _formatMap.ClassificationFormatMappingChanged += OnFormatMappingChanged;
    }

    /// <summary>Raised when the ink of some lines was dropped and the picture should be drawn again.</summary>
    public event EventHandler? Invalidated;

    public int TabSize => _tabSize;

    /// <summary>The colour of unclassified text.</summary>
    public Color TextColor => ColorOf(_formatMap.DefaultTextProperties.ForegroundBrush) ?? s_fallbackText;

    /// <summary>Takes the options the ink depends on; a change drops every line.</summary>
    public void Configure(int tabSize, bool syntaxHighlight, bool showMarkers, string? markersPattern)
    {
        tabSize = Math.Max(1, tabSize);
        string? pattern = showMarkers && !string.IsNullOrWhiteSpace(markersPattern) ? markersPattern : null;
        if (tabSize == _tabSize && syntaxHighlight == _syntaxHighlight && string.Equals(pattern, _markersPattern, StringComparison.Ordinal))
        {
            return;
        }

        _tabSize = tabSize;
        _syntaxHighlight = syntaxHighlight;
        _markersPattern = pattern;
        _markers = null;
        if (pattern is not null)
        {
            try
            {
                _markers = new Regex(pattern, RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(50));
            }
            catch (ArgumentException)
            {
                // A pattern that does not parse marks nothing.
            }
        }

        Clear();
    }

    /// <summary>
    /// The ink of <paramref name="lineNumber"/> in <paramref name="snapshot"/>, built now when <paramref name="build"/> is
    /// set; null when it is not built yet and may not be.
    /// </summary>
    public LineInk? Get(ITextSnapshot snapshot, int lineNumber, bool build)
    {
        Align(snapshot);
        var ink = _lines[lineNumber];
        if (ink is null && build)
        {
            ink = Build(snapshot.GetLineFromLineNumber(lineNumber));
            _lines[lineNumber] = ink;
        }

        return ink;
    }

    /// <summary>
    /// The classification of a visual line by character: the view's classifier over the edit-buffer text the line shows,
    /// the stretches between classified spans in the text colour (null brush). All text colour when syntax colouring is
    /// off.
    /// </summary>
    public List<ColoredSpan> Classify(ITextSnapshotLine visualLine, int length)
    {
        var spans = new List<ColoredSpan>();
        if (length <= 0)
        {
            return spans;
        }

        int current = 0;
        if (_syntaxHighlight)
        {
            // Under elision a visual line shows several edit-buffer segments side by side; each is classified and its
            // spans land at the segment's offset in the line, as the formatter lays them out.
            if (ReferenceEquals(visualLine.Snapshot.TextBuffer, _view.TextBuffer))
            {
                AddClassified(spans, new SnapshotSpan(visualLine.Start, length), visualLine.Start.Position, length, ref current);
            }
            else
            {
                int visualOffset = 0;
                foreach (var editSpan in _view.BufferGraph.MapDownToBuffer(visualLine.Extent, SpanTrackingMode.EdgeExclusive, _view.TextBuffer))
                {
                    if (visualOffset >= length)
                    {
                        break;
                    }

                    AddClassified(spans, editSpan, editSpan.Start.Position - visualOffset, Math.Min(length, visualOffset + editSpan.Length), ref current);
                    visualOffset += editSpan.Length;
                }
            }
        }

        if (current < length)
        {
            spans.Add(new ColoredSpan(current, length, null));
        }

        return spans;
    }

    /// <summary>The column <paramref name="index"/> of <paramref name="text"/> starts at, tabs expanded.</summary>
    public int ColumnOf(string text, int index)
    {
        int column = 0;
        for (int i = 0; i < Math.Min(index, text.Length); i++)
        {
            column += text[i] == '\t' ? _tabSize - (column % _tabSize) : 1;
        }

        return column;
    }

    /// <summary>The index of the character of <paramref name="text"/> at <paramref name="column"/>, tabs expanded; the length past its end.</summary>
    public int IndexOfColumn(string text, int column)
    {
        int current = 0;
        for (int i = 0; i < text.Length; i++)
        {
            int width = text[i] == '\t' ? _tabSize - (current % _tabSize) : 1;
            if (column < current + width)
            {
                return i;
            }

            current += width;
        }

        return text.Length;
    }

    public static Color? ColorOf(IBrush? brush)
        => brush is ISolidColorBrush solid
            ? Color.FromArgb((byte)Math.Round(solid.Color.A * Math.Clamp(solid.Opacity, 0.0, 1.0)), solid.Color.R, solid.Color.G, solid.Color.B)
            : null;

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        _visualBuffer.Changed -= OnVisualBufferChanged;
        _classifier.ClassificationChanged -= OnClassificationChanged;
        _formatMap.ClassificationFormatMappingChanged -= OnFormatMappingChanged;
        (_classifier as IDisposable)?.Dispose();
    }

    private LineInk Build(ITextSnapshotLine line)
    {
        string text = line.Length > MaxCharacters ? line.Snapshot.GetText(line.Start, MaxCharacters) : line.GetText();
        var spans = Classify(line, text.Length);
        var textColor = TextColor;
        var columns = new StringBuilder(Math.Min(text.Length, MaxColumns));
        var runs = new List<InkRun>();
        int column = 0;
        int spanIndex = 0;
        for (int i = 0; i < text.Length && column < MaxColumns; i++)
        {
            char c = text[i];
            int width = c == '\t' ? _tabSize - (column % _tabSize) : 1;
            width = Math.Min(width, MaxColumns - column);
            if (char.IsWhiteSpace(c) || char.IsControl(c))
            {
                columns.Append(' ', width);
                column += width;
                continue;
            }

            while (spanIndex < spans.Count && spans[spanIndex].End <= i)
            {
                spanIndex++;
            }

            var color = spanIndex < spans.Count && spans[spanIndex].Start <= i
                ? ColorOf(spans[spanIndex].Brush) ?? textColor
                : textColor;
            columns.Append(c);
            if (runs.Count > 0 && runs[^1].End == column && runs[^1].Color == color)
            {
                runs[^1] = runs[^1] with { End = column + 1 };
            }
            else
            {
                runs.Add(new InkRun(column, column + 1, color));
            }

            column += width;
        }

        return new LineInk(columns.ToString(), [.. runs], MarkerLabel(text));
    }

    private string? MarkerLabel(string text)
    {
        if (_markers is null || text.Length == 0)
        {
            return null;
        }

        try
        {
            var match = _markers.Match(text);
            if (!match.Success)
            {
                return null;
            }

            // The label is what follows the match, without the closing of a block comment the marker may sit in.
            string label = text[(match.Index + match.Length)..].Trim();
            foreach (string closing in s_commentClosings)
            {
                if (label.EndsWith(closing, StringComparison.Ordinal))
                {
                    label = label[..^closing.Length].TrimEnd();
                }
            }

            return label.Length > 0 ? label : null;
        }
        catch (RegexMatchTimeoutException)
        {
            return null;
        }
    }

    private void AddClassified(List<ColoredSpan> spans, SnapshotSpan span, int delta, int limit, ref int current)
    {
        foreach (var classification in _classifier.GetClassificationSpans(span))
        {
            int start = Math.Max(0, classification.Span.Start.Position - delta);
            int end = Math.Min(limit, classification.Span.End.Position - delta);
            if (end <= current)
            {
                continue;
            }

            if (start > current)
            {
                spans.Add(new ColoredSpan(current, start, null));
            }

            spans.Add(new ColoredSpan(Math.Max(start, current), end, _formatMap.GetTextProperties(classification.ClassificationType).ForegroundBrush));
            current = end;
        }
    }

    /// <summary>
    /// Brings the lines to <paramref name="snapshot"/>: one edit on from the snapshot they belong to, the lines the edit
    /// did not touch keep their ink; otherwise all is dropped. The view lays out inside the buffer's change event, so a
    /// pass can ask for the new snapshot before <see cref="OnVisualBufferChanged"/> has seen the edit.
    /// </summary>
    private void Align(ITextSnapshot snapshot)
    {
        if (ReferenceEquals(snapshot, _snapshot) && _lines.Count == snapshot.LineCount)
        {
            return;
        }

        if (_snapshot is { } before
            && ReferenceEquals(before.Version.Next, snapshot.Version)
            && _lines.Count == before.LineCount
            && before.Version.Changes is { Count: > 0 } changes)
        {
            // Lines before the first change keep their number, lines after the last one shift as a block: only the lines
            // in between are dropped (the outermost changes bound them, whatever lies between the changes).
            int firstOld = before.GetLineNumberFromPosition(changes[0].OldPosition);
            int lastOld = before.GetLineNumberFromPosition(changes[^1].OldEnd);
            int lastNew = snapshot.GetLineNumberFromPosition(changes[^1].NewEnd);
            _lines.RemoveRange(firstOld, lastOld - firstOld + 1);
            _lines.InsertRange(firstOld, Enumerable.Repeat<LineInk?>(null, lastNew - firstOld + 1));
        }
        else
        {
            _lines.Clear();
            _lines.AddRange(Enumerable.Repeat<LineInk?>(null, snapshot.LineCount));
        }

        _snapshot = snapshot;
    }

    private void Clear()
    {
        for (int i = 0; i < _lines.Count; i++)
        {
            _lines[i] = null;
        }
    }

    private void OnVisualBufferChanged(object? sender, TextContentChangedEventArgs e)
    {
        if (_snapshot is not null)
        {
            Align(e.After);
        }

        Invalidated?.Invoke(this, EventArgs.Empty);
    }

    private void OnClassificationChanged(object? sender, ClassificationChangedEventArgs e)
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => OnClassificationChanged(sender, e));
            return;
        }

        if (_isDisposed || _view.IsClosed)
        {
            return;
        }

        var visual = _visualBuffer.CurrentSnapshot;
        if (!ReferenceEquals(visual, _snapshot) || !ReferenceEquals(e.ChangeSpan.Snapshot.TextBuffer, _view.TextBuffer))
        {
            _snapshot = null;
        }
        else
        {
            var span = e.ChangeSpan.TranslateTo(_view.TextBuffer.CurrentSnapshot, SpanTrackingMode.EdgeInclusive);
            foreach (var visualSpan in _view.BufferGraph.MapUpToSnapshot(span, SpanTrackingMode.EdgeInclusive, visual))
            {
                int first = visual.GetLineNumberFromPosition(visualSpan.Start);
                int last = visual.GetLineNumberFromPosition(visualSpan.End);
                for (int line = first; line <= last && line < _lines.Count; line++)
                {
                    _lines[line] = null;
                }
            }
        }

        Invalidated?.Invoke(this, EventArgs.Empty);
    }

    private void OnFormatMappingChanged(object? sender, EventArgs e)
    {
        Clear();
        Invalidated?.Invoke(this, EventArgs.Empty);
    }
}
