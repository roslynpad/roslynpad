using System.Text.Json.Serialization;

namespace RoslynPad.Themes;

public class Theme
{
    private readonly IColorRegistry? _colorRegistry;

    public Theme()
    {
    }

    public Theme(IColorRegistry colorRegistry)
    {
        _colorRegistry = colorRegistry;
    }

    public required string Name { get; set; }

    public List<TokenColor>? TokenColors { get; set; }

    public Dictionary<string, string>? Colors { get; set; }

    [JsonIgnore]
    public ThemeType Type { get; set; }

    [JsonInclude]
    internal string? Include { get; set; }

    internal Trie<TokenColorSettings> ScopeSettings { get; } = new();

    /// <summary>
    /// Scope settings from the built-in base theme, consulted only when <see cref="ScopeSettings"/>
    /// (this theme and its includes) has no match at any specificity. Kept separate so a base
    /// rule that is more specific than a theme rule (e.g. the base's <c>comment.documentation</c>
    /// vs. the theme's <c>comment</c>) cannot outrank the theme via longest-prefix matching.
    /// </summary>
    internal Trie<TokenColorSettings> ScopeSettingsFallback { get; } = new();

    public KeyValuePair<string, TokenColorSettings>? TryGetScopeSettings(string scope) =>
        ScopeSettings.FindLongestPrefix(scope) ?? ScopeSettingsFallback.FindLongestPrefix(scope);

    public string? TryGetColor(string id)
    {
        if (Colors?.TryGetValue(id, out var themeColor) == true)
        {
            return themeColor;
        }

        var color = _colorRegistry.NotNull().ResolveDefaultColor(id, this);
        if (color is not null)
        {
            Colors ??= [];
            Colors.Add(id, color);
            return color;
        }

        return null;
    }
}
