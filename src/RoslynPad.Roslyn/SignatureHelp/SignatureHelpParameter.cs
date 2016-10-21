using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace RoslynPad.Roslyn.SignatureHelp
{
    public class SignatureHelpParameter
    {
        public string Name { get; }

        public Func<CancellationToken, IEnumerable<TaggedText>> DocumentationFactory { get; }

        public IList<TaggedText> PrefixDisplayParts { get; }

        public IList<TaggedText> SuffixDisplayParts { get; }

        public IList<TaggedText> DisplayParts { get; }

        public bool IsOptional { get; }

        public IList<TaggedText> SelectedDisplayParts { get; }

        internal SignatureHelpParameter(Microsoft.CodeAnalysis.SignatureHelp.SignatureHelpParameter inner)
        {
            Name = inner.Name;
            DocumentationFactory = inner.DocumentationFactory;
            PrefixDisplayParts = inner.PrefixDisplayParts;
            SuffixDisplayParts = inner.SuffixDisplayParts;
            DisplayParts = inner.DisplayParts;
            IsOptional = inner.IsOptional;
            SelectedDisplayParts = inner.SelectedDisplayParts;
        }
    }
}