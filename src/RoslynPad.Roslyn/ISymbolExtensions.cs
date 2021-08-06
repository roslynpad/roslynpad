using Microsoft.CodeAnalysis;

namespace RoslynPad.Roslyn
{
    public static class ISymbolExtensions
    {
        public static Completion.Glyph GetGlyph(this ISymbol symbol)
        {
            return (Completion.Glyph)Microsoft.CodeAnalysis.Shared.Extensions.ISymbolExtensions2.GetGlyph(symbol);
        }
    }
}
