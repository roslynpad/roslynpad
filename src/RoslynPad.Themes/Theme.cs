using System.Text.Json.Serialization;

namespace RoslynPad.Themes;

public record Theme(
    string Name,
    Dictionary<string, string>? Colors,
    List<TokenColor>? TokenColors
)
{
    public string? Type { get; set; }

    [JsonInclude]
    internal string? Include { get; set; }

    internal Trie<TokenColorSettings> ScopeSettings { get; } = new();

    public KeyValuePair<string, TokenColorSettings>? TryGetScopeSettings(string scope) => ScopeSettings.FindLongestPrefix(scope);
}
