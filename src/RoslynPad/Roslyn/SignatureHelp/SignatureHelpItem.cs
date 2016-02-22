using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using RoslynPad.Utilities;

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

        internal SignatureHelpItem(object inner)
        {
            IsVariadic = inner.GetPropertyValue<bool>(nameof(IsVariadic));
            PrefixDisplayParts = inner.GetPropertyValue<ImmutableArray<SymbolDisplayPart>>(nameof(PrefixDisplayParts));
            SuffixDisplayParts = inner.GetPropertyValue<ImmutableArray<SymbolDisplayPart>>(nameof(SuffixDisplayParts));
            SeparatorDisplayParts = inner.GetPropertyValue<ImmutableArray<SymbolDisplayPart>>(nameof(SeparatorDisplayParts));
            Parameters = ImmutableArray.CreateRange(inner.GetPropertyValue<IEnumerable<object>>(nameof(Parameters)).Select(source => new SignatureHelpParameter(source)));
            DescriptionParts = inner.GetPropertyValue<ImmutableArray<SymbolDisplayPart>>(nameof(DescriptionParts));
            IsVariadic = inner.GetPropertyValue<bool>(nameof(IsVariadic));
            DocumentationFactory = inner.GetPropertyValue<Func<CancellationToken, IEnumerable<SymbolDisplayPart>>>(nameof(DocumentationFactory));
        }
    }
}