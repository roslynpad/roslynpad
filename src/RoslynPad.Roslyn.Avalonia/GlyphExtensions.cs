using RoslynPad.Roslyn.Completion;
using RoslynPad.Roslyn.Resources;
using Avalonia.Media;

namespace RoslynPad.Roslyn
{
    public static class GlyphExtensions
    {
        private static readonly GlyphService _service = new();

        public static DrawingImage? ToImageSource(this Glyph glyph)
        {
            var image = _service.GetGlyphImage(glyph);
            return image;
        }

        private class GlyphService
        {
            private readonly Glyphs _glyphs = new();

            public DrawingImage? GetGlyphImage(Glyph glyph)
            {
                if (_glyphs != null && _glyphs.TryGetValue(glyph.ToString(), out var glyphImage) && glyphImage is Drawing drawing)
                {
                    return new DrawingImage { Drawing = drawing };
                }

                return null;
            }
        }
    }
}
