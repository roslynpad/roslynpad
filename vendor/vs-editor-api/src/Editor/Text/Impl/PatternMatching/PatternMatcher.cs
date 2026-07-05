// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.Contracts;
using System.Globalization;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using TextSpan = Microsoft.VisualStudio.Text.Span;

namespace Microsoft.VisualStudio.Text.PatternMatching.Implementation
{
    /// <summary>
    /// The pattern matcher is thread-safe.  However, it maintains an internal cache of
    /// information as it is used.  Therefore, you should not keep it around forever and should get
    /// and release the matcher appropriately once you no longer need it.
    /// Also, while the pattern matcher is culture aware, it uses the culture specified in the
    /// constructor.
    /// </summary>
    internal abstract partial class PatternMatcher : IDisposable, IPatternMatcher
    {
        public const int NoBonus = 0;
        public const int CamelCaseContiguousBonus = 1;
        public const int CamelCaseMatchesFromStartBonus = 2;
        public const int CamelCaseMaxWeight = CamelCaseContiguousBonus + CamelCaseMatchesFromStartBonus;

        private readonly object _gate;

        private readonly bool _includeMatchedSpans;
        private readonly bool _allowFuzzyMatching;
        private readonly bool _allowSimpleSubstringMatching;

        private readonly Dictionary<string, StringBreaks> _stringToWordSpans;
        private static readonly Func<string, StringBreaks> _breakIntoWordSpans = StringBreaker.BreakIntoWordParts;

        // PERF: Cache the culture's compareInfo to avoid the overhead of asking for them repeatedly in inner loops
        private readonly CompareInfo _compareInfo;

        public bool HasInvalidPattern { get; private set; }

        /// <summary>
        /// Construct a new PatternMatcher using the specified culture.
        /// </summary>
        /// <param name="culture">The culture to use for string searching and comparison.</param>
        /// <param name="includeMatchedSpans">Whether or not the matching parts of the candidate should be supplied in results.</param>
        /// <param name="allowFuzzyMatching">Whether or not close matches should count as matches.</param>
        protected PatternMatcher(
            bool includeMatchedSpans,
            CultureInfo culture,
            bool allowFuzzyMatching = false,
            bool allowSimpleSubstringMatching = false,
            PatternMatcher linkedMatcher = null)
        {
            culture = culture ?? CultureInfo.CurrentCulture;
            _compareInfo = culture.CompareInfo;
            _includeMatchedSpans = includeMatchedSpans;
            _allowFuzzyMatching = allowFuzzyMatching;
            _allowSimpleSubstringMatching = allowSimpleSubstringMatching;
            _stringToWordSpans = linkedMatcher?._stringToWordSpans ?? new Dictionary<string, StringBreaks>();
            _gate = linkedMatcher?._gate ?? new object();
        }

#pragma warning disable CA1063
        public virtual void Dispose()
#pragma warning restore CA1063
        {
            // Disposing this pattern matcher will dispose any linked matchers as well.
            lock (_gate)
            {
                foreach (var kvp in _stringToWordSpans)
                {
                    kvp.Value.Dispose();
                }

                _stringToWordSpans.Clear();
            }
        }

        public static PatternMatcher CreateSimplePatternMatcher(
            string pattern,
            CultureInfo culture = null,
            bool includeMatchedSpans = false,
            bool allowFuzzyMatching = false,
            bool allowSimpleSubstringMatching = false,
            PatternMatcher linkedMatcher = null)
        {
            return new SimplePatternMatcher(
                pattern,
                culture,
                includeMatchedSpans,
                allowFuzzyMatching,
                allowSimpleSubstringMatching,
                linkedMatcher);
        }

        internal static PatternMatcher CreateContainerPatternMatcher(
            string[] patternParts,
            IReadOnlyCollection<char> containerSplitCharacters,
            CultureInfo culture = null,
            bool allowFuzzyMatching = false,
            bool allowSimpleSubstringMatching = false,
            bool includeMatchedSpans = false,
            PatternMatcher linkedMatcher = null)
        {
            return new ContainerPatternMatcher(
                patternParts,
                containerSplitCharacters,
                culture,
                allowFuzzyMatching,
                allowSimpleSubstringMatching,
                includeMatchedSpans,
                linkedMatcher);
        }

