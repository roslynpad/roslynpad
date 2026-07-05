#nullable enable

namespace Microsoft.VisualStudio.Text.Formatting.Implementation;

using System.Collections.ObjectModel;

using Avalonia;
using Avalonia.Media.TextFormatting;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Projection;

using TextBounds = Microsoft.VisualStudio.Text.Formatting.TextBounds;

/// <summary>
/// An <see cref="IFormattedLine"/> over an Avalonia <see cref="TextLine"/>. One instance
/// corresponds to one visual row: a whole snapshot line, or a segment of it when word wrap
/// is enabled. All geometry is a pure function of the formatted text and the position
/// assigned by the layout engine (SetTop/SetLineTransform); the line holds no mutable
/// document state; the view renders snapshots.
/// </summary>
internal sealed class FormattedLine : IFormattedLine
{
    /// <summary>An adornment's replaced span, line-relative to the paragraph start.</summary>
    internal readonly record struct AdornmentInfo(int Start, int End, IAdornmentElement Element);

    /// <summary>A run of visible row text and the edit-buffer span it maps down to.
    /// Row offsets are contiguous (every visible character comes from the source);
    /// edit positions may have gaps where text is elided.</summary>
    private record struct MappedSegment(int RowOffset, int EditStart, int Length);

    private readonly TextLine _textLine;
    private readonly IReadOnlyList<ClassifiedTextSource.FormattingRun> _runs;
    private readonly IReadOnlyList<AdornmentInfo> _adornments;
    private readonly IBufferGraph? _bufferGraph;
    private readonly int _rowStart;
    private readonly int _rowLength;
    private readonly int _lineBreakLength;
    private readonly bool _isFirstRow;
    private readonly bool _isLastRow;
    private readonly bool _endsAtEndOfBuffer;
    private readonly double _columnWidth;
    private readonly double _defaultBaseline;
    private readonly double _defaultTextHeight;

    private ITextSnapshot _snapshot;
    private ITextSnapshot _visualSnapshot;
    private int _paragraphStart;
    private MappedSegment[]? _segments;
    private int _editStart;
    private int _editEnd;
    private int _editBreakStart;
    private int _editEndIncludingLineBreak;
    private double _left;
    private double _top;
    private double _deltaY;
    private TextViewLineChange _change = TextViewLineChange.NewOrReformatted;
    private LineTransform _lineTransform = new(0.0, 0.0, 1.0);
    private Rect _visibleArea;
    private bool _hasVisibleArea;
    private LineVisual? _visual;
    private bool _isDisposed;

