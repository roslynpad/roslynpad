# RoslynPad.Roslyn.Windows

Provides WPF-specific implementations for UI elements required by the `RoslynPad.Roslyn` package.

## Key Types

### `GlyphExtensions`

Converts Roslyn `Glyph` values to WPF `ImageSource`.

```csharp
ImageSource? image = glyph.ToImageSource();
```

### `SymbolDisplayPartExtensions`

Converts Roslyn `TaggedText` to WPF elements for rich display.

```csharp
Run run = taggedText.ToRun(isBold: false);
TextBlock block = taggedTexts.ToTextBlock(isBold: false);
```

### XAML Converters

- **`GlyphToImageSourceConverter`** — binds `Glyph` values to `Image.Source`
- **`TaggedTextToTextBlockConverter`** — binds `IEnumerable<TaggedText>` to display elements

For a full initialization and editor integration sample, see the [samples](https://github.com/aelij/RoslynPad/tree/main/samples) directory.
