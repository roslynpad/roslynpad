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
                var assembly = typeof(Glyphs).Assembly;
                using (var stream = assembly.GetManifestResourceStream(typeof(Glyphs), $"{nameof(Glyphs)}.{nameof(Glyphs)}.xaml"))
                {
                    _glyphs = (ResourceDictionary)new AvaloniaXamlLoader().Load(stream, assembly);
                }
            }

            public Drawing GetGlyphImage(Glyph glyph) => _glyphs?[glyph] as Drawing;
        }
    }
}