    public FormattedLine(
        TextLine textLine,
        IReadOnlyList<ClassifiedTextSource.FormattingRun> runs,
        IReadOnlyList<AdornmentInfo> adornments,
        ITextSnapshot snapshot,
        ITextSnapshot visualSnapshot,
        IBufferGraph? bufferGraph,
        int paragraphStart,
        int rowStart,
        int rowLength,
        int lineBreakLength,
        bool isFirstRow,
        bool isLastRow,
        bool endsAtEndOfBuffer,
        double left,
        double columnWidth,
        double defaultBaseline,
        double defaultTextHeight)
    {
        _textLine = textLine;
        _runs = runs;
        _adornments = adornments;
        _snapshot = snapshot;
        _visualSnapshot = visualSnapshot;
        _bufferGraph = bufferGraph;
        _paragraphStart = paragraphStart;
        _rowStart = rowStart;
        _rowLength = rowLength;
        _lineBreakLength = lineBreakLength;
        _isFirstRow = isFirstRow;
        _isLastRow = isLastRow;
        _endsAtEndOfBuffer = endsAtEndOfBuffer;
        _left = left;
        _columnWidth = columnWidth;
        _defaultBaseline = defaultBaseline;
        _defaultTextHeight = defaultTextHeight;

        // Under an elision/projection view model the row's visual text maps down to
        // (possibly disjoint) edit-buffer segments; identity models take the fast path
        // (_segments stays null and every offset is plain arithmetic).
        if (!ReferenceEquals(snapshot, visualSnapshot))
        {
            if (bufferGraph is null)
            {
                throw new ArgumentException("A buffer graph is required when the visual buffer differs from the edit buffer.", nameof(bufferGraph));
            }

            var segments = new List<MappedSegment>();
            int rowOffset = 0;
            if (rowLength > 0)
            {
                foreach (var span in bufferGraph.MapDownToSnapshot(
                    new SnapshotSpan(visualSnapshot, paragraphStart + rowStart, rowLength),
                    SpanTrackingMode.EdgeExclusive,
                    snapshot))
                {
                    segments.Add(new MappedSegment(rowOffset, span.Start.Position, span.Length));
                    rowOffset += span.Length;
                }
            }

            _editStart = segments.Count > 0
                ? segments[0].EditStart
                : bufferGraph.MapDownToSnapshot(
                    new SnapshotPoint(visualSnapshot, paragraphStart + rowStart),
                    PointTrackingMode.Negative,
                    snapshot,
                    PositionAffinity.Successor)?.Position ?? 0;
            _editEnd = segments.Count > 0 ? segments[^1].EditStart + segments[^1].Length : _editStart;
            _editBreakStart = _editEnd;
            _editEndIncludingLineBreak = _editEnd;
            if (lineBreakLength > 0)
            {
                var breakSpans = bufferGraph.MapDownToSnapshot(
                    new SnapshotSpan(visualSnapshot, paragraphStart + rowStart + rowLength, lineBreakLength),
                    SpanTrackingMode.EdgeExclusive,
                    snapshot);
                if (breakSpans.Count > 0)
                {
                    // Elided text may sit between the last visible character and the
                    // line break; the break's own edit start marks that boundary.
                    _editBreakStart = breakSpans[0].Start.Position;
                    _editEndIncludingLineBreak = breakSpans[^1].End.Position;
                }
            }

            _segments = [.. segments];
        }
    }

    #region Extent and positions

    public ITextSnapshot Snapshot => _snapshot;

    public SnapshotPoint Start => new(_snapshot, _segments is null ? _paragraphStart + _rowStart : _editStart);

    // Extent lengths are in edit-buffer characters: a row rendering a collapsed region
    // covers the hidden text, so its extent is longer than its visible text.
    public int Length => End.Position - Start.Position;

    public int LengthIncludingLineBreak => EndIncludingLineBreak.Position - Start.Position;

    public SnapshotPoint End => _segments is null ? Start + _rowLength : new(_snapshot, _editEnd);

    public SnapshotPoint EndIncludingLineBreak => _segments is null
        ? Start + _rowLength + _lineBreakLength
        : new(_snapshot, _editEndIncludingLineBreak);

    public int LineBreakLength => _lineBreakLength;

    public SnapshotSpan Extent => new(Start, End);

    public SnapshotSpan ExtentIncludingLineBreak => new(Start, EndIncludingLineBreak);

    public IMappingSpan ExtentAsMappingSpan => CreateMappingSpan(Extent);

    public IMappingSpan ExtentIncludingLineBreakAsMappingSpan => CreateMappingSpan(ExtentIncludingLineBreak);

    public bool IsFirstTextViewLineForSnapshotLine => _isFirstRow;

    public bool IsLastTextViewLineForSnapshotLine => _isLastRow;

    private IMappingSpan CreateMappingSpan(SnapshotSpan span)
    {
        return _bufferGraph is null
            ? throw new InvalidOperationException("This formatted line was created without a buffer graph.")
            : _bufferGraph.CreateMappingSpan(span, SpanTrackingMode.EdgeInclusive);
    }

    #endregion

    #region Vertical geometry

    public double Top => ThrowIfDisposed(_top);

    public double Height => _lineTransform.TopSpace + (TextHeight * _lineTransform.VerticalScale) + _lineTransform.BottomSpace;

    public double Bottom => Top + Height;