        internal static (string name, string containerOpt) GetNameAndContainer(string pattern)
        {
            var dotIndex = pattern.LastIndexOf('.');
            var containsDots = dotIndex >= 0;
            return containsDots
                ? (name: pattern.Substring(dotIndex + 1), containerOpt: pattern.Substring(0, dotIndex))
                : (name: pattern, containerOpt: null);
        }

        public abstract PatternMatch? TryMatch(string candidate);

        private bool SkipMatch(string candidate)
            => HasInvalidPattern || string.IsNullOrWhiteSpace(candidate);

        private StringBreaks GetWordSpans(string word)
        {
            lock (_gate)
            {
                return _stringToWordSpans.GetOrAdd(word, _breakIntoWordSpans);
            }
        }

        private static bool ContainsUpperCaseLetter(string pattern)
        {
            // Expansion of "foreach(char ch in pattern)" to avoid a CharEnumerator allocation
            for (int i = 0; i < pattern.Length; i++)
            {
                if (char.IsUpper(pattern[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private PatternMatch? MatchPatternChunk(
            string candidate,
            TextChunk patternChunk,
            bool punctuationStripped,
            bool fuzzyMatch,
            int chunkOffset)
        {
            return fuzzyMatch
                ? FuzzyMatchPatternChunk(candidate, patternChunk, punctuationStripped)
                : NonFuzzyMatchPatternChunk(candidate, patternChunk, punctuationStripped, chunkOffset);
        }

        private static PatternMatch? FuzzyMatchPatternChunk(
            string candidate,
            TextChunk patternChunk,
            bool punctuationStripped)
        {
            if (patternChunk.SimilarityChecker.AreSimilar(candidate))
            {
                return new PatternMatch(
                    PatternMatchKind.Fuzzy, punctuationStripped, isCaseSensitive: false);
            }

            return null;
        }

        private PatternMatch? NonFuzzyMatchPatternChunk(
            string candidate,
            TextChunk patternChunk,
            bool punctuationStripped,
            int chunkOffset)
        {
            int caseInsensitiveIndex = _compareInfo.IndexOf(candidate, patternChunk.Text, CompareOptions.IgnoreCase);

            if (caseInsensitiveIndex == 0)
            {
                if (patternChunk.Text.Length == candidate.Length)
                {
                    // a) Check if the part matches the candidate entirely, in an case insensitive or
                    //    sensitive manner.  If it does, return that there was an exact match.
                    return new PatternMatch(
                        PatternMatchKind.Exact, punctuationStripped, isCaseSensitive: string.Equals(candidate, patternChunk.Text, StringComparison.Ordinal),
                        matchedSpans: GetMatchedSpans(chunkOffset, candidate.Length));
                }
                else
                {
                    // b) Check if the part is a prefix of the candidate, in a case insensitive or sensitive
                    //    manner.  If it does, return that there was a prefix match.
                    return new PatternMatch(
                        PatternMatchKind.Prefix, punctuationStripped, isCaseSensitive: _compareInfo.IsPrefix(candidate, patternChunk.Text),
                        matchedSpans: GetMatchedSpans(chunkOffset, patternChunk.Text.Length));
                }
            }
            // b++) If the part is a case insensitive substring match, but not a prefix, and the caller
            // requested simple substring matches, return that there was a substring match.
            // This covers the case of non camel case naming conventions, for example matching
            // 'afxsettingsstore.h' when user types 'store.h'
            else if (caseInsensitiveIndex > 0  && _allowSimpleSubstringMatching)
            {
                return new PatternMatch(
                    PatternMatchKind.Substring, punctuationStripped,
                    isCaseSensitive: PartStartsWith(
                        candidate, new TextSpan(caseInsensitiveIndex, patternChunk.Text.Length),
                        patternChunk.Text, CompareOptions.None),
                    matchedSpans: GetMatchedSpans(chunkOffset + caseInsensitiveIndex, patternChunk.Text.Length));
            }

            var isLowercase = !ContainsUpperCaseLetter(patternChunk.Text);
            if (isLowercase)
            {
                if (caseInsensitiveIndex > 0)
                {
                    // c) If the part is entirely lowercase, then check if it is contained anywhere in the
                    //    candidate in a case insensitive manner.  If so, return that there was a substring
                    //    match. 
                    //
                    //    Note: We only have a substring match if the lowercase part is prefix match of some
                    //    word part. That way we don't match something like 'Class' when the user types 'a'.
                    //    But we would match 'FooAttribute' (since 'Attribute' starts with 'a').
                    //
                    //    Also, if we matched at location right after punctuation, then this is a good
                    //    substring match.  i.e. if the user is testing mybutton against _myButton
                    //    then this should hit. As we really are finding the match at the beginning of 
                    //    a word.
                    if (char.IsPunctuation(candidate[caseInsensitiveIndex - 1]) ||
                        char.IsPunctuation(patternChunk.Text[0]))
                    {
                        return new PatternMatch(
                            PatternMatchKind.Substring, punctuationStripped,
                            isCaseSensitive: PartStartsWith(
                                candidate, new TextSpan(caseInsensitiveIndex, patternChunk.Text.Length),
                                patternChunk.Text, CompareOptions.None),
                            matchedSpans: GetMatchedSpans(chunkOffset + caseInsensitiveIndex, patternChunk.Text.Length));
                    }

                    var wordSpans = GetWordSpans(candidate);
                    for (int i = 0, n = wordSpans.GetCount(); i < n; i++)
                    {
                        var span = wordSpans[i];
                        if (PartStartsWith(candidate, span, patternChunk.Text, CompareOptions.IgnoreCase))
                        {
                            return new PatternMatch(PatternMatchKind.Substring, punctuationStripped,
                                isCaseSensitive: PartStartsWith(candidate, span, patternChunk.Text, CompareOptions.None),
                                matchedSpans: GetMatchedSpans(chunkOffset + span.Start, patternChunk.Text.Length));
                        }
                    }
                }
            }
            else
            {
                // d) If the part was not entirely lowercase, then check if it is contained in the
                //    candidate in a case *sensitive* manner. If so, return that there was a substring
                //    match.
                var caseSensitiveIndex = _compareInfo.IndexOf(candidate, patternChunk.Text);
                if (caseSensitiveIndex > 0)
                {
                    return new PatternMatch(
                        PatternMatchKind.Substring, punctuationStripped, isCaseSensitive: true,
                        matchedSpans: GetMatchedSpans(chunkOffset + caseSensitiveIndex, patternChunk.Text.Length));
                }
            }

            var match = TryCamelCaseMatch(
                candidate, patternChunk, punctuationStripped, isLowercase, chunkOffset);
            if (match.HasValue)
            {
                return match.Value;
            }

            if (isLowercase)
            {
                //   g) The word is all lower case. Is it a case insensitive substring of the candidate
                //      starting on a part boundary of the candidate?

                // We could check every character boundary start of the candidate for the pattern. 
                // However, that's an m * n operation in the worst case. Instead, find the first 
                // instance of the pattern  substring, and see if it starts on a capital letter. 
                // It seems unlikely that the user will try to filter the list based on a substring
                // that starts on a capital letter and also with a lowercase one. (Pattern: fogbar, 
                // Candidate: quuxfogbarFogBar).
                if (patternChunk.Text.Length < candidate.Length)
                {
                    if (caseInsensitiveIndex != -1 && char.IsUpper(candidate[caseInsensitiveIndex]))
                    {
                        return new PatternMatch(
                            PatternMatchKind.Substring, punctuationStripped, isCaseSensitive: false,
                            matchedSpans: GetMatchedSpans(chunkOffset + caseInsensitiveIndex, patternChunk.Text.Length));
                    }
                }
            }

            return null;
        }

        private ImmutableArray<Span> GetMatchedSpans(int start, int length)
            => _includeMatchedSpans ? ImmutableArray.Create(new TextSpan(start, length)) : ImmutableArray<Span>.Empty;

        private static bool ContainsSpaceOrAsterisk(string text)
        {
            for (int i = 0; i < text.Length; i++)
            {
                char ch = text[i];
                if (ch == ' ' || ch == '*')
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Internal helper for MatchPatternInternal
        /// </summary>
        /// <remarks>
        /// PERF: Designed to minimize allocations in common cases.
        /// If there's no match, then null is returned.
        /// If there's a single match, or the caller only wants the first match, then it is returned (as a Nullable)
        /// If there are multiple matches they are merged.
        /// </remarks>
        /// <param name="candidate">The word being tested.</param>
        /// <param name="segment">The segment of the pattern to check against the candidate.</param>
        /// <param name="fuzzyMatch">If a fuzzy match should be performed</param>
        /// <param name="spanOffset">Index of segment[0] in candidate, used to merge matches together.</param>
        /// <returns>Returns a match if found. Otherwise it is null.</returns>
        private PatternMatch? MatchPatternSegment(
            string candidate,
            PatternSegment segment,
            bool fuzzyMatch,
            int segmentOffset)
        {

            if (fuzzyMatch && !_allowFuzzyMatching)
            {
                return null;
            }

            // First check if the segment matches as is.  This is also useful if the segment contains
            // characters we would normally strip when splitting into parts that we also may want to
            // match in the candidate.  For example if the segment is "@int" and the candidate is
            // "@int", then that will show up as an exact match here.
            //
            // Note: if the segment contains a space or an asterisk then we must assume that it's a
            // multi-word segment.
            PatternMatch? match = null;
            if (!ContainsSpaceOrAsterisk(segment.TotalTextChunk.Text))
            {
                match = MatchPatternChunk(
                    candidate, segment.TotalTextChunk, punctuationStripped: false, fuzzyMatch: fuzzyMatch, chunkOffset: segmentOffset);
                if (match != null)
                {
                    return match;
                }
            }

            // The logic for pattern matching is now as follows:
            //
            // 1) Break the segment passed in into words.  Breaking is rather simple and a
            //    good way to think about it that if gives you all the individual alphanumeric words
            //    of the pattern.
            //
            // 2) For each word try to match the word against the candidate value.
            //
            // 3) Matching is as follows:
            //
            //   a) Check if the word matches the candidate entirely, in an case insensitive or
            //    sensitive manner.  If it does, return that there was an exact match.
            //
            //   b) Check if the word is a prefix of the candidate, in a case insensitive or
            //      sensitive manner.  If it does, return that there was a prefix match.
            //
            //   c) If the word is entirely lowercase, then check if it is contained anywhere in the
            //      candidate in a case insensitive manner.  If so, return that there was a substring
            //      match. 
            //
            //      Note: We only have a substring match if the lowercase part is prefix match of
            //      some word part. That way we don't match something like 'Class' when the user
            //      types 'a'. But we would match 'FooAttribute' (since 'Attribute' starts with
            //      'a').
            //
            //   d) If the word was not entirely lowercase, then check if it is contained in the
            //      candidate in a case *sensitive* manner. If so, return that there was a substring
            //      match.
            //
            //   e) If the word was entirely lowercase, then attempt a special lower cased camel cased 
            //      match.  i.e. cofipro would match CodeFixProvider.
            //
            //   f) If the word was not entirely lowercase, then attempt a normal camel cased match.
            //      i.e. CoFiPro would match CodeFixProvider, but CofiPro would not.  
            //
            //   g) The word is all lower case. Is it a case insensitive substring of the candidate starting 
            //      on a part boundary of the candidate?
            //
            // 4) Merge matches back together using the Simple strategy.
            //
            // Only if all words have some sort of match is the pattern considered matched.

            int chunkOffset = segmentOffset;
            var subWordTextChunks = segment.SubWordTextChunks;
            foreach (var subWordTextChunk in subWordTextChunks)
            {
                // Try to match the candidate with this word
                var result = MatchPatternChunk(
                    candidate, subWordTextChunk, punctuationStripped: true, fuzzyMatch: fuzzyMatch, chunkOffset: chunkOffset);
                if (result == null)
                {
                    return null;
                }

                match = match?.Merge(result.Value, PatternMatchMergeStrategy.Simple) ?? result.Value;
            }

            return match;
        }

        private static bool IsWordChar(char ch)
            => char.IsLetterOrDigit(ch) || ch == '_';

        /// <summary>
        /// Do the two 'parts' match? i.e. Does the candidate part start with the pattern part?
        /// </summary>
        /// <param name="candidate">The candidate text</param>
        /// <param name="candidatePart">The span within the <paramref name="candidate"/> text</param>
        /// <param name="pattern">The pattern text</param>
        /// <param name="patternPart">The span within the <paramref name="pattern"/> text</param>
        /// <param name="compareOptions">Options for doing the comparison (case sensitive or not)</param>
        /// <returns>True if the span identified by <paramref name="candidatePart"/> within <paramref name="candidate"/> starts with
        /// the span identified by <paramref name="patternPart"/> within <paramref name="pattern"/>.</returns>
        private bool PartStartsWith(string candidate, TextSpan candidatePart, string pattern, TextSpan patternPart, CompareOptions compareOptions)
        {
            if (patternPart.Length > candidatePart.Length)
            {
                // Pattern part is longer than the candidate part. There can never be a match.
                return false;
            }

            return _compareInfo.Compare(candidate, candidatePart.Start, patternPart.Length, pattern, patternPart.Start, patternPart.Length, compareOptions) == 0;
        }

        /// <summary>
        /// Does the given part start with the given pattern?
        /// </summary>
        /// <param name="candidate">The candidate text</param>
        /// <param name="candidatePart">The span within the <paramref name="candidate"/> text</param>
        /// <param name="pattern">The pattern text</param>
        /// <param name="compareOptions">Options for doing the comparison (case sensitive or not)</param>
        /// <returns>True if the span identified by <paramref name="candidatePart"/> within <paramref name="candidate"/> starts with <paramref name="pattern"/></returns>
        private bool PartStartsWith(string candidate, TextSpan candidatePart, string pattern, CompareOptions compareOptions)
            => PartStartsWith(candidate, candidatePart, pattern, new TextSpan(0, pattern.Length), compareOptions);

        private PatternMatch? TryCamelCaseMatch(
            string candidate, TextChunk patternChunk,
            bool punctuationStripped, bool isLowercase,
            int chunkOffset)
        {
            if (isLowercase)
            {
                //   e) If the word was entirely lowercase, then attempt a special lower cased camel cased 
                //      match.  i.e. cofipro would match CodeFixProvider.
                var candidateParts = GetWordSpans(candidate);
                var camelCaseKind = TryAllLowerCamelCaseMatch(
                    candidate, candidateParts, patternChunk, out var matchedSpans, chunkOffset);
                if (camelCaseKind.HasValue)
                {
                    return new PatternMatch(
                        camelCaseKind.Value, punctuationStripped, isCaseSensitive: false,
                        matchedSpans: matchedSpans);
                }
            }
            else
            {
                //   f) If the word was not entirely lowercase, then attempt a normal camel cased match.
                //      i.e. CoFiPro would match CodeFixProvider, but CofiPro would not.  
                if (patternChunk.CharacterSpans.GetCount() > 0)
                {
                    var candidateHumps = GetWordSpans(candidate);
                    var camelCaseKind = TryUpperCaseCamelCaseMatch(candidate, candidateHumps, patternChunk, CompareOptions.None, out var matchedSpans, chunkOffset);
                    if (camelCaseKind.HasValue)
                    {
                        return new PatternMatch(
                            camelCaseKind.Value, punctuationStripped, isCaseSensitive: true,
                            matchedSpans: matchedSpans);
                    }

                    camelCaseKind = TryUpperCaseCamelCaseMatch(candidate, candidateHumps, patternChunk, CompareOptions.IgnoreCase, out matchedSpans, chunkOffset);
                    if (camelCaseKind.HasValue)
                    {
                        return new PatternMatch(
                            camelCaseKind.Value, punctuationStripped, isCaseSensitive: false,
                            matchedSpans: matchedSpans);
                    }
                }
            }

            return null;
        }

        private PatternMatchKind? TryAllLowerCamelCaseMatch(
            string candidate,
            StringBreaks candidateParts,
            TextChunk patternChunk,
            out ImmutableArray<TextSpan> matchedSpans,
            int chunkOffset)
        {
            var matcher = new AllLowerCamelCaseMatcher(_includeMatchedSpans, candidate, candidateParts, patternChunk);
            return matcher.TryMatch(chunkOffset, out matchedSpans);
        }

        private PatternMatchKind? TryUpperCaseCamelCaseMatch(
            string candidate,
            StringBreaks candidateHumps,
            TextChunk patternChunk,
            CompareOptions compareOption,
            out ImmutableArray<TextSpan> matchedSpans,
            int chunkOffset)
        {
            var patternHumps = patternChunk.CharacterSpans;

            // Note: we may have more pattern parts than candidate parts.  This is because multiple
            // pattern parts may match a candidate part.  For example "SiUI" against "SimpleUI".
            // We'll have 3 pattern parts Si/U/I against two candidate parts Simple/UI.  However, U
            // and I will both match in UI. 

            int currentCandidateHump = 0;
            int currentPatternHump = 0;
            int? firstMatch = null;
            int? lastMatch = null;
            bool? contiguous = null;

            var patternHumpCount = patternHumps.GetCount();
            var candidateHumpCount = candidateHumps.GetCount();

            var matchSpans = ArrayBuilder<TextSpan>.GetInstance();
            while (true)
            {
                // Let's consider our termination cases
                if (currentPatternHump == patternHumpCount)
                {
                    Contract.Requires(firstMatch.HasValue);
                    Contract.Requires(contiguous.HasValue);

                    var matchCount = matchSpans.Count;
                    matchedSpans = _includeMatchedSpans
                        ? new NormalizedSpanCollection(matchSpans).ToImmutableArray()
                        : ImmutableArray<TextSpan>.Empty;
                    matchSpans.Free();

                    var camelCaseResult = new CamelCaseResult(
                        fromStart: firstMatch == 0,
                        contiguous: contiguous.Value,
                        toEnd: lastMatch == candidateHumpCount - 1,
                        matchCount: matchCount,
                        matchedSpansInReverse: null,
                        chunkOffset: chunkOffset
                        );
                    return GetCamelCaseKind(camelCaseResult);
                }
                else if (currentCandidateHump == candidateHumpCount)
                {
                    // No match, since we still have more of the pattern to hit
                    matchedSpans = ImmutableArray<TextSpan>.Empty;
                    matchSpans.Free();
                    return null;
                }

                var candidateHump = candidateHumps[currentCandidateHump];
                bool gotOneMatchThisCandidate = false;

                // Consider the case of matching SiUI against SimpleUIElement. The candidate parts
                // will be Simple/UI/Element, and the pattern parts will be Si/U/I.  We'll match 'Si'
                // against 'Simple' first.  Then we'll match 'U' against 'UI'. However, we want to
                // still keep matching pattern parts against that candidate part. 
                for (; currentPatternHump < patternHumpCount; currentPatternHump++)
                {
                    var patternChunkCharacterSpan = patternHumps[currentPatternHump];

                    if (gotOneMatchThisCandidate)
                    {
                        // We've already gotten one pattern part match in this candidate.  We will
                        // only continue trying to consume pattern parts if the last part and this
                        // part are both upper case.  
                        if (!char.IsUpper(patternChunk.Text[patternHumps[currentPatternHump - 1].Start]) ||
                            !char.IsUpper(patternChunk.Text[patternHumps[currentPatternHump].Start]))
                        {
                            break;
                        }
                    }

                    if (!PartStartsWith(candidate, candidateHump, patternChunk.Text, patternChunkCharacterSpan, compareOption))
                    {
                        break;
                    }

                    matchSpans.Add(new TextSpan(chunkOffset + candidateHump.Start, patternChunkCharacterSpan.Length));
                    gotOneMatchThisCandidate = true;

                    firstMatch = firstMatch ?? currentCandidateHump;
                    lastMatch = currentCandidateHump;

                    // If we were contiguous, then keep that value.  If we weren't, then keep that
                    // value.  If we don't know, then set the value to 'true' as an initial match is
                    // obviously contiguous.
                    contiguous = contiguous ?? true;

                    candidateHump = new TextSpan(candidateHump.Start + patternChunkCharacterSpan.Length, candidateHump.Length - patternChunkCharacterSpan.Length);
                }

                // Check if we matched anything at all.  If we didn't, then we need to unset the
                // contiguous bit if we currently had it set.
                // If we haven't set the bit yet, then that means we haven't matched anything so
                // far, and we don't want to change that.
                if (!gotOneMatchThisCandidate && contiguous.HasValue)
                {
                    contiguous = false;
                }

                // Move onto the next candidate.
                currentCandidateHump++;
            }
        }
    }
}
