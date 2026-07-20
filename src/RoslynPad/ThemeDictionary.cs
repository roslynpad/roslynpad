using Avalonia.Media;
using RoslynPad.Themes;

namespace RoslynPad;

public class ThemeDictionary : ThemeDictionaryBase
{
    private static readonly IReadOnlySet<string> s_colors = typeof(ThemeDictionary).GetFields()
        .Where(t => t.IsStatic && t.Attributes.HasFlag(System.Reflection.FieldAttributes.Literal))
        .Select(t => (string)t.GetValue(null)!).ToHashSet();

    public ThemeDictionary(Theme theme) : base(theme)
    {
        Initialize(theme);
    }

    public const string TabBarBackground = "editorGroupHeader.tabsBackground";
    public const string TabBarBorder = "editorGroupHeader.tabsBorder";
    public const string ScrollBarSliderBackground = "scrollbarSlider.background";
    public const string ScrollBarSliderActiveBackground = "scrollbarSlider.activeBackground";
    public const string ScrollBarSliderHoverBackground = "scrollbarSlider.hoverBackground";
    public const string ScrollBarShadow = "scrollbar.shadow";
    public const string EditorBackground = "editor.background";
    public const string EditorForeground = "editor.foreground";
    public const string EditorSelectionBackground = "editor.selectionBackground";
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
    public const string TabActiveBackground = "tab.activeBackground";
    public const string TabUnfocusedActiveBackground = "tab.unfocusedActiveBackground";
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
    public const string FoldingControlForeground = "editorGutter.foldingControlForeground";
    public const string PanelBorder = "panel.border";
    public const string ToolbarHoverBackground = "toolbar.hoverBackground";
    public const string ToolbarActiveBackground = "toolbar.activeBackground";
    public const string InputOptionActiveBackground = "inputOption.activeBackground";
    public const string InputOptionActiveBorder = "inputOption.activeBorder";
    public const string SideBarBackground = "sideBar.background";
    public const string ListHoverBackground = "list.hoverBackground";
    public const string DescriptionForeground = "descriptionForeground";
    public const string StatusBarItemHoverBackground = "statusBarItem.hoverBackground";
    public const string EditorGroupBorder = "editorGroup.border";
    public const string EditorGroupDropBackground = "editorGroup.dropBackground";
    public const string SashHoverBorder = "sash.hoverBorder";
    public const string IconForeground = "icon.foreground";
    public const string PanelTitleActiveForeground = "panelTitle.activeForeground";
    public const string PanelTitleInactiveForeground = "panelTitle.inactiveForeground";

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

