using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace RoslynPad.Roslyn.SignatureHelp
{
    public class SignatureHelpItem
    {
        public bool IsVariadic { get; }

        public ImmutableArray<SymbolDisplayPart> PrefixDisplayParts { get; }

        public ImmutableArray<SymbolDisplayPart> SuffixDisplayParts { get; }

        public ImmutableArray<SymbolDisplayPart> SeparatorDisplayParts { get; }

        public ImmutableArray<SignatureHelpParameter> Parameters { get; }

        public ImmutableArray<SymbolDisplayPart> DescriptionParts { get; }

        public Func<CancellationToken, IEnumerable<SymbolDisplayPart>> DocumentationFactory { get; }

        internal SignatureHelpItem(Microsoft.CodeAnalysis.SignatureHelp.SignatureHelpItem inner)
        {
            IsVariadic = inner.IsVariadic;
            PrefixDisplayParts = inner.PrefixDisplayParts;
            SuffixDisplayParts = inner.SuffixDisplayParts;
            SeparatorDisplayParts = inner.SeparatorDisplayParts;
            Parameters = ImmutableArray.CreateRange(inner.Parameters.Select(source => new SignatureHelpParameter(source)));
            DescriptionParts = inner.DescriptionParts;
            IsVariadic = inner.IsVariadic;
            DocumentationFactory = inner.DocumentationFactory;
        }
    }
}