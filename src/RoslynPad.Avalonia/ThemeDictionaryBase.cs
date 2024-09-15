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

    private static Color ParseColor(string color) => Color.Parse(color);
}
