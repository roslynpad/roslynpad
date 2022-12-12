using Avalonia.Media;
using RoslynPad.Roslyn.Completion;

namespace RoslynPad.Roslyn;

public interface IGlyphService
{
    DrawingImage? GetGlyphImage(Glyph glyph);
}
