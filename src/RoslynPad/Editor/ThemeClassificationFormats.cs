using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;
using Avalonia.Media;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Morgania.CodeAnalysis.Editor.Classification;
using RoslynPad.Themes;

namespace RoslynPad.Editor;

/// <summary>
/// Maps a VS Code theme onto Roslyn classification types.
/// The static format definitions in ClassificationFormats.cs remain the fallback for types the
/// theme does not style.
/// </summary>
public sealed partial class ThemeClassificationFormats
{
    private static readonly ImmutableArray<(string classification, string[] scopes)> s_classifiedScopes = GetClassifiedScopes();

    private readonly Dictionary<string, ThemeStyle> _styles;
    private readonly Theme _theme;

    public ThemeClassificationFormats(Theme theme)
    {
        _theme = theme;
        DefaultForeground = theme.TryGetColor("editor.foreground") is { } foreground ? Color.Parse(foreground) : null;
        Background = theme.TryGetColor("editor.background") is { } background ? Color.Parse(background) : null;

        _styles = s_classifiedScopes
            .Select(t => (t.classification, style: GetStyleForScopes(theme, t.scopes)))
            .Where(t => t.style is not null)
            .ToDictionary(t => t.classification, t => t.style!.Value, StringComparer.OrdinalIgnoreCase);
    }

    public Color? DefaultForeground { get; }
    public Color? Background { get; }

    public Color? GetForeground(string classificationTypeName) =>
        _styles.TryGetValue(classificationTypeName, out var style) ? style.Foreground ?? DefaultForeground : DefaultForeground;

    /// <summary>Applies the theme's styles to a view's classification format map.</summary>
    public void Apply(IClassificationFormatMap formatMap, IClassificationTypeRegistryService registry)
    {
        formatMap.BeginBatchUpdate();
        try
        {
            var defaultProperties = formatMap.DefaultTextProperties;
            if (DefaultForeground is { } foreground)
            {
                defaultProperties = defaultProperties.SetForeground(foreground);
            }

            formatMap.DefaultTextProperties = defaultProperties;

            foreach (var (classification, style) in _styles)
            {
                if (registry.GetClassificationType(ClassificationLayer.Semantic, classification) is not { } type)
                {
                    continue;
                }

                var properties = TextFormattingRunProperties.CreateTextFormattingRunProperties();
                if (style.Foreground is { } styleForeground)
                {
                    properties = properties.SetForeground(styleForeground);
                }

                if (style.Bold)
                {
                    properties = properties.SetBold(true);
                }

                if (style.Italic)
                {
                    properties = properties.SetItalic(true);
                }

                formatMap.SetExplicitTextProperties(type, properties);
            }
        }
        finally
        {
            formatMap.EndBatchUpdate();
        }
    }

    /// <summary>
    /// Feeds the theme's widget colors to the Morgania intellisense popups (quick info,
    /// signature help, completion) through the editor format map's popup palette.
    /// </summary>
    public void ApplyPopup(IEditorFormatMap formatMap)
    {
        var properties = new Avalonia.Controls.ResourceDictionary();
        Set(PopupFormatNames.Background, "editorWidget.background");
        Set(PopupFormatNames.Foreground, "editorWidget.foreground");
        Set(PopupFormatNames.BorderBrush, "editorWidget.border");
        Set(PopupFormatNames.SelectionBackground, "list.activeSelectionBackground");
        Set(PopupFormatNames.SelectionForeground, "list.activeSelectionForeground");
        Set(PopupFormatNames.SoftSelectionBorder, "focusBorder");
        Set(PopupFormatNames.MatchForeground, "list.highlightForeground");
        Set(PopupFormatNames.DeemphasizedForeground, "list.deemphasizedForeground");
        formatMap.SetProperties(PopupFormatNames.Name, properties);

        void Set(string property, string colorId)
        {
            if (_theme.TryGetColor(colorId) is { } color)
            {
                properties[property] = new SolidColorBrush(Color.Parse(color));
            }
        }
    }

    /// <summary>
    /// Feeds the theme's widget and match colors to the Morgania find/replace margin through
    /// the editor format map's find/replace palette.
    /// </summary>
    public void ApplyFindReplace(IEditorFormatMap formatMap)
    {
        var properties = new Avalonia.Controls.ResourceDictionary();
        Set(FindReplaceFormatNames.Background, "editorWidget.background");
        Set(FindReplaceFormatNames.Foreground, "editorWidget.foreground");
        Set(FindReplaceFormatNames.BorderBrush, "editorWidget.border");
        Set(FindReplaceFormatNames.InputBackground, "input.background");
        Set(FindReplaceFormatNames.InputForeground, "input.foreground");
        Set(FindReplaceFormatNames.InputBorder, "input.border");
        Set(FindReplaceFormatNames.MatchBackground, "editor.findMatchHighlightBackground");
        Set(FindReplaceFormatNames.CurrentMatchBackground, "editor.findMatchBackground");
        Set(FindReplaceFormatNames.NoMatchForeground, "errorForeground");
        formatMap.SetProperties(FindReplaceFormatNames.Name, properties);

        void Set(string property, string colorId)
        {
            if (_theme.TryGetColor(colorId) is { } color)
            {
                properties[property] = new SolidColorBrush(ParseThemeColor(color));
            }
        }
    }

