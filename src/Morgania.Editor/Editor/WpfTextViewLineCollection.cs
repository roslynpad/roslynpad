#nullable enable

namespace Microsoft.VisualStudio.Text.Editor.Implementation;

using System.Collections;
using System.Collections.ObjectModel;

using Avalonia;
using Avalonia.Media;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Text.Formatting.Implementation;

/// <summary>
/// The dense, sorted, read-only collection of lines produced by one layout.
/// </summary>
internal sealed class WpfTextViewLineCollection : IWpfTextViewLineCollection
{
    private readonly WpfTextView _view;
    private readonly List<FormattedLine> _lines;
    private bool _isValid = true;

    public WpfTextViewLineCollection(WpfTextView view, List<FormattedLine> lines)
    {
        if (lines.Count == 0)
        {
            throw new ArgumentException("A layout must produce at least one line.", nameof(lines));
        }

        _view = view;
        _lines = lines;
    }

    internal IReadOnlyList<FormattedLine> Lines => _lines;

    internal void Invalidate() => _isValid = false;

    public bool IsValid => _isValid;

    public SnapshotSpan FormattedSpan => new(_lines[0].Start, _lines[^1].EndIncludingLineBreak);

    public ReadOnlyCollection<IWpfTextViewLine> WpfTextViewLines => new([.. _lines]);

    public IWpfTextViewLine FirstVisibleLine
        => _lines.FirstOrDefault(line => line.VisibilityState is VisibilityState.FullyVisible or VisibilityState.PartiallyVisible) ?? _lines[0];

    public IWpfTextViewLine LastVisibleLine
        => _lines.LastOrDefault(line => line.VisibilityState is VisibilityState.FullyVisible or VisibilityState.PartiallyVisible) ?? _lines[^1];

    ITextViewLine ITextViewLineCollection.FirstVisibleLine => FirstVisibleLine;

    ITextViewLine ITextViewLineCollection.LastVisibleLine => LastVisibleLine;

    public IWpfTextViewLine this[int index] => _lines[index];

    ITextViewLine IList<ITextViewLine>.this[int index]
    {
        get => _lines[index];
        set => throw new NotSupportedException();
    }

    public int Count => _lines.Count;

    public bool IsReadOnly => true;

    public bool ContainsBufferPosition(SnapshotPoint bufferPosition)
        => FindLineIndexContainingBufferPosition(bufferPosition) >= 0;

    public bool IntersectsBufferSpan(SnapshotSpan bufferSpan)
        => _lines.Any(line => line.IntersectsBufferSpan(bufferSpan));

    public IWpfTextViewLine GetTextViewLineContainingBufferPosition(SnapshotPoint bufferPosition)
    {
        int index = FindLineIndexContainingBufferPosition(bufferPosition);
        return index >= 0 ? _lines[index] : null!;
    }

    ITextViewLine ITextViewLineCollection.GetTextViewLineContainingBufferPosition(SnapshotPoint bufferPosition)
        => GetTextViewLineContainingBufferPosition(bufferPosition);

    public ITextViewLine GetTextViewLineContainingYCoordinate(double y)
    {
        if (double.IsNaN(y))
        {
            throw new ArgumentOutOfRangeException(nameof(y));
        }

        return _lines.FirstOrDefault(line => y >= line.Top && y < line.Bottom)!;
    }

    public Collection<ITextViewLine> GetTextViewLinesIntersectingSpan(SnapshotSpan bufferSpan)
    {
        var result = new Collection<ITextViewLine>();
        foreach (var line in _lines)
        {
            if (line.IntersectsBufferSpan(bufferSpan))
            {
                result.Add(line);
            }
        }

        return result;
    }

    public SnapshotSpan GetTextElementSpan(SnapshotPoint bufferPosition)
        => GetExistingLine(bufferPosition).GetTextElementSpan(bufferPosition);

    public TextBounds GetCharacterBounds(SnapshotPoint bufferPosition)
        => GetExistingLine(bufferPosition).GetCharacterBounds(bufferPosition);

    public Collection<TextBounds> GetNormalizedTextBounds(SnapshotSpan bufferSpan)
    {
        var result = new Collection<TextBounds>();
        foreach (var line in _lines)
        {
            foreach (var bounds in line.GetNormalizedTextBounds(bufferSpan))
            {
                result.Add(bounds);
            }
        }

        return result;
    }

