using Microsoft.VisualStudio.Utilities;
using System.Collections.Immutable;

namespace Microsoft.VisualStudio.Text.PatternMatching.Implementation
{
    internal static class PatternMatchExtensions
    {
        private static ImmutableArray<Span> MergeSpans(PatternMatch match1, PatternMatch match2)
        {
            var collection1 = new NormalizedSpanCollection(match1.MatchedSpans);
            var collection2 = new NormalizedSpanCollection(match2.MatchedSpans);

            var builder = ArrayBuilder<Span>.GetInstance();
            builder.AddRange(NormalizedSpanCollection.Union(collection1, (collection2)));
            return builder.ToImmutable();
        }

        public static PatternMatch Merge(this PatternMatch match1, PatternMatch match2, PatternMatchMergeStrategy strategy)
        {
            PatternMatchKind kind;
            if (strategy == PatternMatchMergeStrategy.Simple)
            {
                // Do some intelligent merging, since both matches came from the same string.
                kind = MergeMatchKinds(match1.Kind, match2.Kind);
            }
            else
            {
                // Give the worst kind of match, since these are relating to different containers.
                kind = match1.Kind.CompareTo(match2.Kind) > 0 ? match1.Kind : match2.Kind;
            }

            // Give punctuation stripped if either has it stripped
            var punctuation = match1.IsPunctuationStripped || match2.IsPunctuationStripped;

            // Give case sensitivity only if both are case sensitive
            var caseSensitive = match1.IsCaseSensitive && match2.IsCaseSensitive;

            // Give spans from both
            var spans = MergeSpans(match1, match2);

            return new PatternMatch(kind, punctuation, caseSensitive, spans);
        }

        private static PatternMatchKind MergeMatchKinds(PatternMatchKind kind1, PatternMatchKind kind2)
        {
            // Ensure ordering for simpler processing below.
            if (kind2 < kind1)
            {
                return MergeMatchKinds(kind2, kind1);
            }

            // Guess the match kind. Unfortunately we can't assume contiguity. It is hard to guess when dealing with Exact, Fuzzy,
            // and multiple prefixes, so assume that if you have an exact match, the other merged thing must be redundant.
            // The only exception is fuzzy (which degrades everything) since the match obviously missed somehow.
            //
            // Kinds:
            // Exact (E), Prefix (P), Substring (S), CamelCaseExact (CE), CamelCasePrefix (CP), CamelCaseNonContiguousPrefix (NP)
            // CamelCaseSubstring (CS), CamelCaseNonContiguousSubstring(NS ), Fuzzy (F)
            //
            // Mapping:
            // In |E  |P  |S  |CE |CP |NP |CS |NS |F
            //     ___________________________________
            // E  |E   E   E   E   E   E   E   E   F
            // P  |    P   NP  CE  CP  NP  NP  NP  F
            // S  |        NS  CE  NP  NP  NS  NS  F
            // CE |            CE  CE  CE  CE  CE  F
            // CP |                CP  NP  NP  NP  F
            // NP |                    NP  NP  NP  F
            // CS |                        NS  NS  F
            // NS |                            NS  F
            // F  |                                F
            //
            // See PatternMatchingUnitTests.MatchMultiWordPatterns_MatchKindMerge for examples. There's a bunch.

            if (kind1 == PatternMatchKind.Fuzzy || kind2 == PatternMatchKind.Fuzzy)
            {
                return PatternMatchKind.Fuzzy;
            }
            else if (kind1 == PatternMatchKind.Exact || kind2 == PatternMatchKind.Exact)
            {
                return PatternMatchKind.Exact;
            }
            else if (kind1 == PatternMatchKind.CamelCaseExact || kind2 == PatternMatchKind.CamelCaseExact)
            {
                return PatternMatchKind.CamelCaseExact;
            }
            else if (kind1 == PatternMatchKind.CamelCaseNonContiguousPrefix || kind2 == PatternMatchKind.CamelCaseNonContiguousPrefix)
            {
                return PatternMatchKind.CamelCaseNonContiguousPrefix;
            }
            else
            {
                // Ok, we have to actually think about this one. We can assume that kind1 <= kind2 here.
                //
                //Reduced manifest from above:
                // In |P  |S  |CP |CS |NS 
                //     ____________________
                // P  |P   NP  CP  NP  NP  
                // S  |    NS  NP  NS  NS  
                // CP |        CP  NP  NP  
                // CS |            NS  NS  
                // NS |                NS  
                switch (kind1)
                {
                    case PatternMatchKind.Prefix:
                        if (kind2 == PatternMatchKind.Prefix)
                        {
                            return PatternMatchKind.Prefix;
                        }
                        else if (kind2 == PatternMatchKind.CamelCasePrefix)
                        {
                            return PatternMatchKind.CamelCasePrefix;
                        }
                        return PatternMatchKind.CamelCaseNonContiguousPrefix;
                    case PatternMatchKind.Substring:
                        if (kind2 == PatternMatchKind.CamelCasePrefix)
                        {
                            return PatternMatchKind.CamelCaseNonContiguousPrefix;
                        }
                        return PatternMatchKind.CamelCaseNonContiguousSubstring;
                    case PatternMatchKind.CamelCasePrefix:
                        if (kind2 == PatternMatchKind.CamelCasePrefix)
                        {
                            return PatternMatchKind.CamelCasePrefix;
                        }
                        return PatternMatchKind.CamelCaseNonContiguousPrefix;
                    default:
                        // Handles CamelCaseSubstring and CamelCaseNonContiguousSubstring
                        return PatternMatchKind.CamelCaseNonContiguousSubstring;
                }
            }
        }
    }
}
