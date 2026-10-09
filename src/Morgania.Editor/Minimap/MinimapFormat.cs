#nullable enable

namespace Microsoft.VisualStudio.Text.Editor;

using System.Globalization;

using Avalonia.Media;

using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor.Implementation;

/// <summary>
/// The editor-format-map key and property names the minimap reads its colours from. A host themes the minimap by setting
/// these properties (as <see cref="IBrush"/> values) on the map <see cref="IEditorFormatMapService"/> returns for a view;
/// a property left unset takes a built-in default that reads on dark and on light grounds. Errors take the colour their
/// own error type has in the map (<see cref="EditorFormatDefinition.ForegroundBrushId"/>, as the squiggles do), text
/// markers their marker's fill. <see cref="MinimapOptions.ViewportColorId"/> and
/// <see cref="MinimapOptions.ViewportBorderColorId"/> override the viewport's two.
/// </summary>
public static class MinimapFormatNames
{
    /// <summary>The editor format map key.</summary>
    public const string Name = "Minimap";

    /// <summary>The ground under the picture; transparent by default, so the minimap stands on what the editor stands on.</summary>
    public const string Background = "Background";

    /// <summary>The fill of the band that shows the viewport.</summary>
    public const string Viewport = "Viewport";

    /// <summary>The band's fill while the pointer is on it.</summary>
    public const string ViewportHover = "ViewportHover";

    /// <summary>The band's fill while it is dragged.</summary>
    public const string ViewportActive = "ViewportActive";

    /// <summary>The band's border, drawn when <see cref="MinimapOptions.ViewportBorderThicknessId"/> is above 0.</summary>
    public const string ViewportBorder = "ViewportBorder";

    /// <summary>A mark for a match of the find panel.</summary>
    public const string FindMatch = "FindMatch";

    /// <summary>A mark for the selection.</summary>
    public const string Selection = "Selection";

    /// <summary>A mark for a line added since the reference text (<see cref="TextChangeBaseline"/>); green by default.</summary>
    public const string Added = "Added";

    /// <summary>A mark for a line changed since the reference text; blue by default.</summary>
    public const string Modified = "Modified";

    /// <summary>A marker's label; by default the colour of the line's first character.</summary>
    public const string Marker = "Marker";
}

