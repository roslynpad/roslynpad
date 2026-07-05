#nullable enable

namespace Microsoft.VisualStudio.Text.Formatting.Implementation;

using Avalonia;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;

/// <summary>
/// A drawable run standing in for one tab character, sized by the formatter seam to reach
/// the next column-based tab stop (VS semantics; Avalonia's own tab handling advances by a
/// fixed increment from the current position instead).
/// </summary>
internal sealed class TabRun : DrawableTextRun
{
    private readonly double _width;
    private readonly double _height;
    private readonly double _baseline;
    private readonly TextRunProperties _properties;

    public TabRun(double width, double height, double baseline, TextRunProperties properties)
    {
        _width = width;
        _height = height;
        _baseline = baseline;
        _properties = properties;
    }

    public override TextRunProperties? Properties => _properties;

    public override int Length => 1;

    public override Size Size => new(_width, _height);

    public override double Baseline => _baseline;

    public override void Draw(DrawingContext drawingContext, Point origin)
    {
        // Whitespace: nothing to draw.
    }
}
