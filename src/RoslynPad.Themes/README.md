# RoslynPad.Themes

A .NET port of the Visual Studio Code theme reader. Parses VS Code JSON theme files into a `Theme` object that can be used for syntax highlighting.

The VS Code default themes (dark and light) are embedded in the assembly.

## Key Types

### `VsCodeThemeReader`

Reads and resolves VS Code theme files, including inheritance chains.

```csharp
IThemeReader reader = new VsCodeThemeReader();
Theme theme = await reader.ReadThemeAsync("path/to/theme.json", ThemeType.Dark);
```

### `Theme`

Represents a parsed color theme.

```csharp
// Look up a token color by TextMate scope
var settings = theme.TryGetScopeSettings("keyword.control");
if (settings.HasValue)
{
    string? foreground = settings.Value.Value.Foreground; // e.g. "#569CD6"
    string? fontStyle = settings.Value.Value.FontStyle;   // e.g. "bold"
}

// Look up a UI color
string? editorBg = theme.TryGetColor("editor.background");
```

**Properties:** `Name`, `Type` (`Light`/`Dark`), `TokenColors`, `Colors`

### `IColorRegistry`

Implement to provide default color resolution for theme color references.

```csharp
string? color = registry.ResolveDefaultColor("editor.foreground", theme);
```

## Integration with RoslynPad.Editor

Use `ThemeClassificationColors` from the Editor packages to apply a theme to the code editor:

```csharp
var colors = new ThemeClassificationColors(theme);
await editor.InitializeAsync(host, colors, workingDirectory, "", SourceCodeKind.Script);
```
