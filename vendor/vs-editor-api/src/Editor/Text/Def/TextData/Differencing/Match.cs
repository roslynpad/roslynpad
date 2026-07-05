//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
using System;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.Text.Differencing
{
#pragma warning disable CA1710 // Identifiers should have correct suffix
    /// <summary>
    /// Represents a range of matches between two sequences as a pair of spans of equal length.
    /// </summary>
    /// <remarks>
    /// Given two sequences:
    /// abCCd (left)
    /// abFFd (right)
    /// The generated pairs of matches would be:
    /// (0, 0), (1, 1), (4, 4)
    /// Which would turn into the Matches (left-start, right-start, length):
    /// (0, 0, 2) and (4, 4, 1)
    ///</remarks>
    public class Match : IEnumerable<Tuple<int, int>>
#pragma warning restore CA1710 // Identifiers should have correct suffix
    {
        private Span left;
        private Span right;
        
        /// <summary>
        /// Creates a match from two spans of equal length.
        /// </summary>
        /// <param name="left">The span from the left sequence.</param>
        /// <param name="right">The span from the right sequence.</param>
        /// <exception cref="ArgumentNullException">The left span or right span is null.</exception>
        /// <exception cref="ArgumentException">The spans are not of equal length.</exception>
        public Match(Span left, Span right)
        {
            if (left.Length != right.Length)
                throw new ArgumentException("Spans must be of equal length");

            this.left = left;
            this.right = right;
        }

        /// <summary>
        /// Get the left-side range
        /// </summary>
        public Span Left
        {
            get { return left; }
        }
        
        /// <summary>
        /// Gets the right span.
        /// </summary>
        public Span Right
        {
            get { return right; }
        }

        /// <summary>
        /// Gets the length of the spans. Both spans have equal lengths.
        /// </summary>
        public int Length
        {
            get { return left.Length; }
        }

        /// <summary>
        /// Determines whether two Match objects have the same left and right spans.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            Match other = obj as Match;

            if(other != null)
                return left.Equals(other.left) && right.Equals(other.right);

            return false;
        }

        /// <summary>
        /// Provides a hash function.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return left.GetHashCode() << 16 ^ right.GetHashCode();
        }

        #region IEnumerable<Tuple<int,int>> Members

        /// <summary>
        /// Gets an enumerator typed as a <see cref="Tuple"/> of integers.
        /// </summary>
        /// <returns>The typed enumerator.</returns>
        public IEnumerator<Tuple<int, int>> GetEnumerator()
        {
            for (int i = 0; i < Length; i++)
                yield return new Tuple<int, int>(left.Start + i, right.Start + i);
        }

        #endregion

        #region IEnumerable Members

        /// <summary>
        /// Gets an untyped enumerator.
        /// </summary>
        /// <returns>The enumerator.</returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }
}
