using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using RoslynPad.Roslyn.Completion;

namespace RoslynPad.Roslyn
{
    public static class GlyphExtensions
    {
        public static IBitmap ToImageSource(this Glyph glyph)
        {
            return Application.Current?.FindStyleResource(glyph.ToString()) as IBitmap;
        }
    }
}