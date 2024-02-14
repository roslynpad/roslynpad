using System.Windows;
using System.Windows.Media;
using RoslynPad.Themes;

namespace RoslynPad;

#pragma warning disable CA1010 // Generic interface should also be implemented

public class ThemeDictionary : ResourceDictionary
{
    private static readonly IReadOnlySet<string> s_colors = typeof(ThemeDictionary).GetFields()
        .Where(t => t.IsStatic && t.Attributes.HasFlag(System.Reflection.FieldAttributes.Literal))
        .Select(t => (string)t.GetValue(null)!).ToHashSet();

    public ThemeDictionary(Theme theme)
    {
        Initialize(theme);
        SetThemeColorForSystemKeys(Foreground, SystemColors.WindowTextBrushKey, SystemColors.WindowTextColorKey);
        SetThemeColorForSystemKeys(PanelBackground, SystemColors.WindowBrushKey, SystemColors.WindowColorKey);
        SetThemeColorForSystemKeys(FocusBorder, SystemColors.ActiveBorderBrushKey, SystemColors.ActiveBorderColorKey);
    }

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
    public const string StatusBarBackground = "statusBar.background";
    public const string StatusBarForeground = "statusBar.foreground";
    public const string StatusBarItemErrorBackground = "statusBarItem.errorBackground";
    public const string StatusBarItemErrorForeground = "statusBarItem.errorForeground";
    public const string FocusBorder = "focusBorder";
    public const string ListActiveSelectionBackground = "list.activeSelectionBackground";
    public const string ListActiveSelectionForeground = "list.activeSelectionForeground";
    public const string ListInactiveSelectionBackground = "list.inactiveSelectionBackground";

    private void Initialize(Theme theme)
    {
        if (theme.Colors is null)
        {
            return;
        }

        foreach (var id in s_colors)
        {
            if (theme.TryGetColor(id) is { } color)
            {
                SetThemeColor(id, color);
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

    private static SolidColorBrush CreateBrush(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }

    private static Color ParseColor(string color) => (Color)ColorConverter.ConvertFromString(color);
}
