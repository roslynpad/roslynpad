//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Differencing
{
    using Microsoft;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using Microsoft.VisualStudio.Text.Utilities;

    public class SideBySideMap
    {
        public ISnapshotDifference Difference { get; }

        public int SideBySideLength { get; }

        private IList<int> _sideBySideEndAtDiff;

        /// <summary>
        /// Create a Side-by-side map for the given snapshot difference. Note that only side-by-side line maps are supported
        /// (character maps -- needed for word wrap -- will require a different approach since even "matches" could have different
        /// lengths if ignore white space is turned on).
        /// </summary>
        public SideBySideMap(ISnapshotDifference difference)
        {
            Requires.NotNull(difference, nameof(difference));
            this.Difference = difference;

            // Calculate the effective document "length" (where the length of each diff block
            // is the maximum of the right & left diffs) and store it for each diff.
            //
            // This code assumes word wrap is off. If it is on, we need to use the characters in each block.
            _sideBySideEndAtDiff = new List<int>(difference.LineDifferences.Differences.Count);

            var leftEnd = 0;
            var rightEnd = 0;
            var sideBySideEnd = 0;
            for (int i = 0; (i < difference.LineDifferences.Differences.Count); ++i)
            {
                var diff = difference.LineDifferences.Differences[i];

                int newLeftEnd = diff.Left.End;
                int newRightEnd = diff.Right.End;

                var leftDelta = newLeftEnd - leftEnd;
                var rightDelta = newRightEnd - rightEnd;

                sideBySideEnd += Math.Max(leftDelta, rightDelta);
                _sideBySideEndAtDiff.Add(sideBySideEnd);

                leftEnd = newLeftEnd;
                rightEnd = newRightEnd;
            }

            // Since we're in a match, (current.RightBufferSnapshot.LineCount - rightEnd) == current.LeftBufferSnapshot.LineCount - leftEnd
            this.SideBySideLength = sideBySideEnd + difference.RightBufferSnapshot.LineCount - rightEnd;
        }

        /// <summary>
        /// Return the buffer position that corresponds to the specified coordinate (which will be either a line number or a character position). The returned position may be
        /// on either the left or right difference snapshots.
        /// </summary>
        public SnapshotPoint BufferPositionFromSideBySideCoordinate(int coordinate)
        {
            //Convert the coordinate to a valid line number (0 ... line count - 1). 
            coordinate = Math.Min(Math.Max(0, coordinate), (this.SideBySideLength - 1));

            ListUtilities.BinarySearch(_sideBySideEndAtDiff, (s) => (s - coordinate - 1), out int index);

            ITextSnapshotLine line;
            if (index >= this.Difference.LineDifferences.Differences.Count)
            {
                // We're in a match that follows the last difference (assuming there are any differences).
                // Count lines backwards from the end of the right buffer.
                var delta = this.SideBySideLength - coordinate;
                line = this.Difference.RightBufferSnapshot.GetLineFromLineNumber(this.Difference.RightBufferSnapshot.LineCount - delta);
            }
            else
            {
                // We either in a difference or in the match that preceeds a difference.
                // In either case, the sideBySideEnd corresponds to the start of the match.
                int sideBySideEnd = (index > 0) ? _sideBySideEndAtDiff[index - 1] : 0;

                int delta = coordinate - sideBySideEnd;
                Span left;
                Span right;
                var difference = this.Difference.LineDifferences.Differences[index];
                left = difference.Left;
                right = difference.Right;
                if (difference.Before != null)
                {
                    left = Span.FromBounds(difference.Before.Left.Start, difference.Left.End);
                    right = Span.FromBounds(difference.Before.Right.Start, difference.Right.End);
                }

                if (delta < right.Length)
                {
                    line = this.Difference.RightBufferSnapshot.GetLineFromLineNumber(right.Start + delta);
                }
                else
                {
                    Debug.Assert(delta < left.Length);
                    line = this.Difference.LeftBufferSnapshot.GetLineFromLineNumber(left.Start + delta);
                }
            }

            return line.Start;
        }

        /// <summary>
        /// Return the coordinate of the given buffer position (which can be on the left or right buffers).
        /// </summary>
        public int SideBySideCoordinateFromBufferPosition(SnapshotPoint position)
        {
            position = this.Difference.TranslateToSnapshot(position);
            int lineNumber = position.GetContainingLine().LineNumber;
            int index = this.Difference.FindMatchOrDifference(position, out Match match, out Difference difference);
            if (index == 0)
            {
                // Either in a leading match or the 1st difference. In either case, the real line number is the side-by-side line number.
                return lineNumber;
            }

            difference = this.Difference.LineDifferences.Differences[index - 1];
            if (position.Snapshot == this.Difference.LeftBufferSnapshot)
            {
                return _sideBySideEndAtDiff[index - 1] + (lineNumber - difference.Left.End);
            }
            else
            {
                return _sideBySideEndAtDiff[index - 1] + (lineNumber - difference.Right.End);
            }
        }
    }
}
