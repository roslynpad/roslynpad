using System;
using System.Collections.Immutable;
using System.Linq;

namespace Microsoft.VisualStudio.Text.PatternMatching
{
#pragma warning disable CA1815 // Override equals and operator equals on value types
#pragma warning disable CA1036 // Override methods on comparable types
    public struct PatternMatch : IComparable<PatternMatch>
#pragma warning restore CA1036 // Override methods on comparable types
#pragma warning restore CA1815 // Override equals and operator equals on value types
    {
        /// <summary>
        /// True if this was a case sensitive match.
        /// </summary>
        public bool IsCaseSensitive { get; }

        /// <summary>
        /// The type of match that occurred.
        /// </summary>
        public PatternMatchKind Kind { get; }

        /// <summary>
        /// The spans in the original text that were matched.  Only returned if the 
        /// pattern matcher is asked to collect these spans.
        /// </summary>
        public ImmutableArray<Span> MatchedSpans { get; }

        /// <summary>
        /// True if punctuation was removed for this match.
        /// </summary>
        public bool IsPunctuationStripped { get; }

        /// <summary>
        /// Creates a PatternMatch object with an optional single span.
        /// </summary>
        /// <param name="resultType">How is this match categorized?</param>
        /// <param name="punctuationStripped">Was punctuation removed?</param>
        /// <param name="isCaseSensitive">Was this a case sensitive match?</param>
        public PatternMatch(
            PatternMatchKind resultType,
            bool punctuationStripped,
            bool isCaseSensitive)
            : this(resultType, punctuationStripped, isCaseSensitive, ImmutableArray<Span>.Empty)
        {
        }

        /// <summary>
        /// Creates a PatternMatch object with a set of spans
        /// </summary>
        /// <param name="resultType">How is this match categorized?</param>
        /// <param name="punctuationStripped">Was punctuation removed?</param>
        /// <param name="isCaseSensitive">Was this a case sensitive match?</param>
        /// <param name="matchedSpans">What spans of the candidate were matched? An empty array signifies no span information is given.</param>
        public PatternMatch(
            PatternMatchKind resultType,
            bool punctuationStripped,
            bool isCaseSensitive,
            ImmutableArray<Span> matchedSpans)
            : this()
        {
            this.Kind = resultType;
            this.IsCaseSensitive = isCaseSensitive;
            this.MatchedSpans = matchedSpans;
            this.IsPunctuationStripped = punctuationStripped;
        }

        /// <summary>
        /// Get a PatternMatch object with additional spans added to it. This is an optimization to avoid having to call the whole constructor.
        /// </summary>
        /// <param name="matchedSpans">Spans to associate with this PatternMatch.</param>
        /// <returns>A new instance of a PatternMatch with the specified spans.</returns>
        public PatternMatch WithMatchedSpans(ImmutableArray<Span> matchedSpans)
            => new PatternMatch(Kind, IsPunctuationStripped, IsCaseSensitive, matchedSpans);

        /// <summary>
        /// Compares two PatternMatch objects.
        /// </summary>
        public int CompareTo(PatternMatch other)
            => CompareTo(other, ignoreCase: false);

        /// <summary>
        /// Compares two PatternMatch objects with the specified behavior for ignoring capitalization.
        /// </summary>
        /// <param name="ignoreCase">Should case be ignored?</param>
        public int CompareTo(PatternMatch other, bool ignoreCase)
        {
            int diff;
            if ((diff = CompareType(this, other)) != 0 ||
                (diff = CompareCase(this, other, ignoreCase)) != 0 ||
                (diff = ComparePunctuation(this, other)) != 0)
            {
                return diff;
            }

            return 0;
        }

        private static int ComparePunctuation(PatternMatch result1, PatternMatch result2)
        {
            // Consider a match to be better if it was successful without stripping punctuation
            // versus a match that had to strip punctuation to succeed.
            if (result1.IsPunctuationStripped != result2.IsPunctuationStripped)
            {
                return result1.IsPunctuationStripped ? 1 : -1;
            }

            return 0;
        }

        private static int CompareCase(PatternMatch result1, PatternMatch result2, bool ignoreCase)
        {
            if (!ignoreCase)
            {
                if (result1.IsCaseSensitive != result2.IsCaseSensitive)
                {
                    return result1.IsCaseSensitive ? -1 : 1;
                }
            }

            return 0;
        }

        private static int CompareType(PatternMatch result1, PatternMatch result2)
            => result1.Kind.CompareTo(result2.Kind);
    }
}