        MapDockResources(theme.Type);
        MapScrollBarDefaults(theme.Type);
    }

    /// <summary>
    /// VS Code's coded scrollbar defaults (colorRegistry.ts) for themes that don't set
    /// <c>scrollbarSlider.*</c> — the bundled 2026 themes do, the classic ones don't.
    /// </summary>
    private void MapScrollBarDefaults(ThemeType type)
    {
        SetDefaultThemeColor(ScrollBarSliderBackground, type == ThemeType.Dark ? "#79797966" : "#64646466");
        SetDefaultThemeColor(ScrollBarSliderHoverBackground, "#646464B3");
        SetDefaultThemeColor(ScrollBarSliderActiveBackground, type == ThemeType.Dark ? "#BFBFBF66" : "#00000099");
    }

    private void SetDefaultThemeColor(string name, string color)
    {
        if (!TryGetValue(name, out _))
        {
            SetThemeColor(name, color);
        }
    }

    /// <summary>
    /// Maps VS Code theme colors onto the semantic brush keys consumed by Dock's control themes,
    /// overriding the DockFluentTheme accent defaults.
    /// </summary>
    private void MapDockResources(ThemeType type)
    {
        // VS Code sashes are invisible until interacted with; tab strips are part of
        // the framed cards, so they stay transparent too
        this["DockSplitterIdleBrush"] = Brushes.Transparent;
        this["DockTabBackgroundBrush"] = Brushes.Transparent;
        this["DockDocumentTabStripBackgroundBrush"] = Brushes.Transparent;

        MapDockBrush("DockSurfaceWorkbenchBrush", TabBarBackground, SideBarBackground);
        MapDockBrush("DockSurfaceSidebarBrush", SideBarBackground);
        MapDockBrush("DockSurfaceEditorBrush", EditorBackground);
        MapDockBrush("DockSurfacePanelBrush", PanelBackground, EditorBackground);
        MapDockBrush("DockSurfaceHeaderBrush", TabUnfocusedActiveBackground, TabActiveBackground);
        MapDockBrush("DockSurfaceHeaderActiveBrush", ListInactiveSelectionBackground);

        MapDockBrush("DockBorderSubtleBrush", EditorGroupBorder, PanelBorder);
        MapDockBrush("DockBorderStrongBrush", EditorGroupBorder, PanelBorder);
        MapDockBrush("DockSeparatorBrush", TabBarBorder, EditorGroupBorder);
        MapDockBrush("DockDocumentContentBorderBrush", EditorGroupBorder, PanelBorder);

        MapDockBrush("DockSplitterHoverBrush", SashHoverBorder, FocusBorder);
        MapDockBrush("DockSplitterDragBrush", SashHoverBorder, FocusBorder);
        MapDockBrush("DockApplicationAccentBrushIndicator", SashHoverBorder, FocusBorder);

        MapDockBrush("DockTabHoverBackgroundBrush", ToolbarHoverBackground);
        MapDockBrush("DockTabActiveBackgroundBrush", TabActiveBackground);
        MapDockBrush("DockTabActiveIndicatorBrush", TabActiveBorderTop, FocusBorder);
        MapDockBrush("DockTabForegroundBrush", TabInactiveForeground);
        MapDockBrush("DockTabSelectedForegroundBrush", TabActiveForeground);
        MapDockBrush("DockTabActiveForegroundBrush", TabActiveForeground);
        MapDockBrush("DockDocumentTabSelectedForegroundBrush", TabActiveForeground);
        MapDockBrush("DockDocumentTabPointerOverForegroundBrush", TabHoverForeground, TabActiveForeground);
        MapDockBrush("DockDocumentTabCloseSelectedForegroundBrush", TabActiveForeground);
        MapDockBrush("DockDocumentTabClosePointerOverForegroundBrush", TabActiveForeground);
        MapDockBrush("DockTabCloseHoverBackgroundBrush", ToolbarHoverBackground);

        MapDockBrush("DockTargetIndicatorBrush", EditorGroupDropBackground, ListActiveSelectionBackground);

        MapDockBrush("DockChromeButtonForegroundBrush", IconForeground, Foreground);
        MapDockBrush("DockChromeButtonHoverBackgroundBrush", ToolbarHoverBackground);
        MapDockBrush("DockChromeButtonPressedBackgroundBrush", ToolbarActiveBackground, ToolbarHoverBackground);
        MapDockBrush("DockChromeButtonDangerHoverBrush", ToolbarHoverBackground);

        if (TryGetValue(Foreground + "Color", out var foregroundValue) && foregroundValue is Color foreground)
        {
            var isLight = type == ThemeType.Light;
            this["DockTabPillBrush"] = new SolidColorBrush(foreground, isLight ? 0.04 : 0.05);
            this["DockTabPillHoverBrush"] = new SolidColorBrush(foreground, isLight ? 0.06 : 0.08);
            this["DockTabPillSelectedBrush"] = new SolidColorBrush(foreground, isLight ? 0.10 : 0.18);
            this["DockTabPillForegroundBrush"] = new SolidColorBrush(foreground, 0.5);
        }

        MapDockBrush("DockThemeForegroundBrush", Foreground);
        MapDockBrush("DockThemeBackgroundBrush", SideBarBackground);
        MapDockBrush("DockThemeBorderLowBrush", EditorGroupBorder, PanelBorder);
        MapDockBrush("DockThemeControlBackgroundBrush", EditorBackground);
        MapDockBrush("DockThemeAccentBrush", FocusBorder);
    }

    private void MapDockBrush(string dockKey, params string[] colorIds)
    {
        foreach (var id in colorIds)
        {
            if (TryGetValue(id, out var brush))
            {
                this[dockKey] = brush;
                return;
            }
        }
    }
}
