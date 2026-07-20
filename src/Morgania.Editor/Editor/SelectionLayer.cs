#nullable enable

namespace Microsoft.VisualStudio.Text.Editor.Implementation;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Microsoft.VisualStudio.Text.Classification;

/// <summary>
/// Renders every selection of the multi-selection broker as filled rectangles under the
/// text layer. Geometry comes from the formatted lines' normalized bounds, so bidi
/// selections render as their (possibly disjoint) runs.
/// </summary>
internal sealed class SelectionLayer : Control
{
    private static readonly IBrush DefaultActiveBrush = new SolidColorBrush(Color.FromRgb(0x26, 0x4F, 0x78));
    private static readonly IBrush DefaultInactiveBrush = new SolidColorBrush(Color.FromArgb(0x80, 0x26, 0x4F, 0x78));

    private IBrush _activeBrush = DefaultActiveBrush;
    private IBrush _inactiveBrush = DefaultInactiveBrush;

    private readonly WpfTextView _view;

    public SelectionLayer(WpfTextView view)
    {
        _view = view;
        IsHitTestVisible = false;
        ClipToBounds = true;
    }

    /// <summary>
    /// Re-resolves the selection brushes from the host-themeable format map entries
    /// (<see cref="SelectionFormatNames"/>); called at view creation and on format map changes.
    /// </summary>
    public void UpdateBrushes(IEditorFormatMap formatMap)
    {
        _activeBrush = ReadBackground(formatMap, SelectionFormatNames.Active) ?? DefaultActiveBrush;
        _inactiveBrush = ReadBackground(formatMap, SelectionFormatNames.Inactive) ?? DefaultInactiveBrush;
        InvalidateVisual();
    }

    private static IBrush? ReadBackground(IEditorFormatMap formatMap, string key)
    {
        var properties = formatMap.GetProperties(key);
        if (properties.TryGetValue(EditorFormatDefinition.BackgroundBrushId, out var brushValue) && brushValue is IBrush brush)
        {
            return brush;
        }

        return properties.TryGetValue(EditorFormatDefinition.BackgroundColorId, out var colorValue) && colorValue is Color color
            ? new SolidColorBrush(color)
            : null;
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

        var brush = broker.AreSelectionsActive ? _activeBrush : _inactiveBrush;
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
