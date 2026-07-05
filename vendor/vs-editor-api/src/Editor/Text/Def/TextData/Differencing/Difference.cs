//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
using System.Globalization;

namespace Microsoft.VisualStudio.Text.Differencing
{
    /// <summary>
    /// Represents a single difference in the set of differences of two
    /// lists of elements.  
    /// </summary>
    /// <remarks>Each difference consists of a left span and a right span, either
    /// of which may have zero length (that is, the operation may be an add operation or a delete operation).
    /// The text before and after the difference matches.  
    /// In general, differencess are non-null. However, when the difference
    /// appears at the beginning of the lists of differences, the "before" is null, and when the difference appears at
    /// the end of the lists, the "after" is null.</remarks>
    public class Difference
    {
        private Span left;
        private Span right;
        private Match before;
        private Match after;
        private DifferenceType type;

        /// <summary>
        /// Initializes a new instance of a <see cref="Difference"/> with the specified left and right spans and before and after matches.
        /// </summary>
        /// <param name="left">The left side of the difference (may have zero length).</param>
        /// <param name="right">The right side of the difference (may have zero length).</param>
        /// <param name="before">The non-differing element range before this difference.</param>
        /// <param name="after">The non-differing element range after this difference.</param>
        public Difference(Span left, Span right, Match before, Match after)
        {
            this.left = left;
            this.right = right;
            this.before = before;
            this.after = after;

            if (left.Length == 0)
                type = DifferenceType.Add;
            else if (right.Length == 0)
                type = DifferenceType.Remove;
            else
                type = DifferenceType.Change;
        }

        /// <summary>
        /// The left side of the difference (may be zero length).
        /// </summary>
        public Span Left
        {
            get { return left; }
        }

        /// <summary>
        /// The right side of the difference (may be zero length).
        /// </summary>
        public Span Right
        {
            get { return right; }
        }

        /// <summary>
        /// The match before this section. It is null at the beginning of
        /// the sequence.
        /// </summary>
        public Match Before
        {
            get { return before; }
        }

        /// <summary>
        /// The mtch after this difference. It is null at the end of the sequence.
        /// </summary>
        public Match After
        {
            get { return after; }
        }

        /// <summary>
        /// The type of the difference (Add, Remove, or Change).
        /// </summary>
        public DifferenceType DifferenceType
        {
            get { return type; }
        }

        /// <summary>
        /// The string representation of this difference.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return string.Format(CultureInfo.CurrentCulture.NumberFormat, "(Difference; Type: {0}, Left count: {1}, Right count: {2}",
                type, left.Length, right.Length);
        }

        /// <summary>
        /// Determines whether two Difference objects are the same (have the same difference type and the same before and after matches).
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            Difference other = obj as Difference;
            if (other != null)
            {
                return object.Equals(type, other.type) &&
                    object.Equals(before, other.before) && object.Equals(after, other.after) &&
                    object.Equals(left, other.left) && object.Equals(right, other.right);
            }

            return false;
        }

        /// <summary>
        /// Serves as a hash code for this type.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return before.GetHashCode() << 24 ^ after.GetHashCode() << 16 ^
                left.GetHashCode() << 8 ^ right.GetHashCode();
        }
    }
}
