namespace RoslynPad.Themes;

internal class VsCodeColorRegistry : IColorRegistry
{
    private readonly Dictionary<string, ColorDefaults> _colors = [];

    public VsCodeColorRegistry()
    {
        var foreground = RegisterColor("foreground", new ColorDefaults(Dark: "#CCCCCC", Light: "#616161"));
        RegisterColor("disabledForeground", new ColorDefaults(Dark: "#CCCCCC80", Light: "#61616180"));
        var errorForeground = RegisterColor("errorForeground", new ColorDefaults(Dark: "#F48771", Light: "#A1260D"));
        RegisterColor("descriptionForeground", new ColorDefaults(Light: "#717171", Dark: Transparent(foreground, 0.7)));
        var iconForeground = RegisterColor("icon.foreground", new ColorDefaults(Dark: "#C5C5C5", Light: "#424242"));

        var focusBorder = RegisterColor("focusBorder", new ColorDefaults(Dark: "#007FD4", Light: "#0090F1"));
        var contrastBorder = RegisterColor("contrastBorder");

        RegisterColor("textSeparator.foreground", new ColorDefaults(Light: "#0000002e", Dark: "#ffffff2e"));
        RegisterColor("textLink.foreground", new ColorDefaults(Light: "#006AB1", Dark: "#3794FF"));
        RegisterColor("textLink.activeForeground", new ColorDefaults(Light: "#006AB1", Dark: "#3794FF"));
        RegisterColor("textPreformat.foreground", new ColorDefaults(Light: "#A31515", Dark: "#D7BA7D"));
        RegisterColor("textPreformat.background", new ColorDefaults(Light: "#0000001A", Dark: "#FFFFFF1A"));
        RegisterColor("textBlockQuote.background", new ColorDefaults(Light: "#f2f2f2", Dark: "#222222"));
        RegisterColor("textBlockQuote.border", new ColorDefaults(Light: "#007acc80", Dark: "#007acc80"));
        RegisterColor("textCodeBlock.background", new ColorDefaults(Light: "#dcdcdc66", Dark: "#0a0a0a66"));

        var widgetShadow = RegisterColor("widget.shadow", new ColorDefaults(Dark: Transparent("#000000", .36), Light: Transparent("#000000", .16)));

        RegisterColor("input.background", new ColorDefaults(Dark: "#3C3C3C", Light: "#ffffff"));
        RegisterColor("input.foreground", foreground);
        var inputBorder = RegisterColor("input.border");

        RegisterColor("inputOption.activeBorder", new ColorDefaults(Dark: "#007ACC", Light: "#007ACC"));
        RegisterColor("inputOption.hoverBackground", new ColorDefaults(Dark: "#5a5d5e80", Light: "#b8b8b850"));
        RegisterColor("inputOption.activeBackground", new ColorDefaults(Dark: Transparent(focusBorder, 0.4), Light: Transparent(focusBorder, 0.2)));
        RegisterColor("inputOption.activeForeground", new ColorDefaults(Dark: "#ffffff", Light: "#000000"));
        RegisterColor("input.placeholderForeground", new ColorDefaults(Light: Transparent(foreground, 0.5), Dark: Transparent(foreground, 0.5)));

        RegisterColor("inputValidation.infoBackground", new ColorDefaults(Dark: "#063B49", Light: "#D6ECF2"));
        RegisterColor("inputValidation.infoBorder", new ColorDefaults(Dark: "#007acc", Light: "#007acc"));
        RegisterColor("inputValidation.warningBackground", new ColorDefaults(Dark: "#352A05", Light: "#F6F5D2"));
        RegisterColor("inputValidation.warningBorder", new ColorDefaults(Dark: "#B89500", Light: "#B89500"));
        RegisterColor("inputValidation.errorBackground", new ColorDefaults(Dark: "#5A1D1D", Light: "#F2DEDE"));
        RegisterColor("inputValidation.errorBorder", new ColorDefaults(Dark: "#BE1100", Light: "#BE1100"));

        var selectBackground = RegisterColor("dropdown.background", new ColorDefaults(Dark: "#3C3C3C", Light: "#ffffff"));
        var selectForeground = RegisterColor("dropdown.foreground", new ColorDefaults(Dark: "#F0F0F0", Light: foreground));
        var selectBorder = RegisterColor("dropdown.border", new ColorDefaults(Dark: selectBackground, Light: "#CECECE"));

        var buttonForeground = RegisterColor("button.foreground", new ColorDefaults(Dark: "#ffffff", Light: "#ffffff"));
        RegisterColor("button.separator", new ColorDefaults(Dark: Transparent(buttonForeground, .4), Light: Transparent(buttonForeground, .4)));
        var buttonBackground = RegisterColor("button.background", new ColorDefaults(Dark: "#0E639C", Light: "#007ACC"));
        RegisterColor("button.hoverBackground", new ColorDefaults(Dark: Lighten(buttonBackground, 0.2), Light: Darken(buttonBackground, 0.2)));

        RegisterColor("button.secondaryForeground", new ColorDefaults(Dark: "#ffffff", Light: "#ffffff"));
        var buttonSecondaryBackground = RegisterColor("button.secondaryBackground", new ColorDefaults(Dark: "#3A3D41", Light: "#5F6A79"));
        RegisterColor("button.secondaryHoverBackground", new ColorDefaults(Dark: Lighten(buttonSecondaryBackground, 0.2), Light: Darken(buttonSecondaryBackground, 0.2)));

        RegisterColor("badge.background", new ColorDefaults(Dark: "#4D4D4D", Light: "#C4C4C4"));
        RegisterColor("badge.foreground", new ColorDefaults(Dark: "#ffffff", Light: "#333"));

        RegisterColor("scrollbar.shadow", new ColorDefaults(Dark: "#000000", Light: "#DDDDDD"));
        RegisterColor("scrollbarSlider.background", new ColorDefaults(Dark: Transparent("#797979", 0.4), Light: Transparent("#646464", 0.4)));
        RegisterColor("scrollbarSlider.hoverBackground", new ColorDefaults(Dark: Transparent("#646464", 0.7), Light: Transparent("#646464", 0.7)));
        RegisterColor("scrollbarSlider.activeBackground", new ColorDefaults(Dark: Transparent("#BFBFBF", 0.4), Light: Transparent("#000000", 0.6)));

        RegisterColor("progressBar.background", new ColorDefaults(Dark: "#0E70C0", Light: "#0E70C0"));
        RegisterColor("editorError.foreground", new ColorDefaults(Dark: "#F14C4C", Light: "#E51400"));
        var editorWarningForeground = RegisterColor("editorWarning.foreground", new ColorDefaults(Dark: "#CCA700", Light: "#BF8803"));
        RegisterColor("editorInfo.foreground", new ColorDefaults(Dark: "#3794FF", Light: "#1a85ff"));
        RegisterColor("editorHint.foreground", new ColorDefaults(Dark: Transparent("#eeeeee", 0.7), Light: "#6c6c6c"));
        var editorBackground = RegisterColor("editor.background", new ColorDefaults(Light: "#ffffff", Dark: "#1E1E1E"));
        RegisterColor("editor.foreground", new ColorDefaults(Light: "#333333", Dark: "#BBBBBB"));

        var editorWidgetBackground = RegisterColor("editorWidget.background", new ColorDefaults(Dark: "#252526", Light: "#F3F3F3"));
        var editorWidgetForeground = RegisterColor("editorWidget.foreground", new ColorDefaults(Dark: foreground, Light: foreground));
        RegisterColor("editorWidget.border", new ColorDefaults(Dark: "#454545", Light: "#C8C8C8"));

        RegisterColor("quickInput.background", editorWidgetBackground);
        RegisterColor("quickInput.foreground", editorWidgetForeground);
        RegisterColor("quickInputTitle.background", new ColorDefaults(Dark: Rgba(255, 255, 255, 0.105), Light: Rgba(0, 0, 0, 0.06)));
        RegisterColor("pickerGroup.foreground", new ColorDefaults(Dark: "#3794FF", Light: "#0066BF"));
        RegisterColor("pickerGroup.border", new ColorDefaults(Dark: "#3F3F46", Light: "#CCCEDB"));

        RegisterColor("keybindingLabel.background", new ColorDefaults(Dark: Rgba(128, 128, 128, 0.17), Light: Rgba(221, 221, 221, 0.4)));
        RegisterColor("keybindingLabel.foreground", new ColorDefaults(Dark: "#CCCCCC", Light: "#555555"));
        RegisterColor("keybindingLabel.border", new ColorDefaults(Dark: Rgba(51, 51, 51, 0.6), Light: Rgba(204, 204, 204, 0.4)));
        RegisterColor("keybindingLabel.bottomBorder", new ColorDefaults(Dark: Rgba(68, 68, 68, 0.6), Light: Rgba(187, 187, 187, 0.4)));

        var editorSelectionBackground = RegisterColor("editor.selectionBackground", new ColorDefaults(Light: "#ADD6FF", Dark: "#264F78"));
        RegisterColor("editor.inactiveSelectionBackground", new ColorDefaults(Light: Transparent(editorSelectionBackground, 0.5), Dark: Transparent(editorSelectionBackground, 0.5)));
        RegisterColor("editor.selectionHighlightBackground", new ColorDefaults(Light: Lighten(editorSelectionBackground, 0.3), Dark: Lighten(editorSelectionBackground, 0.3)));

        RegisterColor("editor.findMatchBackground", new ColorDefaults(Light: "#A8AC94", Dark: "#515C6A"));
        var editorFindMatchHighlight = RegisterColor("editor.findMatchHighlightBackground", new ColorDefaults(Light: "#EA5C0055", Dark: "#EA5C0055"));
        RegisterColor("editor.findRangeHighlightBackground", new ColorDefaults(Dark: "#3a3d4166", Light: "#b4b4b44d"));

        RegisterColor("editorLink.activeForeground", new ColorDefaults(Dark: "#4E94CE", Light: "#0000ff"));
        RegisterColor("editorLightBulb.foreground", new ColorDefaults(Dark: "#FFCC00", Light: "#DDB100"));

        RegisterColor("list.focusOutline", focusBorder);
        var listActiveSelectionBackground = RegisterColor("list.activeSelectionBackground", new ColorDefaults(Dark: "#04395E", Light: "#0060C0"));
        var listActiveSelectionForeground = RegisterColor("list.activeSelectionForeground", new ColorDefaults(Dark: "#ffffff", Light: "#ffffff"));
        RegisterColor("list.inactiveSelectionBackground", new ColorDefaults(Dark: "#37373D", Light: "#E4E6F1"));
        RegisterColor("list.hoverBackground", new ColorDefaults(Dark: "#2A2D2E", Light: "#F0F0F0"));
        RegisterColor("list.dropBackground", new ColorDefaults(Dark: "#062F4A", Light: "#D6EBFF"));
        RegisterColor("list.dropBetweenBackground", iconForeground);
        var listHighlightForeground = RegisterColor("list.highlightForeground", new ColorDefaults(Dark: "#2AAAFF", Light: "#0066BF"));
        RegisterColor("list.focusHighlightForeground", listHighlightForeground);
        RegisterColor("list.invalidItemForeground", new ColorDefaults(Dark: "#B89500", Light: "#B89500"));
        RegisterColor("list.errorForeground", new ColorDefaults(Dark: "#F88070", Light: "#B01011"));
        RegisterColor("list.warningForeground", new ColorDefaults(Dark: "#CCA700", Light: "#855F00"));
        RegisterColor("listFilterWidget.background", new ColorDefaults(Light: Darken(editorWidgetBackground, 0), Dark: Lighten(editorWidgetBackground, 0)));
        RegisterColor("listFilterWidget.noMatchesOutline", new ColorDefaults(Dark: "#BE1100", Light: "#BE1100"));
        RegisterColor("listFilterWidget.shadow", widgetShadow);
        RegisterColor("list.filterMatchBackground", editorFindMatchHighlight);
        var treeIndentGuidesStroke = RegisterColor("tree.indentGuidesStroke", new ColorDefaults(Dark: "#585858", Light: "#a9a9a9"));
        RegisterColor("tree.inactiveIndentGuidesStroke", new ColorDefaults(Dark: Transparent(treeIndentGuidesStroke, 0.4), Light: Transparent(treeIndentGuidesStroke, 0.4)));
        RegisterColor("tree.tableColumnsBorder", new ColorDefaults(Dark: "#CCCCCC20", Light: "#61616120"));
        RegisterColor("tree.tableOddRowsBackground", new ColorDefaults(Dark: Transparent(foreground, 0.04), Light: Transparent(foreground, 0.04)));
        RegisterColor("list.deemphasizedForeground", new ColorDefaults(Dark: "#8C8C8C", Light: "#8E8E90"));

        RegisterColor("checkbox.background", selectBackground);
        RegisterColor("checkbox.selectBackground", editorWidgetBackground);
        RegisterColor("checkbox.foreground", selectForeground);
        RegisterColor("checkbox.border", selectBorder);
        RegisterColor("checkbox.selectBorder", iconForeground);

        RegisterColor("menu.foreground", selectForeground);
        RegisterColor("menu.background", selectBackground);
        RegisterColor("menu.selectionForeground", listActiveSelectionForeground);
        RegisterColor("menu.selectionBackground", listActiveSelectionBackground);
        RegisterColor("menu.separatorBackground", new ColorDefaults(Dark: "#606060", Light: "#D4D4D4"));

        var toolbarHoverBackground = RegisterColor("toolbar.hoverBackground", new ColorDefaults(Dark: "#5a5d5e50", Light: "#b8b8b850"));
        RegisterColor("toolbar.activeBackground", new ColorDefaults(Dark: Lighten(toolbarHoverBackground, 0.1), Light: Darken(toolbarHoverBackground, 0.1)));

        RegisterColor("breadcrumb.foreground", new ColorDefaults(Light: Transparent(foreground, 0.8), Dark: Transparent(foreground, 0.8)));
        RegisterColor("breadcrumb.background", editorBackground);
        RegisterColor("breadcrumb.focusForeground", new ColorDefaults(Light: Darken(foreground, 0.2), Dark: Lighten(foreground, 0.1)));
        RegisterColor("breadcrumb.activeSelectionForeground", new ColorDefaults(Light: Darken(foreground, 0.2), Dark: Lighten(foreground, 0.1)));
        RegisterColor("breadcrumbPicker.background", editorWidgetBackground);

        var tabActiveBackground = RegisterColor("tab.activeBackground", editorBackground);
        RegisterColor("tab.unfocusedActiveBackground", new ColorDefaults(Dark: tabActiveBackground, Light: tabActiveBackground));

        var tabInactiveBackground = RegisterColor("tab.inactiveBackground", new ColorDefaults(Dark: "#2D2D2D", Light: "#ECECEC"));
        RegisterColor("tab.unfocusedInactiveBackground", new ColorDefaults(Dark: tabInactiveBackground, Light: tabInactiveBackground));

        var tabActiveForeground = RegisterColor("tab.activeForeground", new ColorDefaults(Dark: "#ffffff", Light: "#333333"));

        var tabInactiveForeground = RegisterColor("tab.inactiveForeground", new ColorDefaults(Dark: Transparent(tabActiveForeground, 0.5), Light: Transparent(tabActiveForeground, 0.7)));
        RegisterColor("tab.unfocusedActiveForeground", new ColorDefaults(Dark: Transparent(tabActiveForeground, 0.5), Light: Transparent(tabActiveForeground, 0.7)));
        RegisterColor("tab.unfocusedInactiveForeground", new ColorDefaults(Dark: Transparent(tabInactiveForeground, 0.5), Light: Transparent(tabInactiveForeground, 0.5)));

        var tabHoverBackground = RegisterColor("tab.hoverBackground");
        var tabHoverForeground = RegisterColor("tab.hoverForeground");
        var tabActiveBorder = RegisterColor("tab.activeBorder");
        var tabActiveBorderTop = RegisterColor("tab.activeBorderTop");
        var tabHoverBorder = RegisterColor("tab.hoverBorder");

        RegisterColor("tab.unfocusedHoverBackground", new ColorDefaults(Dark: Transparent(tabHoverBackground, 0.5), Light: Transparent(tabHoverBackground, 0.7)));
        RegisterColor("tab.unfocusedHoverForeground", new ColorDefaults(Dark: Transparent(tabHoverForeground, 0.5), Light: Transparent(tabHoverForeground, 0.5)));
        RegisterColor("tab.border", new ColorDefaults(Dark: "#252526", Light: "#F3F3F3"));
        RegisterColor("tab.lastPinnedBorder", treeIndentGuidesStroke);
        RegisterColor("tab.unfocusedActiveBorder", new ColorDefaults(Dark: Transparent(tabActiveBorder, 0.5), Light: Transparent(tabActiveBorder, 0.7)));
        RegisterColor("tab.unfocusedActiveBorderTop", new ColorDefaults(Dark: Transparent(tabActiveBorderTop, 0.5), Light: Transparent(tabActiveBorderTop, 0.7)));
        RegisterColor("tab.unfocusedHoverBorder", new ColorDefaults(Dark: Transparent(tabHoverBorder, 0.5), Light: Transparent(tabHoverBorder, 0.7)));
        RegisterColor("tab.dragAndDropBorder", new ColorDefaults(Dark: tabActiveForeground, Light: tabActiveForeground));

        var tabActiveModifiedBorder = RegisterColor("tab.activeModifiedBorder", new ColorDefaults(Dark: "#3399CC", Light: "#33AAEE"));
        var editorGroupDropBackground = RegisterColor("editorGroup.dropBackground", new ColorDefaults(Dark: Color.FromHex("#53595D").Transparent(0.5), Light: Color.FromHex("#2677CB").Transparent(0.18)));

        var tabInactiveModifiedBorder = RegisterColor("tab.inactiveModifiedBorder", new ColorDefaults(Dark: Transparent(tabActiveModifiedBorder, 0.5), Light: Transparent(tabActiveModifiedBorder, 0.5)));
        RegisterColor("tab.unfocusedActiveModifiedBorder", new ColorDefaults(Dark: Transparent(tabActiveModifiedBorder, 0.5), Light: Transparent(tabActiveModifiedBorder, 0.7)));
        RegisterColor("tab.unfocusedInactiveModifiedBorder", new ColorDefaults(Dark: Transparent(tabInactiveModifiedBorder, 0.5), Light: Transparent(tabInactiveModifiedBorder, 0.5)));
        RegisterColor("panel.background", editorBackground);

        var panelBorder = RegisterColor("panel.border", new ColorDefaults(Dark: Color.FromHex("#808080").Transparent(0.35), Light: Color.FromHex("#808080").Transparent(0.35)));

        var panelTitleActiveForeground = RegisterColor("panelTitle.activeForeground", new ColorDefaults(Dark: "#E7E7E7", Light: "#424242"));
        RegisterColor("panelTitle.inactiveForeground", new ColorDefaults(Dark: Transparent(panelTitleActiveForeground, 0.6), Light: Transparent(panelTitleActiveForeground, 0.75)));
        RegisterColor("panelTitle.activeBorder", panelTitleActiveForeground);
        RegisterColor("panelInput.border", new ColorDefaults(Dark: inputBorder, Light: Color.FromHex("#ddd")));
        RegisterColor("panel.dropBorder", panelTitleActiveForeground);
        RegisterColor("panelSection.dropBackground", new ColorDefaults(Dark: editorGroupDropBackground, Light: editorGroupDropBackground));
        RegisterColor("panelSectionHeader.background", new ColorDefaults(Dark: Color.FromHex("#808080").Transparent(0.2), Light: Color.FromHex("#808080").Transparent(0.2)));
        RegisterColor("panelSectionHeader.border", new ColorDefaults(Dark: contrastBorder, Light: contrastBorder));
        RegisterColor("panelSection.border", new ColorDefaults(Dark: panelBorder, Light: panelBorder));

        var outputViewBackground = RegisterColor("outputView.background");
        var statusBarBorder = RegisterColor("statusBar.border");
        RegisterColor("outputViewStickyScroll.background", new ColorDefaults(Dark: outputViewBackground, Light: outputViewBackground));

        var statusBarForeground = RegisterColor("statusBar.foreground", new ColorDefaults(Dark: "#FFFFFF", Light: "#FFFFFF"));
        RegisterColor("statusBar.noFolderForeground", statusBarForeground);
        RegisterColor("statusBar.background", new ColorDefaults(Dark: "#007ACC", Light: "#007ACC"));
        RegisterColor("statusBar.noFolderBackground", new ColorDefaults(Dark: "#68217A", Light: "#68217A"));
        RegisterColor("statusBar.focusBorder", statusBarForeground);
        RegisterColor("statusBar.noFolderBorder", new ColorDefaults(Dark: statusBarBorder, Light: statusBarBorder));
        RegisterColor("statusBarItem.activeBackground", new ColorDefaults(Dark: Color.White.Transparent(0.18), Light: Color.White.Transparent(0.18)));
        RegisterColor("statusBarItem.focusBorder", statusBarForeground);

        var statusBarItemHoverBackground = RegisterColor("statusBarItem.hoverBackground", new ColorDefaults(Dark: Color.White.Transparent(0.12), Light: Color.White.Transparent(0.12)));

        var statusBarItemHoverForeground = RegisterColor("statusBarItem.hoverForeground", statusBarForeground);
        RegisterColor("statusBarItem.compactHoverBackground", new ColorDefaults(Dark: Color.White.Transparent(0.20), Light: Color.White.Transparent(0.20)));
        RegisterColor("statusBarItem.prominentForeground", statusBarForeground);
        RegisterColor("statusBarItem.prominentBackground", new ColorDefaults(Dark: Color.Black.Transparent(0.5), Light: Color.Black.Transparent(0.5)));
        RegisterColor("statusBarItem.prominentHoverForeground", statusBarItemHoverForeground);
        RegisterColor("statusBarItem.prominentHoverBackground", new ColorDefaults(Dark: Color.Black.Transparent(0.3), Light: Color.Black.Transparent(0.3)));
        RegisterColor("statusBarItem.errorBackground", new ColorDefaults(Dark: Darken(errorForeground, .4), Light: Darken(errorForeground, .4)));
        RegisterColor("statusBarItem.errorForeground", new ColorDefaults(Dark: Color.White, Light: Color.White));
        RegisterColor("statusBarItem.errorHoverForeground", statusBarItemHoverForeground);
        RegisterColor("statusBarItem.errorHoverBackground", statusBarItemHoverBackground);
        RegisterColor("statusBarItem.warningBackground", new ColorDefaults(Dark: Darken(editorWarningForeground, .4), Light: Darken(editorWarningForeground, .4)));
        RegisterColor("statusBarItem.warningForeground", new ColorDefaults(Dark: Color.White, Light: Color.White));
        RegisterColor("statusBarItem.warningHoverForeground", statusBarItemHoverForeground);
        RegisterColor("statusBarItem.warningHoverBackground", statusBarItemHoverBackground);
        RegisterColor("activityBar.background", new ColorDefaults(Dark: "#333333", Light: "#2C2C2C"));

        var activityBarForeground = RegisterColor("activityBar.foreground", new ColorDefaults(Dark: Color.White, Light: Color.White));
        RegisterColor("activityBar.inactiveForeground", new ColorDefaults(Dark: Transparent(activityBarForeground, 0.4), Light: Transparent(activityBarForeground, 0.4)));
        RegisterColor("activityBar.activeBorder", activityBarForeground);
        RegisterColor("activityBar.dropBorder", activityBarForeground);
        RegisterColor("activityBarBadge.background", new ColorDefaults(Dark: "#007ACC", Light: "#007ACC"));
        RegisterColor("activityBarBadge.foreground", new ColorDefaults(Dark: Color.White, Light: Color.White));

        var sideBarForeground = RegisterColor("sideBar.foreground");

        var activityBarTopForeground = RegisterColor("activityBarTop.foreground", new ColorDefaults(Dark: "#E7E7E7", Light: "#424242"));
        RegisterColor("activityBarTop.activeBorder", new ColorDefaults(Dark: activityBarTopForeground, Light: activityBarTopForeground));
        RegisterColor("activityBarTop.inactiveForeground", new ColorDefaults(Dark: Transparent(activityBarTopForeground, 0.6), Light: Transparent(activityBarTopForeground, 0.75)));
        RegisterColor("activityBarTop.dropBorder", new ColorDefaults(Dark: activityBarTopForeground, Light: activityBarTopForeground));
        RegisterColor("sideBar.background", new ColorDefaults(Dark: "#252526", Light: "#F3F3F3"));
        RegisterColor("sideBarTitle.foreground", sideBarForeground);
        RegisterColor("sideBar.dropBackground", new ColorDefaults(Dark: editorGroupDropBackground, Light: editorGroupDropBackground));
        RegisterColor("sideBarSectionHeader.background", new ColorDefaults(Dark: Color.FromHex("#808080").Transparent(0.2), Light: Color.FromHex("#808080").Transparent(0.2)));
        RegisterColor("sideBarSectionHeader.foreground", sideBarForeground);
        RegisterColor("sideBarSectionHeader.border", new ColorDefaults(Dark: contrastBorder, Light: contrastBorder));

        var titleBarActiveForeground = RegisterColor("titleBar.activeForeground", new ColorDefaults(Dark: "#CCCCCC", Light: "#333333"));
        RegisterColor("titleBar.inactiveForeground", new ColorDefaults(Dark: Transparent(titleBarActiveForeground, 0.6), Light: Transparent(titleBarActiveForeground, 0.6)));

        var titleBarActiveBackground = RegisterColor("titleBar.activeBackground", new ColorDefaults(Dark: "#3C3C3C", Light: "#DDDDDD"));
        RegisterColor("titleBar.inactiveBackground", new ColorDefaults(Dark: Transparent(titleBarActiveBackground, 0.6), Light: Transparent(titleBarActiveBackground, 0.6)));
        RegisterColor("menubar.selectionForeground", titleBarActiveForeground);
        RegisterColor("menubar.selectionBackground", toolbarHoverBackground);
    }

