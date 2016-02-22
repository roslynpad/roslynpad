using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Text;
using RoslynPad.Utilities;

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

        internal SignatureHelpItems(object inner)
        {
            Items = inner.GetPropertyValue<IEnumerable<object>>(nameof(Items)).Select(x => new SignatureHelpItem(x)).ToArray();
            ApplicableSpan = inner.GetPropertyValue<TextSpan>(nameof(ApplicableSpan));
            ArgumentIndex = inner.GetPropertyValue<int>(nameof(ArgumentIndex));
            ArgumentCount = inner.GetPropertyValue<int>(nameof(ArgumentCount));
            ArgumentName = inner.GetPropertyValue<string>(nameof(ArgumentName));
            SelectedItemIndex = inner.GetPropertyValue<int?>(nameof(SelectedItemIndex));
        }
    }
}