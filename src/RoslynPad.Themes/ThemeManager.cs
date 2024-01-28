using System.Text.Json;

namespace RoslynPad.Themes;

/// <summary>
/// Parses Visual Studio Code themes.
/// </summary>
public class ThemeManager
{
    private static readonly JsonSerializerOptions s_serializerOptions = new(JsonSerializerDefaults.Web)
    {
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
    };

    public async Task<Theme> ReadThemeAsync(string file)
    {
        var themes = new Stack<Theme>();
        var theme = await ReadThemeFileAsync(file).ConfigureAwait(false);
        themes.Push(theme);
        while (theme.Include is not null)
        {
            var includePath = Path.Combine(Path.GetDirectoryName(file).NotNull(), theme.Include);
            theme = await ReadThemeFileAsync(includePath).ConfigureAwait(false);
            themes.Push(theme);
        }


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
            theme.Type ??= nextTheme.Type;

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
        var theme = await JsonSerializer.DeserializeAsync<Theme>(stream, s_serializerOptions).ConfigureAwait(false);
        return theme.NotNull();
    }
}
