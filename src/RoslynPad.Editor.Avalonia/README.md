# RoslynPad.Editor.Avalonia

A Roslyn-powered code editor control for Avalonia, built on AvaloniaEdit. Provides completion, diagnostics, signature help, quick actions, and code folding out of the box.

## Key Types

### `RoslynCodeEditor`

The main editor control. Extends `CodeTextEditor` with full Roslyn integration.

```xml
<Window xmlns:editor="clr-namespace:RoslynPad.Editor;assembly=RoslynPad.Editor.Avalonia">
    <editor:RoslynCodeEditor x:Name="Editor"
        FontFamily="Consolas" />
</Window>
```

```csharp
var documentId = await editor.InitializeAsync(
    host,
    new ClassificationHighlightColors(),
    workingDirectory,
    documentText: string.Empty,
    SourceCodeKind.Script);
```

**Properties:** `IsCodeFoldingEnabled`, `IsBraceCompletionEnabled`, `ContextActionsIcon`

### `ClassificationHighlightColors`

Default syntax highlighting colors with configurable brushes.

**Brushes:** `TypeBrush`, `MethodBrush`, `KeywordBrush`, `StringBrush`, `CommentBrush`, `BraceMatchingBrush`, and more.

### `ThemeClassificationColors`

Creates syntax highlighting from a VS Code `Theme` (from the `RoslynPad.Themes` package).

```csharp
var theme = await themeReader.ReadThemeAsync(themeFile, ThemeType.Dark);
var colors = new ThemeClassificationColors(theme);
await editor.InitializeAsync(host, colors, workingDirectory, "", SourceCodeKind.Script);
```

### `AvalonEditTextContainer`

Bridges AvaloniaEdit's `TextDocument` to Roslyn's `SourceTextContainer`.

### `CreatingDocumentEventArgs`

Raised during initialization. Use to customize document creation (e.g., for REPL chaining):

```csharp
editor.CreatingDocument += (sender, args) =>
{
    args.DocumentId = host.AddRelatedDocument(
        previousDocumentId,
        new DocumentCreationArgs(args.TextContainer, workingDirectory,
            SourceCodeKind.Script, args.TextContainer.UpdateText));
};
```

For a full initialization and editor integration sample, see the [samples](https://github.com/aelij/RoslynPad/tree/main/samples) directory.
