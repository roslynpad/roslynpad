#nullable enable

namespace Microsoft.VisualStudio.Language.Intellisense;

using Avalonia;
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

    /// <summary>The room a popup keeps between its edge and its content, as a <see cref="Thickness"/>.</summary>
    public const string Padding = "Padding";

    /// <summary>How round a popup's corners are, as a <see cref="Avalonia.CornerRadius"/>.</summary>
    public const string CornerRadius = "CornerRadius";

    /// <summary>What a popup's text is set in, as a <see cref="Avalonia.Media.FontFamily"/>.</summary>
    public const string FontFamily = "FontFamily";

    /// <summary>How big a popup's text is, as a <see cref="double"/>.</summary>
    public const string FontSize = "FontSize";
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
    /// <summary>The room between a popup's edge and its content; the built-in default where a host set none.</summary>
    public Thickness Padding { get; init; } = new(8.0, 5.0);

    /// <summary>How round a popup's corners are; the built-in default where a host set none.</summary>
    public CornerRadius CornerRadius { get; init; } = new(3.0);

    /// <summary>
    /// What a popup's text is set in, where a host says so. It belongs on the popup ITSELF and not on the runs
    /// inside it: a line box is measured from the text element's own size, so a smaller run in a container left
    /// at the editor's size is laid out in the taller line and sits low in it.
    /// </summary>
    public FontFamily? FontFamily { get; init; }

    /// <summary>How big a popup's text is, where a host says so; see <see cref="FontFamily"/>.</summary>
    public double? FontSize { get; init; }

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
            Get(PopupFormatNames.DeemphasizedForeground, s_defaults.DeemphasizedForeground))
        {
            Padding = properties.TryGetValue(PopupFormatNames.Padding, out var room) && room is Thickness padding
                ? padding
                : new Thickness(8.0, 5.0),
            CornerRadius = properties.TryGetValue(PopupFormatNames.CornerRadius, out var round) && round is CornerRadius radius
                ? radius
                : new CornerRadius(3.0),
            FontFamily = properties.TryGetValue(PopupFormatNames.FontFamily, out var named) ? named as FontFamily : null,
            FontSize = properties.TryGetValue(PopupFormatNames.FontSize, out var sized) && sized is double size ? size : null,
        };

        IBrush Get(string key, IBrush fallback)
            => properties.TryGetValue(key, out var value) && value is IBrush brush ? brush : fallback;
    }
}
