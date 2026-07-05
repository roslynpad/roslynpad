//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
using System;
using System.Collections.Generic;
using System.Composition;
using System.Diagnostics;

namespace Microsoft.VisualStudio.Text.Differencing.Implementation
{
// Ignore deprecated (IHierarchicalStringDifferenceService is deprecated)
#pragma warning disable 0618

    [Export(typeof(IHierarchicalStringDifferenceService))]
    [Shared]
    public sealed class DefaultTextDifferencingService : ITextDifferencingService, IHierarchicalStringDifferenceService
    {
        #region ITextDifferencingService-specific

        public IHierarchicalDifferenceCollection DiffSnapshotSpans(SnapshotSpan leftSpan, SnapshotSpan rightSpan, StringDifferenceOptions differenceOptions)
        {
            return this.DiffSnapshotSpans(leftSpan, rightSpan, differenceOptions, DefaultGetLineTextCallback);
        }

        public IHierarchicalDifferenceCollection DiffSnapshotSpans(SnapshotSpan leftSpan, SnapshotSpan rightSpan, StringDifferenceOptions differenceOptions, Func<ITextSnapshotLine, string> getLineTextCallback)
        {
            StringDifferenceTypes type;
            ITokenizedStringListInternal left;
            ITokenizedStringListInternal right;
            if (differenceOptions.DifferenceType.HasFlag(StringDifferenceTypes.Line))
            {
                type = StringDifferenceTypes.Line;

                left = new SnapshotLineList(leftSpan, getLineTextCallback, differenceOptions);
                right = new SnapshotLineList(rightSpan, getLineTextCallback, differenceOptions);
            }
            else if (differenceOptions.DifferenceType.HasFlag(StringDifferenceTypes.Word))
            {
                type = StringDifferenceTypes.Word;

                left = new WordDecompositionList(leftSpan, differenceOptions);
                right = new WordDecompositionList(rightSpan, differenceOptions);
            }
            else if (differenceOptions.DifferenceType.HasFlag(StringDifferenceTypes.Character))
            {
                type = StringDifferenceTypes.Character;

                left = new CharacterDecompositionList(leftSpan);
                right = new CharacterDecompositionList(rightSpan);
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(differenceOptions));
            }

            return DiffText(left, right, type, differenceOptions);
        }

        public IHierarchicalDifferenceCollection DiffStrings(string leftString, string rightString, StringDifferenceOptions differenceOptions)
        {
            if (leftString == null)
                throw new ArgumentNullException(nameof(leftString));
            if (rightString == null)
                throw new ArgumentNullException(nameof(rightString));

            StringDifferenceTypes type;
            ITokenizedStringListInternal left;
            ITokenizedStringListInternal right;
            if (differenceOptions.DifferenceType.HasFlag(StringDifferenceTypes.Line))
            {
                type = StringDifferenceTypes.Line;

                left = new LineDecompositionList(leftString, differenceOptions.IgnoreTrimWhiteSpace);
                right = new LineDecompositionList(rightString, differenceOptions.IgnoreTrimWhiteSpace);
            }
            else if (differenceOptions.DifferenceType.HasFlag(StringDifferenceTypes.Word))
            {
                type = StringDifferenceTypes.Word;

                left = new WordDecompositionList(leftString, differenceOptions);
                right = new WordDecompositionList(rightString, differenceOptions);
            }
            else if (differenceOptions.DifferenceType.HasFlag(StringDifferenceTypes.Character))
            {
                type = StringDifferenceTypes.Character;

                left = new CharacterDecompositionList(leftString);
                right = new CharacterDecompositionList(rightString);
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(differenceOptions));
            }

            return DiffText(left, right, type, differenceOptions);
        }

        IHierarchicalDifferenceCollection DiffText(ITokenizedStringListInternal left, ITokenizedStringListInternal right, StringDifferenceTypes type, StringDifferenceOptions differenceOptions)
        {
            StringDifferenceOptions nextOptions = new StringDifferenceOptions(differenceOptions);
            nextOptions.DifferenceType &= ~type;

            var diffCollection = ComputeMatches(differenceOptions, left, right);
            return new HierarchicalDifferenceCollection(diffCollection, left, right, this, nextOptions);
        }

        internal static List<Span> GetContiguousSpans(Span span, ITokenizedStringListInternal tokens)
        {
            List<Span> result = new List<Span>();
            int start = span.Start;
            for (int i = span.Start + 1; (i < span.End); ++i)
            {
                if (tokens.GetElementInOriginal(i - 1).End != tokens.GetElementInOriginal(i).Start)
                {
                    result.Add(Span.FromBounds(start, i));
                    start = i;
                }
            }

            if (start < span.End)
            {
                result.Add(Span.FromBounds(start, span.End));
            }

            return result;
        }

        internal static string DefaultGetLineTextCallback(ITextSnapshotLine line)
        {
            return line.GetTextIncludingLineBreak();
        }

        static IDifferenceCollection<string> ComputeMatches(StringDifferenceOptions differenceOptions,
                                           IList<string> leftSequence, IList<string> rightSequence)
        {
            return ComputeMatches(differenceOptions, leftSequence, rightSequence, leftSequence, rightSequence);
        }

        static IDifferenceCollection<string> ComputeMatches(StringDifferenceOptions differenceOptions,
                                                           IList<string> leftSequence, IList<string> rightSequence,
                                                           IList<string> originalLeftSequence, IList<string> originalRightSequence)
        {
            return MaximalSubsequenceAlgorithm.DifferenceSequences(leftSequence, rightSequence, originalLeftSequence, originalRightSequence, differenceOptions.ContinueProcessingPredicate);
        }

        #endregion
    }

#pragma warning restore 0618
}
