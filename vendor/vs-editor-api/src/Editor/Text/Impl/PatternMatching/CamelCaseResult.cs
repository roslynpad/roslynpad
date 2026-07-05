// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.VisualStudio.Utilities;
using TextSpan = Microsoft.VisualStudio.Text.Span;

namespace Microsoft.VisualStudio.Text.PatternMatching.Implementation
{
    internal partial class PatternMatcher
    {
        private struct CamelCaseResult
        {
            public readonly bool FromStart;
            public readonly bool Contiguous;
            public readonly bool ToEnd;
            public readonly int MatchCount;
            public readonly ArrayBuilder<TextSpan> MatchedSpansInReverse;
            public readonly int ChunkOffset;

            public CamelCaseResult(bool fromStart, bool contiguous, bool toEnd, int matchCount, ArrayBuilder<TextSpan> matchedSpansInReverse, int chunkOffset)
            {
                FromStart = fromStart;
                Contiguous = contiguous;
                ToEnd = toEnd;
                MatchCount = matchCount;
                MatchedSpansInReverse = matchedSpansInReverse;
                ChunkOffset = chunkOffset;

                Debug.Assert(matchedSpansInReverse == null || matchedSpansInReverse.Count == matchCount);
            }

            public void Free()
            {
                MatchedSpansInReverse?.Free();
            }

            public CamelCaseResult WithFromStart(bool fromStart)
                => new CamelCaseResult(fromStart, Contiguous, ToEnd, MatchCount, MatchedSpansInReverse, ChunkOffset);

            public CamelCaseResult WithToEnd(bool toEnd)
                => new CamelCaseResult(FromStart, Contiguous, toEnd, MatchCount, MatchedSpansInReverse, ChunkOffset);

            public CamelCaseResult WithAddedMatchedSpan(TextSpan value)
            {
                MatchedSpansInReverse?.Add(value);
                return new CamelCaseResult(FromStart, Contiguous, ToEnd, MatchCount + 1, MatchedSpansInReverse, ChunkOffset);
            }
        }

        private static PatternMatchKind GetCamelCaseKind(CamelCaseResult result)
        {
            /* CamelCase PatternMatchKind truth table:
             * | FromStart | ToEnd | Contiguous || PatternMatchKind                |
             * | True      | True  | True       || CamelCaseExact                  |
             * | True      | True  | False      || CamelCaseNonContiguousPrefix    |
             * | True      | False | True       || CamelCasePrefix                 |
             * | True      | False | False      || CamelCaseNonContiguousPrefix    |
             * | False     | True  | True       || CamelCaseSubstring              |
             * | False     | True  | False      || CamelCaseNonContiguousSubstring |
             * | False     | False | True       || CamelCaseSubstring              |
             * | False     | False | False      || CamelCaseNonContiguousSubstring |
             */

            if (result.FromStart)
            {
                if (result.Contiguous)
                {
                    // We contiguously matched humps from the start of this candidate.  If we 
                    // matched all the humps, then this was an exact match, otherwise it was a 
                    // contiguous prefix match
                    return result.ToEnd
                        ? PatternMatchKind.CamelCaseExact
                        : PatternMatchKind.CamelCasePrefix;
                }
                else
                {
                    return PatternMatchKind.CamelCaseNonContiguousPrefix;
                }
            }
            else
            {
                // We didn't match from the start.  Distinguish between a match whose humps are all
                // contiguous, and one that isn't.
                return result.Contiguous
                    ? PatternMatchKind.CamelCaseSubstring
                    : PatternMatchKind.CamelCaseNonContiguousSubstring;
            }
        }
    }
}
