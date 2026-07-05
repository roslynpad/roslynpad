// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Microsoft.VisualStudio.Text.PatternMatching.Implementation
{
    internal partial class PatternMatcher
    {
        private sealed partial class ContainerPatternMatcher : PatternMatcher
        {
            private readonly PatternSegment[] _patternSegments;
            private readonly char[] _containerSplitCharacters;

            /// <summary>
            /// Creates a new ContainerPatternMatcher.
            /// </summary>
            /// <param name="patternParts">The pattern itself needs to be split up, to match against candidates. Suppose the user searches for Apple.Banana.Charlie using A.B.C,
            /// the compiler recognizes that these are namespaces, splits the pattern up and passes in  { "A", "B", "C" }.</param>
            /// <param name="containerSplitCharacters">What characters should candidates be split on. In the above example, it would be { '.' }</param>
            /// <param name="culture">Important for some string operations.</param>
            /// <param name="allowFuzzyMatching">Do we tolerate mis-spellings?</param>
            /// <param name="allowSimpleSubstringMatching">Does a match not at a camel-case boundary count? (e.g. Does AppleBanana match 'ppl' as a search string?</param>
            /// <param name="includeMatchedSpans">Do we want to get spans back (performance impacting).</param>
            public ContainerPatternMatcher(
                string[] patternParts, IReadOnlyCollection<char> containerSplitCharacters,
                CultureInfo culture,
                bool allowFuzzyMatching = false,
                bool allowSimpleSubstringMatching = false,
                bool includeMatchedSpans = false,
                PatternMatcher linkedMatcher = null)
                : base(includeMatchedSpans, culture, allowFuzzyMatching, allowSimpleSubstringMatching, linkedMatcher)
            {
                _containerSplitCharacters = containerSplitCharacters.ToArray();

                _patternSegments = patternParts
                    .Select(text => new PatternSegment(text.Trim(), allowFuzzyMatching: allowFuzzyMatching))
                    .ToArray();

                HasInvalidPattern = _patternSegments.Length == 0 || _patternSegments.Any(s => s.IsInvalid);
            }

#pragma warning disable CA1063
            public override void Dispose()
            {
                base.Dispose();

                foreach (var segment in _patternSegments)
                {
                    segment.Dispose();
                }
            }
#pragma warning restore CA1063

            public override PatternMatch? TryMatch(string candidate)
            {
                if (SkipMatch(candidate))
                {
                    return null;
                }

                var match = TryMatch(candidate, fuzzyMatch: false);
                if (!match.HasValue)
                {
                    match = TryMatch(candidate, fuzzyMatch: true);
                }

                return match;
            }

            private PatternMatch? TryMatch(string candidate, bool fuzzyMatch)
            {
                if (fuzzyMatch && !_allowFuzzyMatching)
                {
                    return null;
                }

                var containerParts = candidate.Split(_containerSplitCharacters, StringSplitOptions.RemoveEmptyEntries);
                var patternSegmentCount = _patternSegments.Length;
                var containerPartCount = containerParts.Length;

                if (patternSegmentCount > containerPartCount)
                {
                    // There weren't enough container parts to match against the pattern parts.
                    // So this definitely doesn't match.
                    return null;
                }

                // So far so good.  Now break up the container for the candidate and check if all
                // the dotted parts match up correctly.

                PatternMatch? match = null, result = null;

                for (int i = patternSegmentCount - 1, j = containerPartCount - 1;
                     i >= 0;
                     i--, j--)
                {
                    var segment = _patternSegments[i];
                    var containerName = containerParts[j];

                    // Add up the lengths of all the container parts before this one, as well is the split characters that were removed.
                    int containerOffset = j;
                    for (int k = 0; k < j; k++)
                        containerOffset += containerParts[k].Length;

                    result = MatchPatternSegment(containerName, segment, fuzzyMatch, containerOffset);
                    if (!result.HasValue)
                    {
                        // This container didn't match the pattern piece.  So there's no match at all.
                        return null;
                    }

                    match = match?.Merge(result.Value, PatternMatchMergeStrategy.Container) ?? result;
                }

                return match;
            }
        }
    }
}
