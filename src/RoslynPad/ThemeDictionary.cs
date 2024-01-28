using System.Windows;
using System.Windows.Media;
using RoslynPad.Themes;

namespace RoslynPad;

#pragma warning disable CA1010 // Generic interface should also be implemented
#pragma warning disable CA1859 // Use concrete types when possible for improved performance

public class ThemeDictionary : ResourceDictionary
{
    private static readonly IReadOnlySet<string> s_colors = typeof(ThemeDictionary).GetFields()
        .Where(t => t.IsStatic && t.Attributes.HasFlag(System.Reflection.FieldAttributes.Literal))
        .Select(t => (string)t.GetValue(null)!).ToHashSet();

    private static readonly IReadOnlyDictionary<string, ColorDefaults> s_defaults = new Dictionary<string, ColorDefaults>()
    {
        [ScrollBarShadow] = new(dark: "#000000", light: "#DDDDDD"),
        [ScrollBarSliderBackground] = new(dark: "#66797979", light: "#66646464"),
        [ScrollBarSliderHoverBackground] = new(dark: "#B2646464", light: "#B2646464"),
        [ScrollBarSliderActiveBackground] = new(dark: "#66BFBFBF", light: "#99000000")
    };

    public ThemeDictionary(Theme theme)
    {
        Theme = theme;
        Initialize(theme);
        SetThemeColorForSystemKeys(Foreground, SystemColors.WindowTextBrushKey, SystemColors.WindowTextColorKey);
        SetThemeColorForSystemKeys(PanelBackground, SystemColors.WindowBrushKey, SystemColors.WindowColorKey);
    }

    public Theme Theme { get; }

    public const string TabBarBackground = "editorGroupHeader.tabsBackground";
    public const string TabBarBorder = "editorGroupHeader.tabsBorder";
    public const string ScrollBarSliderBackground = "scrollbarSlider.background";
    public const string ScrollBarSliderActiveBackground = "scrollbarSlider.activeBackground";
    public const string ScrollBarSliderHoverBackground = "scrollbarSlider.hoverBackground";
    public const string ScrollBarShadow = "scrollbar.shadow";
    public const string EditorBackground = "editor.background";
    public const string EditorForeground = "editor.foreground";
    public const string Foreground = "foreground";
    public const string PanelBackground = "panel.background";

    private void Initialize(Theme theme)
    {
        if (theme.Colors is null)
        {
            return;
        }

        var isDark = string.Equals(theme.Type, "dark", StringComparison.OrdinalIgnoreCase);

        foreach (var color in s_defaults)
        {
            this[color.Key] = isDark ? color.Value.DarkBrush : color.Value.LightBrush;
        }

        foreach (var themeColor in theme.Colors)
        {
            if (s_colors.Contains(themeColor.Key))
            {
                SetThemeColor(themeColor.Key, themeColor.Value);
            }
        }
    }

    private void SetThemeColor(string name, string colorString)
    {
        var color = ParseColor(colorString);
        this[name] = CreateBrush(color);
        this[GetColorKey(name)] = color;
    }

    private void SetThemeColorForSystemKeys(string name, ResourceKey brushKey, ResourceKey colorKey)
    {
        this[brushKey] = this[name];
        this[colorKey] = this[GetColorKey(name)];
    }

    private static string GetColorKey(string key) => key + "Color";

    private static SolidColorBrush CreateBrush(string color) => CreateBrush(ParseColor(color));

    private static SolidColorBrush CreateBrush(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }

    private static Color ParseColor(string color) => (Color)ColorConverter.ConvertFromString(color);

    private class ColorDefaults(string dark, string light)
    {
        public SolidColorBrush DarkBrush { get; } = CreateBrush(dark);
        public SolidColorBrush LightBrush { get; } = CreateBrush(light);
    }
}
