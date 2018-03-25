using System.IO;
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

        public static Drawing ToImageSource(this Glyph glyph)
        {
            var image = _service.GetGlyphImage(glyph);
            return image;
        }

        private class GlyphService
        {
            private readonly ResourceDictionary _glyphs;

            public GlyphService()
            {
                using (var stream = typeof(Glyphs).Assembly.GetManifestResourceStream(typeof(Glyphs), $"{nameof(Glyphs)}.{nameof(Glyphs)}.xaml"))
                {
                    _glyphs = (ResourceDictionary)new AvaloniaXamlLoader().Load(stream);
                }
            }

            public Drawing GetGlyphImage(Glyph glyph) => _glyphs?[glyph] as Drawing;
        }
    }
}