    private string RegisterColor(string name) => name;

    private string RegisterColor(string name, string reference) => RegisterColor(name, new ColorDefaults(reference, reference));

    private string RegisterColor(string name, ColorDefaults colors)
    {
        _colors.Add(name, colors);
        return name;
    }

    private static ColorValue Transparent(ColorValue value, double alpha) => new ColorTransform(ColorTransformOp.Transparent, value, alpha);
    private static ColorValue Lighten(ColorValue value, double factor) => new ColorTransform(ColorTransformOp.Lighten, value, factor);
    private static ColorValue Darken(ColorValue value, double factor) => new ColorTransform(ColorTransformOp.Darken, value, factor);
    private static ColorValue Rgba(int r, int g, int b, double a) => new(Color: new Color(new Color.RGBA(r, g, b, a)));

    public string? ResolveDefaultColor(string id, Theme theme)
    {
        if (_colors.TryGetValue(id, out var defaults))
        {
            var colorValue = theme.Type == ThemeType.Dark ? defaults.Dark : defaults.Light;
            return ResolveColorValue(colorValue, theme).ToString();
        }

        return null;
    }

    private static Color? ResolveColorValue(ColorValue? colorValue, Theme theme)
    {
        if (colorValue is null)
        {
            return null;
        }

        if (colorValue.Id is { Length: > 1 } id)
        {
            if (id[0] == '#')
            {
                return Color.FromHex(id);
            }

            if (theme.TryGetColor(id) is { } color)
            {
                return Color.FromHex(color);
            }

            return null;
        }
        else if (colorValue.Color is { } color)
        {
            return color;
        }
        else if (colorValue.Transform is { } transform)
        {
            return ExecuteTransform(transform, theme);
        }

        return null;
    }

    private static Color? ExecuteTransform(ColorTransform transform, Theme theme) => transform.Op switch
    {
        ColorTransformOp.Darken => ResolveColorValue(transform.Value, theme)?.Darken(transform.Factor),
        ColorTransformOp.Lighten => ResolveColorValue(transform.Value, theme)?.Lighten(transform.Factor),
        ColorTransformOp.Transparent => ResolveColorValue(transform.Value, theme)?.Transparent(transform.Factor),
        _ => throw new ArgumentOutOfRangeException(nameof(transform)),
    };


    private record ColorDefaults(ColorValue Light, ColorValue Dark);

    private record ColorValue(string? Id = null, Color? Color = null, ColorTransform? Transform = null)
    {
        public static implicit operator ColorValue(string id) => new(Id: id);
        public static implicit operator ColorValue(Color color) => new(Color: color);
        public static implicit operator ColorValue(ColorTransform transform) => new(Transform: transform);
    }

    private record ColorTransform(ColorTransformOp Op, ColorValue Value, double Factor);

    private enum ColorTransformOp
    {
        Transparent,
        Lighten,
        Darken,
    }
}
