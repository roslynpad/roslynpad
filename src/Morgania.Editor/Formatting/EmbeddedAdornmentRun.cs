#nullable enable

namespace Microsoft.VisualStudio.Text.Formatting.Implementation;

using Avalonia;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;

/// <summary>
/// A drawable run that reserves negotiated space in the formatted line for an intra-text
/// adornment so it becomes an embedded object in the text source. The run
/// consumes the adornment's replaced source span; the adornment control itself is
/// positioned over the reserved space by the intra-text support after layout.
/// </summary>
internal sealed class EmbeddedAdornmentRun : DrawableTextRun
{
    private readonly int _length;
    private readonly double _width;
    private readonly double _textHeight;
    private readonly double _baseline;
    private readonly TextRunProperties _properties;

    public EmbeddedAdornmentRun(int length, IAdornmentElement element, TextRunProperties properties)
    {
        _length = length;
        Element = element;
        _width = element.Width;
        _textHeight = element.TextHeight;
        _baseline = element.Baseline;
        _properties = properties;
    }

    public IAdornmentElement Element { get; }

    // Avalonia's TextLineImpl positions drawable runs from Properties.BaselineAlignment
    // (throwing on null); the surrounding text's properties keep the run baseline-aligned,
    // with the offset computed from the element's negotiated Baseline.
    public override TextRunProperties? Properties => _properties;

    public override int Length => _length;

    public override Size Size => new(_width, _textHeight);

    public override double Baseline => _baseline;

    public override void Draw(DrawingContext drawingContext, Point origin)
    {
        // Space reservation only; the adornment control renders on its adornment layer.
    }
}
