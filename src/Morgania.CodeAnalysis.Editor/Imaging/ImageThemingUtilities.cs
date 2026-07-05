using Avalonia.Media;

namespace Morgania.CodeAnalysis.Editor;

/// <summary>
/// Port of Visual Studio's ImageThemingUtilities luminosity transform (the "grayscale
/// inversion" the VS image service applies): image-catalog icons are authored for light
/// backgrounds, and this remaps each color's luminosity to keep its contrast against the
/// actual background while protecting saturated accent hues (the gold/orange band).
/// </summary>
internal static class ImageThemingUtilities
{
    /// <summary>Returns a copy of the drawing with every solid brush adapted to the background.</summary>
    public static Drawing TransformDrawing(Drawing drawing, Color background)
    {
        var backgroundLuminosity = GetLuminosity(background);
        return Transform(drawing, backgroundLuminosity);
    }

    private static Drawing Transform(Drawing drawing, double backgroundLuminosity) => drawing switch
    {
        DrawingGroup group => TransformGroup(group, backgroundLuminosity),
        GeometryDrawing geometry => new GeometryDrawing
        {
            Geometry = geometry.Geometry,
            Pen = geometry.Pen,
            Brush = geometry.Brush is ISolidColorBrush solid
                ? new SolidColorBrush(TransformColor(solid.Color, backgroundLuminosity), solid.Opacity)
                : geometry.Brush,
        },
        _ => drawing,
    };

    private static DrawingGroup TransformGroup(DrawingGroup group, double backgroundLuminosity)
    {
        var transformed = new DrawingGroup { Opacity = group.Opacity, Transform = group.Transform };
        foreach (var child in group.Children)
        {
            transformed.Children.Add(Transform(child, backgroundLuminosity));
        }

        return transformed;
    }

    private static Color TransformColor(Color color, double backgroundLuminosity)
    {
        var (hue, saturation, luminosity) = RgbToHsl(color);
        var transformed = TransformLuminosity(hue, saturation, luminosity, backgroundLuminosity);
        return HslToRgb(hue, saturation, transformed, color.A);
    }

    private static double GetLuminosity(Color color) => RgbToHsl(color).Luminosity;

    private static double TransformLuminosity(double hue, double saturation, double luminosity, double backgroundLuminosity)
    {
        if (backgroundLuminosity < 0.5)
        {
            if (luminosity >= 0.96)
            {
                // Near-white maps onto the contrast of the (dark) background.
                return backgroundLuminosity * (luminosity - 0.96) / 0.04 + (1.0 - backgroundLuminosity);
            }

            // Saturated colors in the gold/orange band (hue ≈ 37°) keep more of their
            // original luminosity; everything else is inverted against the background.
            var desaturation = saturation >= 0.2 ? (saturation <= 0.3 ? 1.0 - (saturation - 0.2) / 0.1 : 0.0) : 1.0;
            var weight = Math.Max(Math.Min(1.0, Math.Abs(hue - 37.0) / 20.0), desaturation);
            var pivot = ((backgroundLuminosity - 1.0) * 0.66 / 0.96 + 1.0) * weight + 0.66 * (1.0 - weight);
            return luminosity < 0.66
                ? (pivot - 1.0) / 0.66 * luminosity + 1.0
                : (pivot - backgroundLuminosity) / -0.34 * (luminosity - 0.66) + pivot;
        }

        return luminosity < 0.96
            ? luminosity
            : backgroundLuminosity * (luminosity - 0.96) / 0.04 + (1.0 - backgroundLuminosity);
    }

    private static (double Hue, double Saturation, double Luminosity) RgbToHsl(Color color)
    {
        var r = color.R / 255.0;
        var g = color.G / 255.0;
        var b = color.B / 255.0;

        var max = Math.Max(r, Math.Max(g, b));
        var min = Math.Min(r, Math.Min(g, b));
        var luminosity = (max + min) / 2.0;
        var chroma = max - min;
        if (chroma == 0)
        {
            return (0.0, 0.0, luminosity);
        }

        var saturation = luminosity <= 0.5 ? chroma / (max + min) : chroma / (2.0 - max - min);
        var hue =
            max == r ? (g - b) / chroma + (g < b ? 6.0 : 0.0) :
            max == g ? (b - r) / chroma + 2.0 :
            (r - g) / chroma + 4.0;
        return (hue * 60.0, saturation, luminosity);
    }

    private static Color HslToRgb(double hue, double saturation, double luminosity, byte alpha)
    {
        if (saturation == 0)
        {
            var value = (byte)Math.Round(Math.Clamp(luminosity, 0.0, 1.0) * 255.0);
            return Color.FromArgb(alpha, value, value, value);
        }

        var h = hue / 360.0;
        var q = luminosity < 0.5 ? luminosity * (1.0 + saturation) : luminosity + saturation - luminosity * saturation;
        var p = 2.0 * luminosity - q;
        return Color.FromArgb(alpha, Component(h + 1.0 / 3.0), Component(h), Component(h - 1.0 / 3.0));

        byte Component(double t)
        {
            if (t < 0.0)
            {
                t += 1.0;
            }
            else if (t > 1.0)
            {
                t -= 1.0;
            }

            var value = t switch
            {
                < 1.0 / 6.0 => p + (q - p) * 6.0 * t,
                < 1.0 / 2.0 => q,
                < 2.0 / 3.0 => p + (q - p) * (2.0 / 3.0 - t) * 6.0,
                _ => p,
            };
            return (byte)Math.Round(Math.Clamp(value, 0.0, 1.0) * 255.0);
        }
    }
}