    public double TextTop => Top + _lineTransform.TopSpace;

    public double TextBottom => TextTop + (TextHeight * _lineTransform.VerticalScale);

    public double TextHeight => _rowLength == 0 ? _defaultTextHeight : _textLine.Height;

    public double Baseline => (_rowLength == 0 ? _defaultBaseline : _textLine.Baseline) * _lineTransform.VerticalScale;

    #endregion

    #region Horizontal geometry

    public double Left => ThrowIfDisposed(_left);

    public double TextLeft => Left;

    public double TextWidth => _rowLength == 0 ? 0.0 : _textLine.WidthIncludingTrailingWhitespace;

    public double TextRight => TextLeft + TextWidth;

    public double EndOfLineWidth => (_lineBreakLength > 0 || (_isLastRow && _endsAtEndOfBuffer)) ? _columnWidth : 0.0;

    public double Width => TextWidth + EndOfLineWidth;

    public double Right => Left + Width;

    public double VirtualSpaceWidth => _columnWidth;

    #endregion

    #region Layout state

    public object IdentityTag => this;

    public bool IsValid => !_isDisposed;

    public LineTransform LineTransform => _lineTransform;

    public LineTransform DefaultLineTransform
    {
        get
        {
            double topSpace = 0.0;
            double bottomSpace = 0.0;
            foreach (var adornment in RowAdornments())
            {
                topSpace = Math.Max(topSpace, adornment.Element.TopSpace);
                bottomSpace = Math.Max(bottomSpace, adornment.Element.BottomSpace);
            }

            return new LineTransform(topSpace, bottomSpace, 1.0);
        }
    }

    public double DeltaY => _deltaY;

    public TextViewLineChange Change => _change;

    public Rect VisibleArea => _visibleArea;

    public VisibilityState VisibilityState
    {
        get
        {
            ThrowIfDisposed(0.0);
            if (!_hasVisibleArea)
            {
                return VisibilityState.Unattached;
            }

            if (Bottom <= _visibleArea.Y || Top >= _visibleArea.Bottom)
            {
                return VisibilityState.Hidden;
            }

            return (Top >= _visibleArea.Y && Bottom <= _visibleArea.Bottom)
                ? VisibilityState.FullyVisible
                : VisibilityState.PartiallyVisible;
        }
    }

    public void SetTop(double top)
    {
        ThrowIfDisposed(0.0);
        if (top != _top)
        {
            _top = top;
            _visual?.InvalidateVisual();
        }
    }

    public void SetDeltaY(double deltaY) => _deltaY = deltaY;

    public void SetChange(TextViewLineChange change) => _change = change;

    public void SetLineTransform(LineTransform transform)
    {
        ThrowIfDisposed(0.0);
        _lineTransform = transform;
        _visual?.InvalidateVisual();
    }

    public void SetVisibleArea(Rect visibleArea)
    {
        ThrowIfDisposed(0.0);
        _visibleArea = visibleArea;
        _hasVisibleArea = true;
    }

    public void SetSnapshot(ITextSnapshot visualSnapshot, ITextSnapshot editSnapshot)
    {
        ThrowIfDisposed(0.0);
        ArgumentNullException.ThrowIfNull(visualSnapshot);
        ArgumentNullException.ThrowIfNull(editSnapshot);

        // The paragraph start lives in visual coordinates; the mapped segments (when the
        // visual buffer differs) live in edit coordinates. Each translates in its own space.
        int newStart = new SnapshotPoint(_visualSnapshot, _paragraphStart).TranslateTo(visualSnapshot, PointTrackingMode.Negative).Position;
        if (_segments is { } segments)
        {
            for (int i = 0; i < segments.Length; i++)
            {
                segments[i] = segments[i] with
                {
                    EditStart = new SnapshotPoint(_snapshot, segments[i].EditStart).TranslateTo(editSnapshot, PointTrackingMode.Negative).Position,
                };
            }

            _editStart = new SnapshotPoint(_snapshot, _editStart).TranslateTo(editSnapshot, PointTrackingMode.Negative).Position;
            _editEnd = new SnapshotPoint(_snapshot, _editEnd).TranslateTo(editSnapshot, PointTrackingMode.Positive).Position;
            _editBreakStart = new SnapshotPoint(_snapshot, _editBreakStart).TranslateTo(editSnapshot, PointTrackingMode.Positive).Position;
            _editEndIncludingLineBreak = new SnapshotPoint(_snapshot, _editEndIncludingLineBreak).TranslateTo(editSnapshot, PointTrackingMode.Positive).Position;
        }

        _snapshot = editSnapshot;
        _visualSnapshot = visualSnapshot;
        _paragraphStart = newStart;
    }

