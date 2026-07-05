#nullable enable

namespace Microsoft.VisualStudio.Text.Formatting.Implementation;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

/// <summary>
/// The visual for one formatted line: a lightweight control that draws the line's formatted
/// text at its own origin. The view's text layer positions it at
/// (line.Left - ViewportLeft, line.Top - ViewportTop).
/// </summary>
internal sealed class LineVisual : Control
{
    private readonly FormattedLine _line;

    public LineVisual(FormattedLine line)
    {
        _line = line;
        IsHitTestVisible = false;
    }

    protected override Size MeasureOverride(Size availableSize)
        => new(_line.RenderWidth, _line.RenderHeight);

    public override void Render(DrawingContext context)
        => _line.Render(context, default);
}
