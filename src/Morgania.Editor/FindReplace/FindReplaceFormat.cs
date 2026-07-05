#nullable enable

namespace Microsoft.VisualStudio.Text.Editor;

using Avalonia.Media;

using Microsoft.VisualStudio.Text.Classification;

/// <summary>
/// The editor-format-map key and property names the find/replace panel reads its colors
/// from. Hosts theme the panel by setting these properties (as <see cref="IBrush"/> values)
/// on the map returned by <see cref="IEditorFormatMapService"/>; properties left unset fall
/// back to the built-in dark palette.
/// </summary>
public static class FindReplaceFormatNames
{
    /// <summary>The editor format map key.</summary>
    public const string Name = "Find Replace";

    public const string Background = "Background";
    public const string Foreground = "Foreground";
    public const string BorderBrush = "BorderBrush";
    public const string InputBackground = "InputBackground";
    public const string InputForeground = "InputForeground";
    public const string InputBorder = "InputBorder";
    public const string MatchBackground = "MatchBackground";
    public const string CurrentMatchBackground = "CurrentMatchBackground";
    public const string NoMatchForeground = "NoMatchForeground";
}

/// <summary>
/// The resolved find/replace palette: the host-set <see cref="FindReplaceFormatNames"/>
/// properties overlaid on the built-in dark defaults.
/// </summary>
internal sealed record FindReplaceBrushes(
    IBrush Background,
    IBrush Foreground,
    IBrush BorderBrush,
    IBrush InputBackground,
    IBrush InputForeground,
    IBrush InputBorder,
    IBrush MatchBackground,
    IBrush CurrentMatchBackground,
    IBrush NoMatchForeground)
{
    private static readonly FindReplaceBrushes s_defaults = new(
        Background: new SolidColorBrush(Color.FromRgb(0x25, 0x25, 0x26)),
        Foreground: new SolidColorBrush(Color.FromRgb(0xCC, 0xCC, 0xCC)),
        BorderBrush: new SolidColorBrush(Color.FromRgb(0x45, 0x45, 0x45)),
        InputBackground: new SolidColorBrush(Color.FromRgb(0x3C, 0x3C, 0x3C)),
        InputForeground: new SolidColorBrush(Color.FromRgb(0xCC, 0xCC, 0xCC)),
        InputBorder: new SolidColorBrush(Color.FromRgb(0x3C, 0x3C, 0x3C)),
        MatchBackground: new SolidColorBrush(Color.FromArgb(0x55, 0xEA, 0x5C, 0x00)),
        CurrentMatchBackground: new SolidColorBrush(Color.FromRgb(0x51, 0x5C, 0x6A)),
        NoMatchForeground: new SolidColorBrush(Color.FromRgb(0xF4, 0x87, 0x71)));

    public static FindReplaceBrushes Read(IEditorFormatMap formatMap)
    {
        var properties = formatMap.GetProperties(FindReplaceFormatNames.Name);
        return new FindReplaceBrushes(
            Get(FindReplaceFormatNames.Background, s_defaults.Background),
            Get(FindReplaceFormatNames.Foreground, s_defaults.Foreground),
            Get(FindReplaceFormatNames.BorderBrush, s_defaults.BorderBrush),
            Get(FindReplaceFormatNames.InputBackground, s_defaults.InputBackground),
            Get(FindReplaceFormatNames.InputForeground, s_defaults.InputForeground),
            Get(FindReplaceFormatNames.InputBorder, s_defaults.InputBorder),
            Get(FindReplaceFormatNames.MatchBackground, s_defaults.MatchBackground),
            Get(FindReplaceFormatNames.CurrentMatchBackground, s_defaults.CurrentMatchBackground),
            Get(FindReplaceFormatNames.NoMatchForeground, s_defaults.NoMatchForeground));

        IBrush Get(string key, IBrush fallback)
            => properties.TryGetValue(key, out var value) && value is IBrush brush ? brush : fallback;
    }
}
