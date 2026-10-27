#nullable enable

namespace Microsoft.VisualStudio.Text.Editor.Implementation;

/// <summary>
/// The minimap's vertical layout, a pure function of the document, the viewport and the margin: how tall a line is
/// drawn, how far the picture is shifted when the document is taller than the margin, and where the viewport band
/// stands. Lengths are device-independent pixels; line positions are visual-snapshot lines, fractional where the view
/// is scrolled part of a line.
/// </summary>
internal readonly record struct MinimapLayout
{
    public MinimapLayout(int lineCount, double firstVisibleLine, double viewportLines, double height, double pixelsPerLine, MinimapSizing sizing)
    {
        LineCount = Math.Max(1, lineCount);
        Height = Math.Max(0.0, height);
        FirstVisibleLine = Math.Max(0.0, firstVisibleLine);
        ViewportLines = Math.Max(0.0, viewportLines);
        Pitch = Height <= 0.0 ? pixelsPerLine : sizing switch
        {
            MinimapSizing.Fit => Math.Min(pixelsPerLine, Height / LineCount),
            MinimapSizing.Fill => Height / LineCount,
            _ => pixelsPerLine,
        };

        // A document taller than the margin slides under it as the view scrolls, the way a scroll bar's thumb
        // travels its track: at the top of the document its top shows, at the bottom its bottom.
        MaxFirstLine = Math.Max(0.0, LineCount - ViewportLines);
        double overflow = (LineCount * Pitch) - Height;
        Offset = overflow > 0.0 && MaxFirstLine > 0.0
            ? overflow * Math.Clamp(FirstVisibleLine / MaxFirstLine, 0.0, 1.0)
            : 0.0;
    }

    public int LineCount { get; }

    public double Height { get; }

    public double FirstVisibleLine { get; }

    public double ViewportLines { get; }

    /// <summary>The height of one line in the picture.</summary>
    public double Pitch { get; }

    /// <summary>How far the picture is shifted up, so that <see cref="YOf"/> of the first line is <c>-Offset</c>.</summary>
    public double Offset { get; }

    /// <summary>The first line the view can scroll to by the minimap: the last one that still fills the viewport.</summary>
    public double MaxFirstLine { get; }

    public double ContentHeight => LineCount * Pitch;

    public double BandTop => YOf(FirstVisibleLine);

    public double BandHeight => ViewportLines * Pitch;

    /// <summary>The first line the margin shows, partly or wholly.</summary>
    public int FirstDrawnLine => LineAt(0.0);

    /// <summary>The last line the margin shows, partly or wholly.</summary>
    public int LastDrawnLine => Math.Min(LineAt(Math.Max(0.0, Math.Min(Height, ContentHeight - Offset) - 1e-6)), LineCount - 1);

    public double YOf(double line) => (line * Pitch) - Offset;

    /// <summary>The line at <paramref name="y"/>, clamped to the document.</summary>
    public int LineAt(double y) => Pitch <= 0.0 ? 0 : Math.Clamp((int)Math.Floor((y + Offset) / Pitch), 0, LineCount - 1);

    public bool BandContains(double y) => y >= BandTop && y < BandTop + Math.Max(BandHeight, 2.0);

    /// <summary>
    /// The first visible line that puts the band's top at <paramref name="bandTop"/>, clamped to what the view can scroll
    /// to. Where the picture slides, band and picture move together and the band travels the margin like a scroll bar's
    /// thumb its track; otherwise the band moves over a still picture.
    /// </summary>
    public double FirstLineForBandTop(double bandTop)
    {
        if (Pitch <= 0.0 || Height <= 0.0)
        {
            return 0.0;
        }

        double line;
        if (ContentHeight > Height && MaxFirstLine > 0.0)
        {
            double track = Height - BandHeight;
            line = track > 0.0 ? bandTop / track * MaxFirstLine : bandTop / Height * MaxFirstLine;
        }
        else
        {
            line = bandTop / Pitch;
        }

        return Math.Clamp(line, 0.0, MaxFirstLine);
    }
}