/// <summary>
/// The minimap's resolved palette: the host-set <see cref="MinimapFormatNames"/> properties and the viewport colour
/// options overlaid on the built-in defaults.
/// </summary>
internal sealed record MinimapPalette(
    IBrush? Background,
    IBrush Viewport,
    IBrush ViewportHover,
    IBrush ViewportActive,
    IBrush ViewportBorder,
    IBrush FindMatch,
    IBrush Selection,
    IBrush Added,
    IBrush Modified,
    IBrush? Marker)
{
    private static readonly IBrush s_viewport = new SolidColorBrush(Color.FromArgb(0x40, 0xA0, 0xA0, 0xA0));
    private static readonly IBrush s_viewportHover = new SolidColorBrush(Color.FromArgb(0x58, 0xA0, 0xA0, 0xA0));
    private static readonly IBrush s_viewportActive = new SolidColorBrush(Color.FromArgb(0x70, 0xA0, 0xA0, 0xA0));
    private static readonly IBrush s_viewportBorder = new SolidColorBrush(Color.FromRgb(0x00, 0xFF, 0x00));
    private static readonly IBrush s_findMatch = new SolidColorBrush(Color.FromArgb(0xC0, 0xEA, 0x5C, 0x00));
    private static readonly IBrush s_selection = new SolidColorBrush(Color.FromArgb(0x90, 0x26, 0x4F, 0x78));
    private static readonly IBrush s_added = new SolidColorBrush(Color.FromRgb(0x2E, 0xA0, 0x43));
    private static readonly IBrush s_modified = new SolidColorBrush(Color.FromRgb(0x1B, 0x81, 0xA8));

    private static readonly Dictionary<string, Color> s_errorColors = new(StringComparer.OrdinalIgnoreCase)
    {
        [PredefinedErrorTypeNames.SyntaxError] = Color.FromRgb(0xF1, 0x4C, 0x4C),
        [PredefinedErrorTypeNames.CompilerError] = Color.FromRgb(0xF1, 0x4C, 0x4C),
        [PredefinedErrorTypeNames.OtherError] = Color.FromRgb(0xF1, 0x4C, 0x4C),
        [PredefinedErrorTypeNames.Warning] = Color.FromRgb(0xCC, 0xA7, 0x00),
        [PredefinedErrorTypeNames.Suggestion] = Color.FromRgb(0x75, 0xBE, 0xFF),
        [PredefinedErrorTypeNames.HintedSuggestion] = Color.FromRgb(0xB8, 0xB8, 0xB8),
    };

    public static MinimapPalette Read(IEditorFormatMap formatMap, IEditorOptions options)
    {
        var properties = formatMap.GetProperties(MinimapFormatNames.Name);
        IBrush? Get(string name) => properties.TryGetValue(name, out var value) ? value as IBrush : null;

        // A colour option overrides the map's viewport: the fill with its hover and drag shades, the border on its own.
        IBrush viewport = Get(MinimapFormatNames.Viewport) ?? s_viewport;
        IBrush viewportHover = Get(MinimapFormatNames.ViewportHover) ?? s_viewportHover;
        IBrush viewportActive = Get(MinimapFormatNames.ViewportActive) ?? s_viewportActive;
        if (ParseColor(options.GetOptionValue(MinimapOptions.ViewportColorId), translucentAlpha: 0x40) is { } fill)
        {
            viewport = new SolidColorBrush(fill);
            viewportHover = new SolidColorBrush(WithAlpha(fill, fill.A + 0x18));
            viewportActive = new SolidColorBrush(WithAlpha(fill, fill.A + 0x30));
        }

        IBrush viewportBorder = ParseColor(options.GetOptionValue(MinimapOptions.ViewportBorderColorId), translucentAlpha: 0xFF) is { } border
            ? new SolidColorBrush(border)
            : Get(MinimapFormatNames.ViewportBorder) ?? s_viewportBorder;

        return new MinimapPalette(
            Background: Get(MinimapFormatNames.Background),
            Viewport: viewport,
            ViewportHover: viewportHover,
            ViewportActive: viewportActive,
            ViewportBorder: viewportBorder,
            FindMatch: Get(MinimapFormatNames.FindMatch) ?? s_findMatch,
            Selection: Get(MinimapFormatNames.Selection) ?? s_selection,
            Added: Get(MinimapFormatNames.Added) ?? s_added,
            Modified: Get(MinimapFormatNames.Modified) ?? s_modified,
            Marker: Get(MinimapFormatNames.Marker));
    }

    /// <summary>The colour an error mark of <paramref name="errorType"/> takes: the error type's own, else the squiggles' default.</summary>
    public static Color ErrorColor(IEditorFormatMap formatMap, string errorType)
    {
        if (formatMap.GetProperties(errorType) is { } properties
            && properties.TryGetValue(EditorFormatDefinition.ForegroundBrushId, out var value)
            && value is ISolidColorBrush brush)
        {
            return brush.Color;
        }

        return s_errorColors.TryGetValue(errorType, out var color) ? color : s_errorColors[PredefinedErrorTypeNames.SyntaxError];
    }

    /// <summary>
    /// The colour a text marker of <paramref name="markerType"/> takes, read as the text-marker adornments read it: the
    /// marker's fill, else the background of a classification-shaped entry, else its border; null when it has none.
    /// </summary>
    public static Color? MarkerColor(IEditorFormatMap formatMap, string markerType)
    {
        var properties = formatMap.GetProperties(markerType);
        var fill = properties.TryGetValue(MarkerFormatDefinition.FillId, out var fillValue) ? fillValue as IBrush : null;
        fill ??= properties.TryGetValue(EditorFormatDefinition.BackgroundBrushId, out var background) ? background as IBrush : null;
        if (MinimapInkSource.ColorOf(fill) is { A: > 0 } color)
        {
            return color;
        }

        if (properties.TryGetValue(EditorFormatDefinition.BackgroundColorId, out var colorValue) && colorValue is Color { A: > 0 } plain)
        {
            return plain;
        }

        return properties.TryGetValue(MarkerFormatDefinition.BorderId, out var borderValue)
            && borderValue is IPen pen
            && MinimapInkSource.ColorOf(pen.Brush) is { A: > 0 } border
                ? border
                : null;
    }

    /// <summary>
    /// Reads <c>#RRGGBB</c> (given <paramref name="translucentAlpha"/>) or <c>#AARRGGBB</c>, with or without the hash; null
    /// for an empty or unreadable text.
    /// </summary>
    internal static Color? ParseColor(string? text, byte translucentAlpha)
    {
        string hex = (text ?? string.Empty).Trim().TrimStart('#');
        if (!uint.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint value))
        {
            return null;
        }

        return hex.Length switch
        {
            6 => Color.FromArgb(translucentAlpha, (byte)(value >> 16), (byte)(value >> 8), (byte)value),
            8 => Color.FromArgb((byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)value),
            _ => null,
        };
    }

    private static Color WithAlpha(Color color, int alpha) => Color.FromArgb((byte)Math.Clamp(alpha, 0, 255), color.R, color.G, color.B);
}