    internal ITextSnapshot VisualSnapshot => _visualSnapshot;

    /// <summary>The snapshot line start this row was formatted from (the view's line
    /// cache groups reusable rows by it).</summary>
    internal int ParagraphStart => _paragraphStart;

    /// <summary>Number of the visual-buffer line this row renders (layout iterates the
    /// visual buffer; under elision it differs from the edit buffer's numbering).</summary>
    internal int VisualLineNumber => _visualSnapshot.GetLineNumberFromPosition(_paragraphStart);

    /// <summary>True when this row's visual line is the last line of the visual buffer.</summary>
    internal bool EndsAtEndOfVisualBuffer => _endsAtEndOfBuffer;

    /// <summary>Visual-snapshot position of the row's first character (layout walks the
    /// visual buffer).</summary>
    internal int VisualRowStart => _paragraphStart + _rowStart;

    /// <summary>Visual-snapshot position just past the row's text and line break.</summary>
    internal int VisualEndIncludingLineBreak => _paragraphStart + _rowStart + _rowLength + _lineBreakLength;

    internal void SetLeft(double left)
    {
        if (left != _left)
        {
            _left = left;
            _visual?.InvalidateVisual();
        }
    }

    #endregion

    #region Containment

    public bool ContainsBufferPosition(SnapshotPoint bufferPosition)
    {
        ValidateSnapshot(bufferPosition.Snapshot);
        return (bufferPosition >= Start)
            && ((bufferPosition < EndIncludingLineBreak)
                || (_isLastRow && _endsAtEndOfBuffer && bufferPosition == EndIncludingLineBreak));
    }

    public bool IntersectsBufferSpan(SnapshotSpan bufferSpan)
    {
        ValidateSnapshot(bufferSpan.Snapshot);
        return ExtentIncludingLineBreak.IntersectsWith(bufferSpan)
            || (_isLastRow && _endsAtEndOfBuffer && bufferSpan.Start == EndIncludingLineBreak);
    }

    #endregion

    #region Geometry queries

    public TextBounds GetCharacterBounds(SnapshotPoint bufferPosition)
    {
        ValidatePosition(bufferPosition);
        int index = GetRowOffset(bufferPosition.Position);
        if (index >= _rowLength)
        {
            // The line break (or end-of-buffer) box.
            return new TextBounds(TextRight, Top, EndOfLineWidth, Height, TextTop, TextHeight * _lineTransform.VerticalScale);
        }

        return GetElementBounds(_rowStart + index, GetTextElementLength(index));
    }

    public TextBounds GetCharacterBounds(VirtualSnapshotPoint bufferPosition)
    {
        if (bufferPosition.VirtualSpaces > 0 && ContainsVirtualSpace(bufferPosition))
        {
            double leading = TextRight + EndOfLineWidth + ((bufferPosition.VirtualSpaces - 1) * VirtualSpaceWidth);
            return new TextBounds(leading, Top, VirtualSpaceWidth, Height, TextTop, TextHeight * _lineTransform.VerticalScale);
        }

        return GetCharacterBounds(bufferPosition.Position);
    }