    /// <summary>
    /// Parses a VS Code theme color, which uses CSS #RRGGBBAA ordering for 8-digit hex values
    /// (Avalonia's <see cref="Color.Parse"/> would read those as #AARRGGBB).
    /// </summary>
    private static Color ParseThemeColor(string color)
    {
        if (color.Length == 9 && color[0] == '#' && uint.TryParse(color.AsSpan(1), System.Globalization.NumberStyles.HexNumber, null, out var rgba))
        {
            return Color.FromUInt32(((rgba & 0xFF) << 24) | (rgba >> 8));
        }

        return Color.Parse(color);
    }

    /// <summary>
    /// Feeds the theme's cursor color to the caret layer through the editor format map.
    /// The bundled themes don't define <c>editorCursor.foreground</c>, so the fallback mirrors
    /// VS Code's coded defaults: black on light themes, a light gray on dark ones (the
    /// secondary-caret entry stays unset and derives from the primary, dimmed).
    /// </summary>
    public void ApplyCaret(IEditorFormatMap formatMap)
    {
        var color = _theme.TryGetColor("editorCursor.foreground") is { } cursor
            ? Color.Parse(cursor)
            : _theme.Type == ThemeType.Light ? Colors.Black : Color.FromRgb(0xAE, 0xAF, 0xAD);

        var properties = new Avalonia.Controls.ResourceDictionary
        {
            [EditorFormatDefinition.ForegroundBrushId] = new SolidColorBrush(color),
        };
        formatMap.SetProperties(Microsoft.VisualStudio.Text.Editor.CaretFormatNames.Primary, properties);
    }

    /// <summary>
    /// Feeds the theme's bracket-match colors to the brace highlight markers
    /// (TextMarkerAdornmentManager) through the editor format map entry Roslyn's
    /// BraceHighlightTag names. The static BraceMatchingMarkerFormat export remains the
    /// fallback when the theme defines neither color.
    /// </summary>
    public void ApplyBraceMatching(IEditorFormatMap formatMap)
    {
        var background = _theme.TryGetColor("editorBracketMatch.background");
        var border = _theme.TryGetColor("editorBracketMatch.border");
        if (background is null && border is null)
        {
            return;
        }

        var properties = new Avalonia.Controls.ResourceDictionary();
        if (background is not null)
        {
            properties[MarkerFormatDefinition.FillId] = new SolidColorBrush(Color.Parse(background));
        }

        if (border is not null)
        {
            properties[MarkerFormatDefinition.BorderId] = new Pen(new SolidColorBrush(Color.Parse(border)));
        }

        formatMap.SetProperties(Microsoft.CodeAnalysis.BraceMatching.ClassificationTypeDefinitions.BraceMatchingName, properties);
    }

    private readonly record struct ThemeStyle(Color? Foreground, bool Bold, bool Italic);

    private static ImmutableArray<(string classification, string[] scopes)> GetClassifiedScopes()
    {
        var vsCodeScopes = ReadScopes("scopes-vscode");
        var roslynScopes = ReadScopes("scopes-roslyn");

        var scopes = new Dictionary<string, string[]>(vsCodeScopes, StringComparer.OrdinalIgnoreCase);
        foreach (var scope in roslynScopes)
        {
            scopes[scope.Key] = scope.Value;
        }

        var classificationsMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var classification in SemanticTokensSchema.ClassificationTypeNameToTokenName.Concat(SemanticTokensSchema.ClassificationTypeNameToCustomTokenName))
        {
            classificationsMap[classification.Value] = classification.Key;
        }

        return scopes.Select(d => (name: d.Key, found: classificationsMap.TryGetValue(d.Key, out var classification), classification: classification!, scopes: d.Value))
            .Where(d => d.found)
            .Select(d => (d.classification, d.scopes))
            .ToImmutableArray();
    }

    private static Dictionary<string, string[]> ReadScopes(string name)
    {
        using var stream = typeof(ThemeClassificationFormats).Assembly.GetManifestResourceStream($"RoslynPad.Editor.Resources.{name}.json")
            ?? throw new InvalidOperationException("Stream not found");
        return JsonSerializer.Deserialize(stream, ScopesJsonContext.Default.DictionaryStringStringArray)
            ?? throw new InvalidOperationException($"Empty {name}.json");
    }

    [JsonSourceGenerationOptions(JsonSerializerDefaults.Web, AllowTrailingCommas = true, ReadCommentHandling = JsonCommentHandling.Skip)]
    [JsonSerializable(typeof(Dictionary<string, string[]>))]
    private sealed partial class ScopesJsonContext : JsonSerializerContext;

    private static ThemeStyle? GetStyleForScopes(Theme theme, string[] scopes) =>
        scopes.Select(theme.TryGetScopeSettings).FirstOrDefault(s => s is not null) is { } scopeSettings
        ? new ThemeStyle(
            Foreground: scopeSettings.Value.Foreground is { } foreground ? Color.Parse(foreground) : null,
            Bold: scopeSettings.Value.FontStyle?.Contains("bold", StringComparison.OrdinalIgnoreCase) == true,
            Italic: scopeSettings.Value.FontStyle?.Contains("italic", StringComparison.OrdinalIgnoreCase) == true)
        : null;
}
