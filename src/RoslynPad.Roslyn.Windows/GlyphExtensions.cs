using System.Windows.Media;
using RoslynPad.Roslyn.Completion;
using RoslynPad.Roslyn.Resources;

namespace RoslynPad.Roslyn
{
    public static class GlyphExtensions
    {
        private static readonly GlyphService _service = new GlyphService();

        public static ImageSource? ToImageSource(this Glyph glyph) => _service.GetGlyphImage(glyph);

        private class GlyphService
        {
            private readonly Glyphs _glyphs;

            public GlyphService()
            {
                _glyphs = new Glyphs();
            }

            public ImageSource? GetGlyphImage(Glyph glyph) => _glyphs[glyph] as ImageSource;
        }
    }
}