    public TextBounds GetExtendedCharacterBounds(SnapshotPoint bufferPosition)
    {
        // A position inside an adornment's replaced span answers the adornment's bounds.
        int index = _rowStart + GetRowOffset(bufferPosition.Position);
        foreach (var adornment in RowAdornments())
        {
            if (index >= adornment.Start && index < adornment.End)
            {
                return GetElementBounds(adornment.Start, Math.Max(1, adornment.End - adornment.Start));
            }
        }

        return GetCharacterBounds(bufferPosition);
    }

    public TextBounds GetExtendedCharacterBounds(VirtualSnapshotPoint bufferPosition)
        => bufferPosition.VirtualSpaces > 0 ? GetCharacterBounds(bufferPosition) : GetExtendedCharacterBounds(bufferPosition.Position);

    public TextBounds? GetAdornmentBounds(object identityTag)
    {
        ArgumentNullException.ThrowIfNull(identityTag);
        foreach (var adornment in RowAdornments())
        {
            if (Equals(adornment.Element.IdentityTag, identityTag))
            {
                return GetElementBounds(adornment.Start, Math.Max(1, adornment.End - adornment.Start));
            }
        }

        return null;
    }

    public ReadOnlyCollection<object> GetAdornmentTags(object providerTag)
    {
        ArgumentNullException.ThrowIfNull(providerTag);
        var tags = new List<object>();
        foreach (var adornment in RowAdornments())
        {
            if (Equals(adornment.Element.ProviderTag, providerTag))
            {
                tags.Add(adornment.Element.IdentityTag);
            }
        }

        return new ReadOnlyCollection<object>(tags);
    }

    private IEnumerable<AdornmentInfo> RowAdornments()
    {
        foreach (var adornment in _adornments)
        {
            if (adornment.Start >= _rowStart && adornment.Start < _rowStart + Math.Max(1, _rowLength))
            {
                yield return adornment;
            }
        }
    }

    public Collection<TextBounds> GetNormalizedTextBounds(SnapshotSpan bufferSpan)
    {
        ValidateSnapshot(bufferSpan.Snapshot);
        var result = new Collection<TextBounds>();
        var overlap = ExtentIncludingLineBreak.Overlap(bufferSpan);
        if (overlap is not { } span)
        {
            return result;
        }

        int startIndex = GetRowOffset(span.Start.Position);
        int endIndex = Math.Min(GetRowOffset(span.End.Position), _rowLength);
        if (endIndex > startIndex)
        {
            foreach (var bounds in _textLine.GetTextBounds(_rowStart + startIndex, endIndex - startIndex))
            {
                var rect = bounds.Rectangle;
                bool rightToLeft = bounds.FlowDirection == Avalonia.Media.FlowDirection.RightToLeft;
                double leading = Left + (rightToLeft ? rect.Right : rect.Left);
                double width = rightToLeft ? -rect.Width : rect.Width;
                result.Add(new TextBounds(leading, Top, width, Height, TextTop, TextHeight * _lineTransform.VerticalScale));
            }
        }

        if (span.End > End || (span.End == End && _isLastRow && _endsAtEndOfBuffer && span.End == span.Start))
        {
            // The span extends into (or an empty span sits at) the end-of-line region.
            result.Add(new TextBounds(TextRight, Top, EndOfLineWidth, Height, TextTop, TextHeight * _lineTransform.VerticalScale));
        }

        return result;
    }

    public SnapshotPoint? GetBufferPositionFromXCoordinate(double xCoordinate) => GetBufferPositionFromXCoordinate(xCoordinate, false);

    public SnapshotPoint? GetBufferPositionFromXCoordinate(double xCoordinate, bool textOnly)
    {
        ThrowIfDisposed(0.0);
        if (double.IsNaN(xCoordinate))
        {
            throw new ArgumentOutOfRangeException(nameof(xCoordinate));
        }

        if (xCoordinate < Left || xCoordinate >= Right)
        {
            return null;
        }

        if (xCoordinate >= TextRight)
        {
            // Per the contract, textOnly means "only when actually over a character":
            // the end-of-line/line-break region does not count.
            return textOnly ? null : End;
        }

        var hit = _textLine.GetCharacterHitFromDistance(xCoordinate - Left);
        int index = Math.Clamp(hit.FirstCharacterIndex - _rowStart, 0, Math.Max(0, _rowLength - 1));
        return new SnapshotPoint(_snapshot, GetEditPosition(index));
    }

