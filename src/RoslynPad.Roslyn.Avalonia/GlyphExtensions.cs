using RoslynPad.Roslyn.Completion;
using RoslynPad.Roslyn.Resources;
using Avalonia.Media;

namespace RoslynPad.Roslyn;

public static class GlyphExtensions
{
    public static IGlyphService GlyphService { get; set; } = new DefaultGlyphService();

    public static DrawingImage? ToImageSource(this Glyph glyph) => GlyphService.GetGlyphImage(glyph);

    private class DefaultGlyphService : IGlyphService
    {
        private readonly Glyphs _glyphs = new();

        public DrawingImage? GetGlyphImage(Glyph glyph)
        {
            if (_glyphs.TryGetValue(glyph.ToString(), out var glyphImage) && glyphImage is Drawing drawing)
            {
                return new DrawingImage { Drawing = drawing };
            }

            return null;
        }
    }
}
