using Avalonia.Media.Imaging;
using Avalonia.Threading;
using RoslynPad.Roslyn.Completion;
using RoslynPad.Roslyn.Resources;
using System.Collections.Generic;
using System.Reflection;

namespace RoslynPad.Roslyn
{
    public static class GlyphExtensions
    {
        private static readonly GlyphService _service = new GlyphService();

        public static IBitmap ToImageSource(this Glyph glyph) => _service.GetGlyphImage(glyph);

        private class GlyphService
        {
            private readonly Dictionary<Glyph, IBitmap> _cache = new Dictionary<Glyph, IBitmap>();

            public IBitmap GetGlyphImage(Glyph glyph)
            {
                Dispatcher.UIThread.VerifyAccess();

                if (!_cache.TryGetValue(glyph, out var image))
                {
                    var assembly = typeof(Glyphs).GetTypeInfo().Assembly;
                    using (var stream = assembly.GetManifestResourceStream(typeof(Glyphs).FullName + "." + glyph + ".png"))
                    {
                        if (stream != null)
                        {
                            image = new Bitmap(stream);
                        }
                    }

                    _cache.Add(glyph, image);
                }

                return image;
            }
        }
    }
}