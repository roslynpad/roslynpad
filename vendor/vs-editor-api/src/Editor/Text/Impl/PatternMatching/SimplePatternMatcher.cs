// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Globalization;
using Microsoft.VisualStudio.Utilities;
using TextSpan = Microsoft.VisualStudio.Text.Span;

namespace Microsoft.VisualStudio.Text.PatternMatching.Implementation
{
    internal partial class PatternMatcher
    {
        private sealed partial class SimplePatternMatcher : PatternMatcher
        {
            private readonly PatternSegment _fullPatternSegment;

            public SimplePatternMatcher(
                string pattern,
                CultureInfo culture,
                bool includeMatchedSpans,
                bool allowFuzzyMatching,
                bool allowSimpleSubstringMatching = false,
                PatternMatcher linkedMatcher = null)
                : base(includeMatchedSpans, culture, allowFuzzyMatching, allowSimpleSubstringMatching, linkedMatcher)
            {
                pattern = pattern.Trim();

                _fullPatternSegment = new PatternSegment(pattern, allowFuzzyMatching);
                HasInvalidPattern = _fullPatternSegment.IsInvalid;
            }

            public override void Dispose()
            {
                base.Dispose();
                _fullPatternSegment.Dispose();
            }

            /// <summary>
            /// Determines if a given candidate string matches under a multiple word query text, as you
            /// would find in features like Navigate To.
            /// </summary>
            /// <returns>If this was a match, a set of match types that occurred while matching the
            /// patterns. If it was not a match, it returns null.</returns>
            public override PatternMatch? TryMatch(string candidate)
            {
                if (SkipMatch(candidate))
                {
                    return null;
                }

                var match = MatchPatternSegment(candidate, _fullPatternSegment, fuzzyMatch: false, segmentOffset: 0);

                if (!match.HasValue && _allowFuzzyMatching)
                {
                    match = MatchPatternSegment(candidate, _fullPatternSegment, fuzzyMatch: true, segmentOffset: 0);
                }

                return match;
            }
        }
    }
}
