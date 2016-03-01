using System;
using System.Linq.Expressions;
using Microsoft.CodeAnalysis;
using RoslynPad.Roslyn.Completion;

namespace RoslynPad.Roslyn
{
    // ReSharper disable once InconsistentNaming
    public static class ISymbolExtensions
    {
        // ReSharper disable InconsistentNaming
        private static readonly Type ISymbolExtensions2Type = Type.GetType("Microsoft.CodeAnalysis.Shared.Extensions.ISymbolExtensions2, Microsoft.CodeAnalysis.Features", throwOnError: true);
        // ReSharper restore InconsistentNaming

        private static readonly Func<ISymbol, int> _getGlyph = CreateGetGlyph();

        private static Func<ISymbol, int> CreateGetGlyph()
        {
            var p = Expression.Parameter(typeof(ISymbol));
            return Expression.Lambda<Func<ISymbol, int>>(
                Expression.Convert(Expression.Call(ISymbolExtensions2Type.GetMethod(nameof(GetGlyph)), p), typeof (int)),
                p).Compile();
        }

        public static Glyph GetGlyph(this ISymbol symbol)
        {
            return (Glyph)_getGlyph(symbol);
        }
    }
}