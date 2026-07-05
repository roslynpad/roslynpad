#nullable enable

namespace Microsoft.VisualStudio.Text.Editor.Implementation;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Formatting;

/// <summary>
/// Scrolling helper over the view's layout engine. Vertical scrolling is a re-layout with a
/// translated anchor; horizontal scrolling adjusts <see cref="ITextView.ViewportLeft"/>.
/// </summary>
internal sealed class ViewScroller : IViewScroller
{
    private readonly WpfTextView _view;

    public ViewScroller(WpfTextView view)
    {
        _view = view;
    }

    public void ScrollViewportVerticallyByPixels(double distanceToScroll)
    {
        if (double.IsNaN(distanceToScroll))
        {
            throw new ArgumentOutOfRangeException(nameof(distanceToScroll));
        }

        var firstLine = _view.TextViewLines.FirstVisibleLine;
        _view.DisplayTextLineContainingBufferPosition(firstLine.Start, firstLine.Top - _view.ViewportTop + distanceToScroll, ViewRelativePosition.Top);
    }

    public void ScrollViewportVerticallyByLine(ScrollDirection direction) => ScrollViewportVerticallyByLines(direction, 1);

    public void ScrollViewportVerticallyByLines(ScrollDirection direction, int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        double distance = count * _view.LineHeight;
        ScrollViewportVerticallyByPixels(direction == ScrollDirection.Up ? distance : -distance);
    }

    public bool ScrollViewportVerticallyByPage(ScrollDirection direction)
    {
        // Preserve one line of overlap, like the WPF editor.
        double distance = Math.Max(_view.LineHeight, _view.ViewportHeight - _view.LineHeight);
        var firstLineBefore = _view.TextViewLines.FirstVisibleLine.Start;
        ScrollViewportVerticallyByPixels(direction == ScrollDirection.Up ? distance : -distance);
        return _view.TextViewLines.FirstVisibleLine.Start != firstLineBefore;
    }

    public void ScrollViewportHorizontallyByPixels(double distanceToScroll)
    {
        _view.ViewportLeft += distanceToScroll;
    }

    public void EnsureSpanVisible(SnapshotSpan span)
        => EnsureSpanVisible(new VirtualSnapshotSpan(span), EnsureSpanVisibleOptions.None);

    public void EnsureSpanVisible(SnapshotSpan span, EnsureSpanVisibleOptions options)
        => EnsureSpanVisible(new VirtualSnapshotSpan(span), options);

    public void EnsureSpanVisible(VirtualSnapshotSpan span, EnsureSpanVisibleOptions options)
    {
        var start = span.Start.Position.TranslateTo(_view.TextSnapshot, PointTrackingMode.Negative);
        var lines = _view.TextViewLines;
        bool minimal = (options & EnsureSpanVisibleOptions.AlwaysCenter) == 0;
        if (!lines.ContainsBufferPosition(start))
        {
            // Off-screen: bring the position in with minimal movement (top if scrolling up,
            // bottom if scrolling down) or center it.
            bool below = start > lines.FormattedSpan.End;
            if (!minimal)
            {
                _view.DisplayTextLineContainingBufferPosition(
                    start, Math.Max(0.0, (_view.ViewportHeight - _view.LineHeight) / 2.0), ViewRelativePosition.Top);
            }
            else if (below)
            {
                _view.DisplayTextLineContainingBufferPosition(start, 0.0, ViewRelativePosition.Bottom);
            }
            else
            {
                _view.DisplayTextLineContainingBufferPosition(start, 0.0, ViewRelativePosition.Top);
            }
        }
        else
        {
            var line = lines.GetTextViewLineContainingBufferPosition(start);
            if (line.VisibilityState != VisibilityState.FullyVisible)
            {
                bool below = line.Bottom > _view.ViewportBottom;
                _view.DisplayTextLineContainingBufferPosition(
                    start, 0.0, below ? ViewRelativePosition.Bottom : ViewRelativePosition.Top);
            }
        }

        // Horizontal: bring the span's start x-coordinate into the viewport.
        var visibleLine = _view.TextViewLines.GetTextViewLineContainingBufferPosition(start);
        if (visibleLine is not null)
        {
            var bounds = visibleLine.GetCharacterBounds(new VirtualSnapshotPoint(start, span.Start.VirtualSpaces));
            if (bounds.Left < _view.ViewportLeft)
            {
                _view.ViewportLeft = Math.Max(0.0, bounds.Left - (2.0 * _view.FormattedLineSource.ColumnWidth));
            }
            else if (bounds.Right > _view.ViewportRight)
            {
                _view.ViewportLeft += bounds.Right - _view.ViewportRight + (2.0 * _view.FormattedLineSource.ColumnWidth);
            }
        }
    }
}
