using System.Windows;
using RoslynPad.Themes;

#pragma warning disable CA1010 // Generic interface should also be implemented

namespace RoslynPad;

public class ThemeDictionary : ThemeDictionaryBase
{
    private static readonly IReadOnlySet<string> s_colors = typeof(ThemeDictionary).GetFields()
        .Where(t => t.IsStatic && t.Attributes.HasFlag(System.Reflection.FieldAttributes.Literal))
        .Select(t => (string)t.GetValue(null)!).ToHashSet();

    public ThemeDictionary(Theme theme) : base(theme)
    {
        Initialize(theme);
        SetThemeColorForSystemKeys(Foreground, SystemColors.WindowTextBrushKey, SystemColors.WindowTextColorKey);
        SetThemeColorForSystemKeys(PanelBackground, SystemColors.WindowBrushKey, SystemColors.WindowColorKey);
        SetThemeColorForSystemKeys(FocusBorder, SystemColors.ActiveBorderBrushKey, SystemColors.ActiveBorderColorKey);
        SetThemeColorForSystemKeys(PanelBackground, SystemColors.ControlBrushKey, SystemColors.ControlColorKey);
        SetThemeColorForSystemKeys(TabBarBorder, SystemColors.ControlDarkBrushKey, SystemColors.ControlDarkColorKey);
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
    public const string TabActiveBackground ="tab.activeBackground";
    public const string TabInactiveBackground = "tab.inactiveBackground";
    public const string TabActiveForeground = "tab.activeForeground";
    public const string TabInactiveForeground = "tab.inactiveForeground";
    public const string TabHoverBackground = "tab.hoverBackground";
    public const string TabHoverForeground = "tab.hoverForeground";
    public const string TabActiveBorder = "tab.activeBorder";
    public const string TabActiveBorderTop = "tab.activeBorderTop";
    public const string TabHoverBorder = "tab.hoverBorder";
    public const string TabBorder = "tab.border";
    public const string InputBorder = "input.border";
    public const string TitleBarActiveBackground = "titleBar.activeBackground";
    public const string InputBackground = "input.background";
    public const string InputForeground = "input.foreground";
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
}
