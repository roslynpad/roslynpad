#nullable enable

namespace Microsoft.VisualStudio.Text.Editor.Implementation;

using System.Runtime.InteropServices;

using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Media.TextFormatting;

/// <summary>
/// The minimap's picture as premultiplied BGRA pixels. Rectangles are filled by coverage, so a line or a character
/// narrower than a device pixel still contributes its share of colour: the picture stays true to the text at any scale.
/// </summary>
internal sealed class MinimapCanvas
{
    private int[] _pixels = [];

    public int Width { get; private set; }

    public int Height { get; private set; }

    /// <summary>Sizes the canvas and clears it.</summary>
    public void Reset(int width, int height)
    {
        Width = Math.Max(0, width);
        Height = Math.Max(0, height);
        if (_pixels.Length < Width * Height)
        {
            _pixels = new int[Width * Height];
        }
        else
        {
            Array.Clear(_pixels, 0, Width * Height);
        }
    }

    /// <summary>Blends <paramref name="color"/> at <paramref name="opacity"/> over the rectangle, pixels at its edges by the share they have of it.</summary>
    public void Fill(double left, double top, double right, double bottom, Color color, double opacity)
    {
        left = Math.Max(0.0, left);
        top = Math.Max(0.0, top);
        right = Math.Min(Width, right);
        bottom = Math.Min(Height, bottom);
        double alpha = color.A / 255.0 * opacity;
        if (right <= left || bottom <= top || alpha <= 0.0)
        {
            return;
        }

        int x0 = (int)left;
        int x1 = (int)Math.Ceiling(right);
        int y0 = (int)top;
        int y1 = (int)Math.Ceiling(bottom);
        for (int y = y0; y < y1; y++)
        {
            double rowCoverage = Math.Min(bottom, y + 1) - Math.Max(top, y);
            int row = y * Width;
            for (int x = x0; x < x1; x++)
            {
                double coverage = rowCoverage * (Math.Min(right, x + 1) - Math.Max(left, x));
                Blend(ref _pixels[row + x], color, alpha * coverage);
            }
        }
    }

    /// <summary>Copies the picture into <paramref name="bitmap"/>, which has the canvas's size and a BGRA premultiplied format.</summary>
    public void CopyTo(WriteableBitmap bitmap)
    {
        using var buffer = bitmap.Lock();
        int width = Math.Min(Width, buffer.Size.Width);
        for (int y = 0; y < Math.Min(Height, buffer.Size.Height); y++)
        {
            Marshal.Copy(_pixels, y * Width, buffer.Address + (y * buffer.RowBytes), width);
        }
    }

    /// <summary>The pixel at <paramref name="x"/>, <paramref name="y"/> as a colour (premultiplied), for tests.</summary>
    internal Color GetPixel(int x, int y)
    {
        uint value = (uint)_pixels[(y * Width) + x];
        return Color.FromArgb((byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)value);
    }

    private static void Blend(ref int pixel, Color color, double alpha)
    {
        if (alpha <= 0.0)
        {
            return;
        }

        alpha = Math.Min(1.0, alpha);
        uint destination = (uint)pixel;
        double keep = 1.0 - alpha;
        uint b = (uint)Math.Round((color.B * alpha) + ((destination & 0xFF) * keep));
        uint g = (uint)Math.Round((color.G * alpha) + (((destination >> 8) & 0xFF) * keep));
        uint r = (uint)Math.Round((color.R * alpha) + (((destination >> 16) & 0xFF) * keep));
        uint a = (uint)Math.Round((255.0 * alpha) + ((destination >> 24) * keep));
        pixel = (int)(b | (g << 8) | (r << 16) | (a << 24));
    }
}

/// <summary>
/// How much of each part of a character's cell its glyph inks, from the glyph outlines of one typeface: a grid of
/// <see cref="Columns"/> by <see cref="Rows"/> densities from 0 to 1, row by row from the top of the line box. Null for a
/// character the platform builds no outline for.
/// </summary>
internal sealed class MinimapGlyphMasks(Typeface typeface)
{
    public const int Columns = 2;
    public const int Rows = 4;

    private const double SampleSize = 32.0;
    private const int SamplesPerCell = 4;

    private readonly Dictionary<char, float[]?> _masks = [];

    public Typeface Typeface { get; } = typeface;

    public float[]? Get(char c)
    {
        if (!_masks.TryGetValue(c, out var mask))
        {
            mask = Build(c);
            _masks[c] = mask;
        }

        return mask;
    }

    private float[]? Build(char c)
    {
        using var layout = new TextLayout(new string(c, 1), Typeface, SampleSize, null);
        var line = layout.TextLines[0];
        double width = line.WidthIncludingTrailingWhitespace;
        double height = line.Height;
        if (width <= 0.0 || height <= 0.0 || line.TextRuns.OfType<ShapedTextRun>().FirstOrDefault() is not { } run
            || run.GlyphRun.BuildGeometry() is not { } outline || outline.Bounds.Width <= 0.0)
        {
            return null;
        }

        // The outline stands where the run draws its glyphs: the baseline at the run's baseline origin, the line box
        // above and below it by the line's metrics.
        double top = run.GlyphRun.BaselineOrigin.Y - line.Baseline;
        var mask = new float[Columns * Rows];
        float densest = 0f;
        for (int row = 0; row < Rows; row++)
        {
            for (int column = 0; column < Columns; column++)
            {
                int hits = 0;
                for (int sy = 0; sy < SamplesPerCell; sy++)
                {
                    for (int sx = 0; sx < SamplesPerCell; sx++)
                    {
                        var point = new Point(
                            (column + ((sx + 0.5) / SamplesPerCell)) * width / Columns,
                            top + ((row + ((sy + 0.5) / SamplesPerCell)) * height / Rows));
                        if (outline.FillContains(point))
                        {
                            hits++;
                        }
                    }
                }

                float density = hits / (float)(SamplesPerCell * SamplesPerCell);
                mask[(row * Columns) + column] = density;
                densest = Math.Max(densest, density);
            }
        }

        // Strokes are thin against a cell: lifted so the glyph's densest part inks fully, a miniature reads as its letter
        // rather than as a haze.
        if (densest > 0f)
        {
            for (int i = 0; i < mask.Length; i++)
            {
                mask[i] = Math.Min(1f, mask[i] / densest);
            }
        }

        return mask;
    }
}
