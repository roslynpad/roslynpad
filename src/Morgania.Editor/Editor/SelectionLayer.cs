#nullable enable

namespace Microsoft.VisualStudio.Text.Editor.Implementation;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

/// <summary>
/// Renders every selection of the multi-selection broker as filled rectangles under the
/// text layer. Geometry comes from the formatted lines' normalized bounds, so bidi
/// selections render as their (possibly disjoint) runs.
/// </summary>
internal sealed class SelectionLayer : Control
{
    private static readonly IBrush ActiveBrush = new SolidColorBrush(Color.FromArgb(0x66, 0x26, 0x4F, 0x78));
    private static readonly IBrush InactiveBrush = new SolidColorBrush(Color.FromArgb(0x44, 0x68, 0x68, 0x68));

    private readonly WpfTextView _view;

    public SelectionLayer(WpfTextView view)
    {
        _view = view;
        IsHitTestVisible = false;
        ClipToBounds = true;
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        if (_view.IsClosed || _view.InLayout)
        {
            return;
        }

        // Render is strictly read-only: never create the broker or trigger a layout here
        // (invalidating a visual during the render pass throws in Avalonia).
        var broker = _view.ExistingBroker;
        if (broker is null || !_view.TryGetTextViewLines(out var textViewLines))
        {
            return;
        }

        var brush = broker.AreSelectionsActive ? ActiveBrush : InactiveBrush;
        var formattedSpan = textViewLines.FormattedSpan;
        foreach (var virtualSpan in broker.VirtualSelectedSpans)
        {
            if (virtualSpan.IsEmpty)
            {
                continue;
            }

            var span = virtualSpan.SnapshotSpan;
            var visible = formattedSpan.Overlap(span);
            if (visible is not { } clipped || (clipped.IsEmpty && span.Length > 0))
            {
                continue;
            }

            foreach (var bounds in textViewLines.GetNormalizedTextBounds(clipped))
            {
                var rect = new Rect(
                    bounds.Left - _view.ViewportLeft,
                    bounds.Top - _view.ViewportTop,
                    Math.Abs(bounds.Width),
                    bounds.Height);
                if (rect.Width > 0)
                {
                    context.FillRectangle(brush, rect);
                }
            }

            // A box selection runs through virtual space on lines shorter than its columns;
            // that segment has no text bounds, so it is measured off the endpoints' caret
            // coordinates (both endpoints of a per-line box span sit on the same line).
            if (virtualSpan.End.IsInVirtualSpace && textViewLines.ContainsBufferPosition(virtualSpan.End.Position))
            {
                var line = textViewLines.GetTextViewLineContainingBufferPosition(virtualSpan.End.Position);
                double left = virtualSpan.Start.IsInVirtualSpace && line.ContainsBufferPosition(virtualSpan.Start.Position)
                    ? line.GetCharacterBounds(virtualSpan.Start).Left
                    : line.TextRight;
                var endBounds = line.GetCharacterBounds(virtualSpan.End);
                var rect = new Rect(
                    left - _view.ViewportLeft,
                    endBounds.Top - _view.ViewportTop,
                    endBounds.Left - left,
                    endBounds.Height);
                if (rect.Width > 0)
                {
                    context.FillRectangle(brush, rect);
                }
            }
        }
    }
}
