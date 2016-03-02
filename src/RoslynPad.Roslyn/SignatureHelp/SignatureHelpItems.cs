using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Text;

namespace RoslynPad.Roslyn.SignatureHelp
{
    public class SignatureHelpItems
    {
        public IList<SignatureHelpItem> Items { get; }

        public TextSpan ApplicableSpan { get; }

        public int ArgumentIndex { get; }

        public int ArgumentCount { get; }

        public string ArgumentName { get; }

        public int? SelectedItemIndex { get; }

        internal SignatureHelpItems(Microsoft.CodeAnalysis.Editor.SignatureHelpItems inner)
        {
            Items = inner.Items.Select(x => new SignatureHelpItem(x)).ToArray();
            ApplicableSpan = inner.ApplicableSpan;
            ArgumentIndex = inner.ArgumentIndex;
            ArgumentCount = inner.ArgumentCount;
            ArgumentName = inner.ArgumentName;
            SelectedItemIndex = inner.SelectedItemIndex;
        }
    }
}