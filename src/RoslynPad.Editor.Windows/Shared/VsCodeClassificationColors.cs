using System.Text.Json;
using Microsoft.CodeAnalysis.Classification;
using RoslynPad.Themes;
using RoslynPad.Roslyn.Classification;

namespace RoslynPad.Editor;

public class VsCodeClassificationColors : IClassificationHighlightColors
{
    public HighlightingColor StaticSymbolColor { get; protected set; } = new();
    public HighlightingColor BraceMatchingColor { get; protected set; } = new HighlightingColor { Background = new SimpleHighlightingBrush(Color.FromArgb(150, 219, 224, 204)) };

    private static readonly JsonSerializerOptions s_serializerOptions = new(JsonSerializerDefaults.Web)
    {
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
    };

    private readonly Dictionary<string, HighlightingColor> _colors;

    public VsCodeClassificationColors(Theme theme)
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

        DefaultBrush = GetColorFromTheme(theme, "editor.foreground");

        _colors = scopes.Select(d => (name: d.Key, found: classificationsMap.TryGetValue(d.Key, out var classification), classification: classification!, scopes: d.Value))
            .Where(d => d.found)
            .Select(t => (t.classification, color: GetColorForScopes(theme, t.scopes, DefaultBrush)))
            .Append((classification: ClassificationTypeNames.StaticSymbol, color: StaticSymbolColor))
            .Append((classification: AdditionalClassificationTypeNames.BraceMatching, color: BraceMatchingColor))
            .ToDictionary(t => t.classification, t => t.color, StringComparer.OrdinalIgnoreCase);
    }

    private static Dictionary<string, string[]> ReadScopes(string name) => DeserializeResource<Dictionary<string, string[]>>(name);

    private static T DeserializeResource<T>(string name)
    {
        using var stream = typeof(VsCodeClassificationColors).Assembly.GetManifestResourceStream($"RoslynPad.Editor.Shared.Resources.{name}.json")
            ?? throw new InvalidOperationException("Stream not found");
        return JsonSerializer.Deserialize<T>(stream, s_serializerOptions)
            ?? throw new InvalidOperationException($"Empty {name}.json");
    }

    private static HighlightingColor GetColorFromTheme(Theme theme, string name) => new HighlightingColor
    {
        Foreground = theme.Colors?.TryGetValue(name, out var value) == true ? ParseBrush(value) : null
    }.AsFrozen();

    private static HighlightingColor GetColorForScopes(Theme theme, string[] scopes, HighlightingColor defaultColor) =>
        scopes.Select(theme.TryGetScopeSettings).FirstOrDefault(s => s is not null) is { } scopeSettings
        ? new HighlightingColor
            {
                FontWeight = scopeSettings.Value.FontStyle?.Contains("bold", StringComparison.OrdinalIgnoreCase) == true ? FontWeights.Bold : null,
                FontStyle = scopeSettings.Value.FontStyle?.Contains("italic", StringComparison.OrdinalIgnoreCase) == true ? FontStyles.Italic : null,
                Foreground = ParseBrush(scopeSettings.Value.Foreground)
            }.AsFrozen()
        : defaultColor;

    private static SimpleHighlightingBrush? ParseBrush(string? value) => value is null ? null : new(Parsers.ParseColor(value));

    public HighlightingColor DefaultBrush { get; }

    public HighlightingColor GetBrush(string classificationTypeName) =>
        _colors.TryGetValue(classificationTypeName, out var color) ? color : DefaultBrush;
}