    public VirtualSnapshotPoint GetVirtualBufferPositionFromXCoordinate(double xCoordinate)
    {
        if (GetBufferPositionFromXCoordinate(xCoordinate) is { } position)
        {
            return new VirtualSnapshotPoint(position);
        }

        if (xCoordinate <= Left)
        {
            return new VirtualSnapshotPoint(Start);
        }

        // To the right of the end of the line: virtual space (only meaningful at a real line end).
        int spaces = (_lineBreakLength == 0 && !_endsAtEndOfBuffer)
            ? 0
            : Math.Max(0, (int)((xCoordinate - Right) / VirtualSpaceWidth) + 1);
        return new VirtualSnapshotPoint(End, spaces);
    }

    public VirtualSnapshotPoint GetInsertionBufferPositionFromXCoordinate(double xCoordinate)
    {
        ThrowIfDisposed(0.0);
        if (xCoordinate < Left)
        {
            return new VirtualSnapshotPoint(Start);
        }

        if (xCoordinate < TextRight)
        {
            var hit = _textLine.GetCharacterHitFromDistance(xCoordinate - Left);
            int index = Math.Clamp(hit.FirstCharacterIndex + hit.TrailingLength - _rowStart, 0, _rowLength);
            return new VirtualSnapshotPoint(new SnapshotPoint(_snapshot, GetEditPosition(index)));
        }

        return GetVirtualBufferPositionFromXCoordinate(xCoordinate);
    }

    public SnapshotSpan GetTextElementSpan(SnapshotPoint bufferPosition)
    {
        ValidatePosition(bufferPosition);
        if (bufferPosition >= End)
        {
            // Elided text between the last visible character and the line break is its
            // own element, distinct from the break.
            if (_segments is not null && bufferPosition.Position < _editBreakStart)
            {
                return new SnapshotSpan(_snapshot, _editEnd, _editBreakStart - _editEnd);
            }

            return _segments is null
                ? new SnapshotSpan(End, EndIncludingLineBreak)
                : new SnapshotSpan(_snapshot, _editBreakStart, _editEndIncludingLineBreak - _editBreakStart);
        }

        // A position inside elided text answers the whole hidden region as one element:
        // the caret and navigation treat a collapsed region as a single unit.
        if (_segments is { } segments)
        {
            int previousEnd = _editStart;
            foreach (var segment in segments)
            {
                if (bufferPosition.Position < segment.EditStart)
                {
                    return new SnapshotSpan(_snapshot, previousEnd, segment.EditStart - previousEnd);
                }

                if (bufferPosition.Position < segment.EditStart + segment.Length)
                {
                    break;
                }

                previousEnd = segment.EditStart + segment.Length;
            }
        }

        int rowOffset = GetRowOffset(bufferPosition.Position);
        return new SnapshotSpan(bufferPosition, GetTextElementLength(rowOffset));
    }

    private int GetTextElementLength(int rowOffset)
    {
        // UTF-16 surrogate pairs form a single text element (the ITextView contract's bar);
        // combining sequences are not merged, matching the documented behavior. The chars
        // come from the visual snapshot: that is what was formatted.
        int visualPosition = _paragraphStart + _rowStart + rowOffset;
        if (rowOffset < _rowLength - 1
            && char.IsHighSurrogate(_visualSnapshot[visualPosition])
            && char.IsLowSurrogate(_visualSnapshot[visualPosition + 1]))
        {
            return 2;
        }

        return 1;
    }

