using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using RoslynPad.Utilities;

namespace RoslynPad.Roslyn.Completion
{
    [DebuggerDisplay("{DisplayText}")]
    public class CompletionItem : IComparable<CompletionItem>
    {
        internal object Inner { get; set; }

        public Glyph? Glyph { get; }

        public string DisplayText { get; }

        public string FilterText { get; }

        public string SortText { get; }

        public bool Preselect { get; }

        public TextSpan FilterSpan { get; }

        public bool IsBuilder { get; }

        public CompletionItemRules Rules { get; }

        public bool ShowsWarningIcon { get; }

        public bool ShouldFormatOnCommit { get; }

        internal CompletionItem(object inner)
        {
            Inner = inner;
            Glyph = (Glyph)inner.GetPropertyValue<int>(nameof(Glyph));
            DisplayText = inner.GetPropertyValue<string>(nameof(DisplayText));
            FilterText = inner.GetPropertyValue<string>(nameof(FilterText));
            SortText = inner.GetPropertyValue<string>(nameof(SortText));
            Preselect = inner.GetPropertyValue<bool>(nameof(Preselect));
            Rules = new CompletionItemRules(inner.GetPropertyValue<object>(nameof(Rules)));
            FilterSpan = inner.GetPropertyValue<TextSpan>(nameof(FilterSpan));
            IsBuilder = inner.GetPropertyValue<bool>(nameof(IsBuilder));
            ShowsWarningIcon = inner.GetPropertyValue<bool>(nameof(ShowsWarningIcon));
            ShouldFormatOnCommit = inner.GetPropertyValue<bool>(nameof(ShouldFormatOnCommit));
        }

        public virtual Task<ImmutableArray<SymbolDisplayPart>> GetDescriptionAsync(
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return (Task<ImmutableArray<SymbolDisplayPart>>)Inner.GetType().GetMethod(nameof(GetDescriptionAsync))
                .Invoke(Inner, new object[] { cancellationToken });
        }

        public int CompareTo(CompletionItem other)
        {
            var result = StringComparer.OrdinalIgnoreCase.Compare(SortText, other.SortText);
            if (result == 0)
            {
                result = StringComparer.OrdinalIgnoreCase.Compare(DisplayText, other.DisplayText);
            }
            return result;
        }
    }
}