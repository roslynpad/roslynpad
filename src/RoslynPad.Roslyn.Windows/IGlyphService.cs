using System.Windows.Media;
using RoslynPad.Roslyn.Completion;

namespace RoslynPad.Roslyn;

public interface IGlyphService
{
    ImageSource? GetGlyphImage(Glyph glyph);
}
