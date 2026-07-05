#nullable enable

namespace Microsoft.VisualStudio.Language.Intellisense;

using Avalonia.Media;

using Microsoft.VisualStudio.Text.Classification;

/// <summary>
/// The editor-format-map key and property names the default intellisense popup presenters
/// (quick info tooltips, signature help, completion) read their colors from. Hosts theme the
/// popups by setting these properties (as <see cref="IBrush"/> values) on the map returned by
/// <see cref="IEditorFormatMapService"/>; properties left unset fall back to the built-in
/// dark palette.
/// </summary>
public static class PopupFormatNames
{
    /// <summary>The editor format map key.</summary>
    public const string Name = "Intellisense Popup";

    public const string Background = "Background";
    public const string Foreground = "Foreground";
    public const string BorderBrush = "BorderBrush";
    public const string SelectionBackground = "SelectionBackground";
    public const string SelectionForeground = "SelectionForeground";
    public const string SoftSelectionBorder = "SoftSelectionBorder";
    public const string MatchForeground = "MatchForeground";
    public const string DeemphasizedForeground = "DeemphasizedForeground";
}

/// <summary>
/// The resolved popup palette: the host-set <see cref="PopupFormatNames"/> properties overlaid
/// on the built-in dark defaults.
/// </summary>
internal sealed record PopupBrushes(
    IBrush Background,
    IBrush Foreground,
    IBrush BorderBrush,
    IBrush SelectionBackground,
    IBrush SelectionForeground,
    IBrush SoftSelectionBorder,
    IBrush MatchForeground,
    IBrush DeemphasizedForeground)
{
    private static readonly PopupBrushes s_defaults = new(
        Background: new SolidColorBrush(Color.FromRgb(0x25, 0x25, 0x26)),
        Foreground: new SolidColorBrush(Color.FromRgb(0xD4, 0xD4, 0xD4)),
        BorderBrush: new SolidColorBrush(Color.FromRgb(0x45, 0x45, 0x48)),
        SelectionBackground: new SolidColorBrush(Color.FromRgb(0x04, 0x39, 0x5E)),
        SelectionForeground: new SolidColorBrush(Color.FromRgb(0xFF, 0xFF, 0xFF)),
        SoftSelectionBorder: new SolidColorBrush(Color.FromRgb(0x00, 0x7A, 0xCC)),
        MatchForeground: new SolidColorBrush(Color.FromRgb(0x56, 0x9C, 0xD6)),
        DeemphasizedForeground: new SolidColorBrush(Color.FromRgb(0x9C, 0x9C, 0x9C)));

    public static PopupBrushes Read(IEditorFormatMap formatMap)
    {
        var properties = formatMap.GetProperties(PopupFormatNames.Name);
        return new PopupBrushes(
            Get(PopupFormatNames.Background, s_defaults.Background),
            Get(PopupFormatNames.Foreground, s_defaults.Foreground),
            Get(PopupFormatNames.BorderBrush, s_defaults.BorderBrush),
            Get(PopupFormatNames.SelectionBackground, s_defaults.SelectionBackground),
            Get(PopupFormatNames.SelectionForeground, s_defaults.SelectionForeground),
            Get(PopupFormatNames.SoftSelectionBorder, s_defaults.SoftSelectionBorder),
            Get(PopupFormatNames.MatchForeground, s_defaults.MatchForeground),
            Get(PopupFormatNames.DeemphasizedForeground, s_defaults.DeemphasizedForeground));

        IBrush Get(string key, IBrush fallback)
            => properties.TryGetValue(key, out var value) && value is IBrush brush ? brush : fallback;
    }
}