    /// <summary>Row-relative visual offset (0.._rowLength) of an edit position; positions
    /// inside elided text answer the offset where the following visible text resumes.</summary>
    private int GetRowOffset(int editPosition)
    {
        if (_segments is null)
        {
            return editPosition - (_paragraphStart + _rowStart);
        }

        foreach (var segment in _segments)
        {
            if (editPosition < segment.EditStart)
            {
                return segment.RowOffset;
            }

            if (editPosition < segment.EditStart + segment.Length)
            {
                return segment.RowOffset + (editPosition - segment.EditStart);
            }
        }

        return _rowLength;
    }

    /// <summary>Edit position of a row-relative visual offset (0.._rowLength inclusive).</summary>
    private int GetEditPosition(int rowOffset)
    {
        if (_segments is null)
        {
            return _paragraphStart + _rowStart + rowOffset;
        }

        foreach (var segment in _segments)
        {
            if (rowOffset < segment.RowOffset + segment.Length)
            {
                return segment.EditStart + (rowOffset - segment.RowOffset);
            }
        }

        return _editEnd;
    }

    private TextBounds GetElementBounds(int sourceIndex, int elementLength)
    {
        var boundsList = _textLine.GetTextBounds(sourceIndex, elementLength);
        if (boundsList.Count == 0)
        {
            return new TextBounds(TextRight, Top, 0.0, Height, TextTop, TextHeight * _lineTransform.VerticalScale);
        }

        var bounds = boundsList[0];
        var rect = bounds.Rectangle;
        bool rightToLeft = bounds.FlowDirection == Avalonia.Media.FlowDirection.RightToLeft;
        double leading = Left + (rightToLeft ? rect.Right : rect.Left);
        double width = rightToLeft ? -rect.Width : rect.Width;
        return new TextBounds(leading, Top, width, Height, TextTop, TextHeight * _lineTransform.VerticalScale);
    }

    private bool ContainsVirtualSpace(VirtualSnapshotPoint bufferPosition)
        => bufferPosition.Position == End && (_lineBreakLength > 0 || _endsAtEndOfBuffer);

    #endregion

    #region Formatting and rendering

    public Avalonia.Media.TextFormatting.TextRunProperties GetCharacterFormatting(SnapshotPoint bufferPosition)
    {
        ValidatePosition(bufferPosition);
        int index = Math.Min(GetRowOffset(bufferPosition.Position), Math.Max(0, _rowLength - 1)) + _rowStart;
        foreach (var run in _runs)
        {
            if (index < run.End)
            {
                return run.Properties;
            }
        }

        return _runs.Count > 0 ? _runs[^1].Properties : throw new InvalidOperationException("The line has no formatting runs.");
    }

    public Visual GetOrCreateVisual()
    {
        ThrowIfDisposed(0.0);
        return _visual ??= new LineVisual(this);
    }

    public void RemoveVisual() => _visual = null;

    internal void Render(Avalonia.Media.DrawingContext drawingContext, Point origin)
    {
        if (!_isDisposed && _rowLength > 0)
        {
            _textLine.Draw(drawingContext, origin + new Point(0.0, _lineTransform.TopSpace));
        }
    }

    internal double RenderWidth => TextWidth;

    internal double RenderHeight => Height;

    #endregion

    public void Dispose()
    {
        if (!_isDisposed)
        {
            _isDisposed = true;
            _visual = null;
            (_textLine as IDisposable)?.Dispose();
        }
    }

    private void ValidatePosition(SnapshotPoint bufferPosition)
    {
        ValidateSnapshot(bufferPosition.Snapshot);
        if (!ContainsBufferPosition(bufferPosition))
        {
            throw new ArgumentOutOfRangeException(nameof(bufferPosition));
        }
    }

    private void ValidateSnapshot(ITextSnapshot snapshot)
    {
        ThrowIfDisposed(0.0);
        if (snapshot != _snapshot)
        {
            throw new ArgumentException("The position belongs to a different snapshot.");
        }
    }

    private T ThrowIfDisposed<T>(T value)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        return value;
    }
}
