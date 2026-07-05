using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data
{
    /// <summary>
    /// Wraps <see cref="CompletionItem"/> with information about highlighted parts of its <see cref="CompletionItem.DisplayText"/>.
    /// </summary>
    [DebuggerDisplay("{CompletionItem}")]
    public struct CompletionItemWithHighlight : IEquatable<CompletionItemWithHighlight>
    {
        /// <summary>
        /// The completion item
        /// </summary>
        public CompletionItem CompletionItem { get; }

        /// <summary>
        /// Which parts of <see cref="CompletionItem.DisplayText"/> to highlight
        /// </summary>
        public ImmutableArray<Span> HighlightedSpans { get; }

        /// <summary>
        /// Constructs <see cref="CompletionItemWithHighlight"/> without any highlighting.
        /// Used when the <see cref="CompletionItem"/> appears in the completion list without being a text match.
        /// </summary>
        /// <param name="completionItem">Instance of the <see cref="CompletionItem"/></param>
        public CompletionItemWithHighlight(CompletionItem completionItem)
            : this (completionItem, ImmutableArray<Span>.Empty)
        {
        }

        /// <summary>
        /// Constructs <see cref="CompletionItemWithHighlight"/> with given highlighting.
        /// Used when text used to filter the completion list can be found in the <see cref="CompletionItem.DisplayText"/>.
        /// </summary>
        /// <param name="completionItem">Instance of the <see cref="CompletionItem"/></param>
        /// <param name="highlightedSpans"><see cref="Span"/>s of <see cref="CompletionItem.DisplayText"/> to highlight</param>
        public CompletionItemWithHighlight(CompletionItem completionItem, ImmutableArray<Span> highlightedSpans)
        {
            CompletionItem = completionItem ?? throw new ArgumentNullException(nameof(completionItem));
            if (highlightedSpans.IsDefault)
                throw new ArgumentException("Array must be initialized", nameof(highlightedSpans));

            HighlightedSpans = highlightedSpans;
        }

        bool IEquatable<CompletionItemWithHighlight>.Equals(CompletionItemWithHighlight other)
            => CompletionItem != null && CompletionItem.Equals(other.CompletionItem) && HighlightedSpans.Equals(other.HighlightedSpans);

        public override bool Equals(object other) => (other is CompletionItemWithHighlight otherItem) ? ((IEquatable<CompletionItemWithHighlight>)this).Equals(otherItem) : false;

        public static bool operator ==(CompletionItemWithHighlight left, CompletionItemWithHighlight right) => left.Equals(right);

        public static bool operator !=(CompletionItemWithHighlight left, CompletionItemWithHighlight right) => !(left == right);

        /// <summary>
        /// Assumption: We won't see two instances of <see cref="CompletionItemWithHighlight"/> with same <see cref="CompletionItem"/> but different highlighting.
        /// Therefore, we don't calculate hash code for the highlights.
        /// </summary>
        /// <returns><see cref="CompletionItem.GetHashCode"/></returns>
        public override int GetHashCode() => CompletionItem.GetHashCode();
    }
}
