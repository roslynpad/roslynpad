# RoslynPad.Roslyn.Avalonia

Provides Avalonia-specific implementations for UI elements required by the `RoslynPad.Roslyn` package.

## Key Types

### `GlyphExtensions`

Converts Roslyn `Glyph` values to Avalonia `DrawingImage`.

```csharp
DrawingImage? image = glyph.ToImageSource();
```

### `SymbolDisplayPartExtensions`

Converts Roslyn `TaggedText` to Avalonia elements for rich display.

```csharp
TextBlock run = taggedText.ToRun(isBold: false);
Panel panel = taggedTexts.ToTextBlock(isBold: false);
```

### XAML Converters

- **`TaggedTextToTextBlockConverter`** — binds `IEnumerable<TaggedText>` to display elements

For a full initialization and editor integration sample, see the [samples](https://github.com/aelij/RoslynPad/tree/main/samples) directory.
