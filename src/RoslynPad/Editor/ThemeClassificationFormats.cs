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
        DefaultForeground = theme.TryGetColor("editor.foreground") is { } foreground ? ThemeDictionaryBase.ParseThemeColor(foreground) : null;
        Background = theme.TryGetColor("editor.background") is { } background ? ThemeDictionaryBase.ParseThemeColor(background) : null;

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
                properties[property] = new SolidColorBrush(ThemeDictionaryBase.ParseThemeColor(color));
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
                properties[property] = new SolidColorBrush(ThemeDictionaryBase.ParseThemeColor(color));
            }
        }
    }

    /// <summary>
    /// Feeds the theme's progress bar color to the background-work indicator (the indeterminate
    /// bar shown at the top of the view during async operations like go-to-definition into
    /// decompiled or Source Link sources).
    /// </summary>
    public void ApplyBackgroundWorkIndicator(IEditorFormatMap formatMap)
    {
        if (_theme.TryGetColor("progressBar.background") is { } color)
        {
            var properties = new Avalonia.Controls.ResourceDictionary
            {
                [BackgroundWorkIndicatorFormatNames.Foreground] = new SolidColorBrush(ThemeDictionaryBase.ParseThemeColor(color)),
            };
            formatMap.SetProperties(BackgroundWorkIndicatorFormatNames.Name, properties);
        }
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
            ? ThemeDictionaryBase.ParseThemeColor(cursor)
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
    public void ApplyBraceMatching(IEditorFormatMap formatMap) =>
        ApplyMarker(formatMap, Microsoft.CodeAnalysis.BraceMatching.ClassificationTypeDefinitions.BraceMatchingName,
            "editorBracketMatch.background", "editorBracketMatch.border");

    /// <summary>
    /// Feeds the theme's word-highlight colors to the reference highlight markers
    /// (TextMarkerAdornmentManager) through the editor format map entries Roslyn's
    /// NavigableHighlightTags name: read references use editor.wordHighlight*, the definition
    /// and written references the strong variants (VS Code's write-access colors). The static
    /// marker format exports in Morgania.CodeAnalysis.Editor remain the fallback.
    /// </summary>
    public void ApplyReferenceHighlighting(IEditorFormatMap formatMap)
    {
        ApplyMarker(formatMap, Microsoft.CodeAnalysis.Editor.ReferenceHighlighting.ReferenceHighlightTag.TagId,
            "editor.wordHighlightBackground", "editor.wordHighlightBorder");
        ApplyMarker(formatMap, Microsoft.CodeAnalysis.Editor.ReferenceHighlighting.DefinitionHighlightTag.TagId,
            "editor.wordHighlightStrongBackground", "editor.wordHighlightStrongBorder");
        ApplyMarker(formatMap, Microsoft.CodeAnalysis.Editor.ReferenceHighlighting.WrittenReferenceHighlightTag.TagId,
            "editor.wordHighlightStrongBackground", "editor.wordHighlightStrongBorder");
    }

    private void ApplyMarker(IEditorFormatMap formatMap, string markerName, string backgroundKey, string borderKey)
    {
        var background = _theme.TryGetColor(backgroundKey);
        var border = _theme.TryGetColor(borderKey);
        if (background is null && border is null)
        {
            return;
        }

        var properties = new Avalonia.Controls.ResourceDictionary();
        if (background is not null)
        {
            properties[MarkerFormatDefinition.FillId] = new SolidColorBrush(ThemeDictionaryBase.ParseThemeColor(background));
        }

        if (border is not null)
        {
            properties[MarkerFormatDefinition.BorderId] = new Pen(new SolidColorBrush(ThemeDictionaryBase.ParseThemeColor(border)));
        }

        formatMap.SetProperties(markerName, properties);
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

        // Multiple classifications can share a token name (e.g. identifier and local name are both
        // "variable"), so map each classification independently; the custom map wins on conflicts.
        var tokensMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var classification in SemanticTokensSchema.ClassificationTypeNameToTokenName.Concat(SemanticTokensSchema.ClassificationTypeNameToCustomTokenName))
        {
            tokensMap[classification.Key] = classification.Value;
        }

        return tokensMap
            .Where(t => scopes.ContainsKey(t.Value))
            .Select(t => (classification: t.Key, scopes: scopes[t.Value]))
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
            Foreground: scopeSettings.Value.Foreground is { } foreground ? ThemeDictionaryBase.ParseThemeColor(foreground) : null,
            Bold: scopeSettings.Value.FontStyle?.Contains("bold", StringComparison.OrdinalIgnoreCase) == true,
            Italic: scopeSettings.Value.FontStyle?.Contains("italic", StringComparison.OrdinalIgnoreCase) == true)
        : null;
}
