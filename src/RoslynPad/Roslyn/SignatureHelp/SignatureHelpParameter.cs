using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis;
using RoslynPad.Utilities;

namespace RoslynPad.Roslyn.SignatureHelp
{
    public class SignatureHelpParameter
    {
        public string Name { get; }

        public Func<CancellationToken, IEnumerable<SymbolDisplayPart>> DocumentationFactory { get; }

        public IList<SymbolDisplayPart> PrefixDisplayParts { get; }

        public IList<SymbolDisplayPart> SuffixDisplayParts { get; }

        public IList<SymbolDisplayPart> DisplayParts { get; }

        public bool IsOptional { get; }

        public IList<SymbolDisplayPart> SelectedDisplayParts { get; }

        internal SignatureHelpParameter(object inner)
        {
            Name = inner.GetPropertyValue<string>(nameof(Name));
            DocumentationFactory = inner.GetPropertyValue<Func<CancellationToken, IEnumerable<SymbolDisplayPart>>>(nameof(DocumentationFactory));
            PrefixDisplayParts = inner.GetPropertyValue<IList<SymbolDisplayPart>>(nameof(PrefixDisplayParts));
            SuffixDisplayParts = inner.GetPropertyValue<IList<SymbolDisplayPart>>(nameof(SuffixDisplayParts));
            DisplayParts = inner.GetPropertyValue<IList<SymbolDisplayPart>>(nameof(DisplayParts));
            IsOptional = inner.GetPropertyValue<bool>(nameof(IsOptional));
            SelectedDisplayParts = inner.GetPropertyValue<IList<SymbolDisplayPart>>(nameof(SelectedDisplayParts));
        }
    }
}