using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace RoslynPad.Roslyn.Completion
{
    [DebuggerDisplay("{DisplayText}")]
    public class CompletionItem : IComparable<CompletionItem>
    {
        internal Microsoft.CodeAnalysis.Completion.CompletionItem Inner { get; set; }

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

        internal CompletionItem(Microsoft.CodeAnalysis.Completion.CompletionItem inner)
        {
            Inner = inner;
            Glyph = (Glyph?)inner.Glyph;
            DisplayText = inner.DisplayText;
            FilterText = inner.FilterText;
            SortText = inner.SortText;
            Preselect = inner.Preselect;
            Rules = new CompletionItemRules(inner.Rules);
            FilterSpan = inner.FilterSpan;
            IsBuilder = inner.IsBuilder;
            ShowsWarningIcon = inner.ShowsWarningIcon;
            ShouldFormatOnCommit = inner.ShouldFormatOnCommit;
        }

        public Task<ImmutableArray<SymbolDisplayPart>> GetDescriptionAsync(
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return Inner.GetDescriptionAsync(cancellationToken);
        }

        public int CompareTo(CompletionItem other)
        {
            return ((IComparable<Microsoft.CodeAnalysis.Completion.CompletionItem>)Inner).CompareTo(other.Inner);
        }
    }
}