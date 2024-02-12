using System.Text.Json;

namespace RoslynPad.Themes;

/// <summary>
/// Parses Visual Studio Code themes.
/// </summary>
public class VsCodeThemeReader : IThemeReader
{
    private static readonly JsonSerializerOptions s_serializerOptions = new(JsonSerializerDefaults.Web)
    {
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
    };

    private static readonly Lazy<Task<Theme>> s_vsDarkTheme = new(() => ReadThemeEmebeddedResourceAsync("vs2019_dark"));
    private static readonly Lazy<Task<Theme>> s_vsLightTheme = new(() => ReadThemeEmebeddedResourceAsync("vs2019_light"));

    public async Task<Theme> ReadThemeAsync(string file, ThemeType type)
    {
        var themes = new Stack<Theme>();
        var theme = await ReadThemeFileAsync(file).ConfigureAwait(false);
        theme.Type = type;
        themes.Push(theme);

        while (theme.Include is not null)
        {
            var includePath = Path.Combine(Path.GetDirectoryName(file).NotNull(), theme.Include);
            theme = await ReadThemeFileAsync(includePath).ConfigureAwait(false);
            themes.Push(theme);
        }

        var baseTheme = await (type == ThemeType.Dark ? s_vsDarkTheme.Value : s_vsLightTheme.Value).ConfigureAwait(false);
        themes.Push(baseTheme);


        theme = themes.Pop();

        if (theme.Colors is null)
        {
            theme = theme with { Colors = [] };
        }

        if (theme.TokenColors is null)
        {
            theme = theme with { TokenColors = [] };
        }

        while (themes.TryPop(out var nextTheme))
        {
            if (nextTheme.Colors is not null)
            {
                foreach (var color in nextTheme.Colors)
                {
                    theme.Colors[color.Key] = color.Value;
                }
            }

            if (nextTheme.TokenColors is not null)
            {
                foreach (var tokenColor in nextTheme.TokenColors)
                {
                    theme.TokenColors.Add(tokenColor);
                }
            }
        }

        theme.TokenColors.Reverse();

        foreach (var tokenColor in theme.TokenColors)
        {
            if (tokenColor.Settings is null)
            {
                continue;
            }

            foreach (var scope in tokenColor.Scope)
            {
                theme.ScopeSettings.TryAdd(scope, tokenColor.Settings);
            }
        }

        return theme;
    }

    private static async Task<Theme> ReadThemeFileAsync(string file)
    {
        using var stream = File.OpenRead(file);
        return await ReadThemeAsync(stream).ConfigureAwait(false);
    }

    private static async Task<Theme> ReadThemeEmebeddedResourceAsync(string name)
    {
        using var stream = typeof(VsCodeThemeReader).Assembly.GetManifestResourceStream($"RoslynPad.Themes.Themes.{name}.json")
            ?? throw new InvalidOperationException("Stream not found");
        return await ReadThemeAsync(stream).ConfigureAwait(false);
    }

    private static async Task<Theme> ReadThemeAsync(Stream stream)
    {
        var theme = await JsonSerializer.DeserializeAsync<Theme>(stream, s_serializerOptions).ConfigureAwait(false);
        return theme.NotNull();
    }
}
