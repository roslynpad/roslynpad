#nullable enable

namespace Microsoft.VisualStudio.Text.Formatting.Implementation;

using Avalonia;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;

/// <summary>
/// A drawable run standing in for one tab character, sized by the formatter seam to reach
/// the next column-based tab stop (VS semantics; Avalonia's own tab handling advances by a
/// fixed increment from the current position instead). It takes width only: a tab draws
/// nothing, and a run with a height of its own enters the line's ascent and descent, to
/// which the formatter adds the line gap again — a run as tall as a line made its line
/// taller by that gap and moved its baseline down by half of it. The line's height comes
/// from its text and the paragraph's default font, as for any line.
/// </summary>
internal sealed class TabRun : DrawableTextRun
{
    private readonly double _width;
    private readonly TextRunProperties _properties;

    public TabRun(double width, TextRunProperties properties)
    {
        _width = width;
        _properties = properties;
    }

    public override TextRunProperties? Properties => _properties;

    public override int Length => 1;

    public override Size Size => new(_width, 0.0);

    public override double Baseline => 0.0;

    public override void Draw(DrawingContext drawingContext, Point origin)
    {
        // Whitespace: nothing to draw.
    }
}
