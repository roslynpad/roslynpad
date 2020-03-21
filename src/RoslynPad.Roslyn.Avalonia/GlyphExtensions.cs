using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using RoslynPad.Roslyn.Completion;
using RoslynPad.Roslyn.Resources;
using Avalonia.Media;

namespace RoslynPad.Roslyn
{
    public static class GlyphExtensions
    {
        private static readonly GlyphService _service = new GlyphService();

        public static Drawing? ToImageSource(this Glyph glyph)
        {
            var image = _service.GetGlyphImage(glyph);
            return image;
        }

        private class GlyphService
        {
            private readonly Glyphs _glyphs = new Glyphs();

            public Drawing? GetGlyphImage(Glyph glyph)
            {
                if (_glyphs != null && _glyphs.TryGetValue(glyph.ToString(), out var glyphImage))
                {
                    return glyphImage as Drawing;
                }

                return null;
            }
        }
    }
}