using System.Globalization;
using Avalonia.Controls;
using Avalonia.Media;
using RoslynPad.Themes;

namespace RoslynPad;

public abstract class ThemeDictionaryBase : ResourceDictionary
{
    protected ThemeDictionaryBase(Theme theme)
    {
    }

    protected void SetThemeColor(string name, string colorString)
    {
        var brush = CreateBrush(ParseColor(colorString));
        this[name] = brush;
        this[GetColorKey(name)] = brush.Color;
    }

    protected void SetThemeColorForSystemKeys(string name, object brushKey, object colorKey)
    {
        this[brushKey] = this[name];
        this[colorKey] = this[GetColorKey(name)];
    }

    private static string GetColorKey(string key) => key + "Color";

    protected static SolidColorBrush? CreateBrush(Theme theme, string id)
    {
        return theme.TryGetColor(id) is { } color ? CreateBrush(ParseColor(color)) : null;
    }

    private static SolidColorBrush CreateBrush(Color color) => new(color);

    private static Color ParseColor(string color) => ParseThemeColor(color);

    /// <summary>
    /// Parses a VS Code theme color, which uses CSS #RRGGBBAA ordering for 8-digit hex values
    /// (Avalonia's <see cref="Color.Parse"/> would read those as #AARRGGBB).
    /// </summary>
    internal static Color ParseThemeColor(string color)
    {
        if (color.Length == 9 && color[0] == '#' && uint.TryParse(color.AsSpan(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var rgba))
        {
            return Color.FromUInt32(((rgba & 0xFF) << 24) | (rgba >> 8));
        }

        return Color.Parse(color);
    }
}