    public int GetIndexOfTextLine(ITextViewLine textLine)
    {
        ArgumentNullException.ThrowIfNull(textLine);
        return _lines.FindIndex(line => ReferenceEquals(line, textLine));
    }

    #region Marker geometry

    public Geometry? GetTextMarkerGeometry(SnapshotSpan bufferSpan)
        => GetTextMarkerGeometry(bufferSpan, false, default);

    public Geometry? GetTextMarkerGeometry(SnapshotSpan bufferSpan, bool clipToViewport, Thickness padding)
        => BuildGeometry(GetNormalizedTextBounds(bufferSpan), clipToViewport, padding);

    public Geometry? GetLineMarkerGeometry(SnapshotSpan bufferSpan)
        => GetLineMarkerGeometry(bufferSpan, false, default);

    public Geometry? GetLineMarkerGeometry(SnapshotSpan bufferSpan, bool clipToViewport, Thickness padding)
    {
        var boundsList = new Collection<TextBounds>();
        foreach (var line in _lines)
        {
            if (line.IntersectsBufferSpan(bufferSpan))
            {
                boundsList.Add(new TextBounds(line.Left, line.Top, line.Width, line.Height, line.TextTop, line.TextHeight));
            }
        }

        return BuildGeometry(boundsList, clipToViewport, padding);
    }

    public Geometry? GetMarkerGeometry(SnapshotSpan bufferSpan)
        => GetMarkerGeometry(bufferSpan, false, default);

    public Geometry? GetMarkerGeometry(SnapshotSpan bufferSpan, bool clipToViewport, Thickness padding)
    {
        var spanningLines = GetTextViewLinesIntersectingSpan(bufferSpan);
        return spanningLines.Count <= 1
            ? GetTextMarkerGeometry(bufferSpan, clipToViewport, padding)
            : GetLineMarkerGeometry(bufferSpan, clipToViewport, padding);
    }

    private GeometryGroup? BuildGeometry(IEnumerable<TextBounds> boundsList, bool clipToViewport, Thickness padding)
    {
        GeometryGroup? group = null;
        foreach (var bounds in boundsList)
        {
            var rect = new Rect(bounds.Left, bounds.Top, bounds.Width, bounds.Height).Inflate(padding);
            if (clipToViewport)
            {
                var viewport = new Rect(_view.ViewportLeft, _view.ViewportTop, _view.ViewportWidth, _view.ViewportHeight);
                rect = rect.Intersect(viewport);
            }

            if (rect.Width > 0 || rect.Height > 0)
            {
                group ??= new GeometryGroup { FillRule = FillRule.NonZero };
                group.Children.Add(new RectangleGeometry(rect));
            }
        }

        return group;
    }

    #endregion

    #region IList<ITextViewLine> (read-only)

    public int IndexOf(ITextViewLine item) => GetIndexOfTextLine(item);

    public bool Contains(ITextViewLine item) => item is not null && GetIndexOfTextLine(item) >= 0;

    public void CopyTo(ITextViewLine[] array, int arrayIndex)
    {
        ArgumentNullException.ThrowIfNull(array);
        foreach (var line in _lines)
        {
            array[arrayIndex++] = line;
        }
    }

    public IEnumerator<ITextViewLine> GetEnumerator() => _lines.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void Insert(int index, ITextViewLine item) => throw new NotSupportedException();

    public void RemoveAt(int index) => throw new NotSupportedException();

    public void Add(ITextViewLine item) => throw new NotSupportedException();

    public void Clear() => throw new NotSupportedException();

    public bool Remove(ITextViewLine item) => throw new NotSupportedException();

    #endregion

    private FormattedLine GetExistingLine(SnapshotPoint bufferPosition)
    {
        int index = FindLineIndexContainingBufferPosition(bufferPosition);
        return index >= 0
            ? _lines[index]
            : throw new ArgumentOutOfRangeException(nameof(bufferPosition));
    }

    private int FindLineIndexContainingBufferPosition(SnapshotPoint bufferPosition)
    {
        if (bufferPosition.Snapshot != _lines[0].Snapshot)
        {
            throw new ArgumentException("The position belongs to a different snapshot.");
        }

        for (int i = 0; i < _lines.Count; i++)
        {
            if (_lines[i].ContainsBufferPosition(bufferPosition))
            {
                return i;
            }
        }

        return -1;
    }
}
