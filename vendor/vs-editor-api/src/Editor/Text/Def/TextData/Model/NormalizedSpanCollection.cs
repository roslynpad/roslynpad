//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text
{
    using System;
    using System.Text;
    using System.Diagnostics;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    /// <summary>
    /// A collection of spans that are sorted by start position, with adjacent and overlapping spans combined.
    /// </summary>
    public class NormalizedSpanCollection : ReadOnlyCollection<Span>
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104", Justification = "Type is readonly")]
        // 'new': hides ReadOnlyCollection<T>.Empty added in .NET 8; same value semantics, narrower type.
        public new readonly static NormalizedSpanCollection Empty = new NormalizedSpanCollection();

        /// <summary>
        /// Initializes a new instance of <see cref="NormalizedSpanCollection"/> that is empty.
        /// </summary>
        public NormalizedSpanCollection()
            : base(new List<Span>(0))
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="NormalizedSpanCollection"/> that contains the specified span.
        /// </summary>
        /// <param name="span">Span contained by the span set.</param>
        public NormalizedSpanCollection(Span span)
            : base(ListFromSpan(span))
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="NormalizedSpanCollection"/> that contains the specified list of spans.
        /// </summary>
        /// <param name="spans">The spans to be added.</param>
        /// <remarks>
        /// <para>The list of spans will be sorted and normalized (overlapping and adjoining spans will be combined).</para>
        /// <para>This constructor runs in O(N log N) time, where N = spans.Count.</para></remarks>
        /// <exception cref="ArgumentNullException"><paramref name="spans"/> is null.</exception>
        public NormalizedSpanCollection(IEnumerable<Span> spans)
            : base(NormalizedSpanCollection.NormalizeSpans(spans))
        {
            //NormalizeSpans will throw if spans == null.
        }

        /// <summary>
        /// Initializes a new instance of <see cref="NormalizedSpanCollection"/> that contains the specified (already 
        /// normalized) list of spans.
        /// </summary>
        /// <param name="spans">The spans to be added.</param>
        /// <param name="ignored">This parameter is present just to give this constructor a different signature that
        /// is used to indicate the input spans are already normalized.</param>
        /// <remarks>
        /// <para>This constructor runs in O(N) time, where N = spans.Count.</para></remarks>
        /// <exception cref="ArgumentNullException"><paramref name="normalizedSpans"/> is null.</exception>
        /// <remarks>This constructor is private so as not to expose the misleading <paramref name="ignored"/> parameter.</remarks>
#pragma warning disable CA1801 // Parameter ignored is never used
        private NormalizedSpanCollection(IList<Span> normalizedSpans, bool ignored)
            : base(normalizedSpans)
#pragma warning restore CA1801 // Parameter ignored is never used
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="NormalizedSpanCollection"/> from a list of <see cref="Span"/>s that are
        /// already normalized. For internal use only.
        /// </summary>
        /// <param name="alreadyNormalizedSpans"></param>
        /// <returns></returns>
        internal static NormalizedSpanCollection CreateFromNormalizedSpans(IList<Span> alreadyNormalizedSpans)
        {
            return new NormalizedSpanCollection(alreadyNormalizedSpans, true);
        }

        /// <summary>
        /// Finds the union of two span sets.
        /// </summary>
        /// <param name="left">
        /// The first span set.
        /// </param>
        /// <param name="right">
        /// The second span set.
        /// </param>
        /// <returns>
        /// The new span set that corresponds to the union of <paramref name="left"/> and <paramref name="right"/>.
        /// </returns>
        /// <remarks>This operator runs in O(N+M) time where N = left.Count, M = right.Count.</remarks>
        /// <exception cref="ArgumentNullException">Either <paramref name="left"/> or <paramref name="right"/> is null.</exception>
        public static NormalizedSpanCollection Union(NormalizedSpanCollection left, NormalizedSpanCollection right)
        {
            if (left == null)
            {
                throw new ArgumentNullException(nameof(left));
            }
            if (right == null)
            {
                throw new ArgumentNullException(nameof(right));
            }

            if (left.Count == 0)
            {
                return right;
            }
            if (right.Count == 0)
            {
                return left;
            }

            var spans = new List<Span>();

            int index1 = 0;
            int index2 = 0;

            int start = -1;
            int end = int.MaxValue;
            while ((index1 < left.Count) && (index2 < right.Count))
            {
                Span span1 = left[index1];
                Span span2 = right[index2];

                if (span1.Start < span2.Start)
                {
                    NormalizedSpanCollection.UpdateSpanUnion(span1, spans, ref start, ref end);
                    ++index1;
                }
                else
                {
                    NormalizedSpanCollection.UpdateSpanUnion(span2, spans, ref start, ref end);
                    ++index2;
                }
            }
            while (index1 < left.Count)
            {
                NormalizedSpanCollection.UpdateSpanUnion(left[index1], spans, ref start, ref end);
                ++index1;
            }
            while (index2 < right.Count)
            {
                NormalizedSpanCollection.UpdateSpanUnion(right[index2], spans, ref start, ref end);
                ++index2;
            }

            if (end != int.MaxValue)
            {
                spans.Add(Span.FromBounds(start, end));
            }

            return CreateFromNormalizedSpans(spans);
        }

        /// <summary>
        /// Findx the overlap of two span sets.
        /// </summary>
        /// <param name="left">The first span set.</param>
        /// <param name="right">The second span set.</param>
        /// <returns>The new span set that corresponds to the overlap of <paramref name="left"/> and <paramref name="right"/>.</returns>
        /// <remarks>This operator runs in O(N+M) time where N = left.Count, M = right.Count.</remarks>
        /// <exception cref="ArgumentNullException"><paramref name="left"/> or <paramref name="right"/> is null.</exception>
        public static NormalizedSpanCollection Overlap(NormalizedSpanCollection left, NormalizedSpanCollection right)
        {
            if (left == null)
            {
                throw new ArgumentNullException(nameof(left));
            }
            if (right == null)
            {
                throw new ArgumentNullException(nameof(right));
            }

            if (left.Count == 0)
            {
                return left;
            }
            if (right.Count == 0)
            {
                return right;
            }

            var spans = new List<Span>();
            for (int index1 = 0, index2 = 0; (index1 < left.Count) && (index2 < right.Count); )
            {
                Span span1 = left[index1];
                Span span2 = right[index2];

                if (span1.OverlapsWith(span2))
                {
                    spans.Add(span1.Overlap(span2).Value);
                }

                if (span1.End < span2.End)
                {
                    ++index1;
                }
                else if (span1.End == span2.End)
                {
                    ++index1; ++index2;
                }
                else
                {
                    ++index2;
                }
            }

            return CreateFromNormalizedSpans(spans);
        }

        /// <summary>
        /// Finds the intersection of two span sets.
        /// </summary>
        /// <param name="left">The first span set.</param>
        /// <param name="right">The second span set.</param>
        /// <returns>The new span set that corresponds to the intersection of <paramref name="left"/> and <paramref name="right"/>.</returns>
        /// <remarks>This operator runs in O(N+M) time where N = left.Count, M = right.Count.</remarks>
        /// <exception cref="ArgumentNullException"><paramref name="left"/> is null.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="right"/> is null.</exception>
        public static NormalizedSpanCollection Intersection(NormalizedSpanCollection left, NormalizedSpanCollection right)
        {
            if (left == null)
            {
                throw new ArgumentNullException(nameof(left));
            }
            if (right == null)
            {
                throw new ArgumentNullException(nameof(right));
            }

            if (left.Count == 0)
            {
                return left;
            }
            if (right.Count == 0)
            {
                return right;
            }

            var spans = new List<Span>();
            for (int index1 = 0, index2 = 0; (index1 < left.Count) && (index2 < right.Count) ;)
            {
                Span span1 = left[index1];
                Span span2 = right[index2];

                if (span1.IntersectsWith(span2))
                {
                    spans.Add(span1.Intersection(span2).Value);
                }

                if (span1.End < span2.End)
                {
                    ++index1;
                }
                else
                {
                    ++index2;
                }
            }

            return CreateFromNormalizedSpans(spans);
        }

        /// <summary>
        /// Finds the difference between two sets. The difference is defined as everything in the first span set that is not in the second span set.
        /// </summary>
        /// <param name="left">The first span set.</param>
        /// <param name="right">The second span set.</param>
        /// <returns>The new span set that corresponds to the difference between <paramref name="left"/> and <paramref name="right"/>.</returns>
        /// <remarks>
        /// Empty spans in the second set do not affect the first set at all. This method returns empty spans in the first set that are not contained by any set in
        /// the second set.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="left"/> is null.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="right"/> is null.</exception>
        public static NormalizedSpanCollection Difference(NormalizedSpanCollection left, NormalizedSpanCollection right)
        {
            if (left == null)
            {
                throw new ArgumentNullException(nameof(left));
            }
            if (right == null)
            {
                throw new ArgumentNullException(nameof(right));
            }

            if (left.Count == 0)
            {
                return left;
            }
            if (right.Count == 0)
            {
                return left;
            }

            var spans = new List<Span>();

            int index1 = 0;
            int index2 = 0;
            int lastEnd = -1;
            do
            {
                Span span1 = left[index1];
                Span span2 = right[index2];

                if ((span2.Length == 0) || (span1.Start >= span2.End))
                {
                    ++index2;
                }
                else if (span1.End <= span2.Start)
                {
                    //lastEnd is set to the end of the previously encountered intersecting span from right when
                    //it ended before the end of span1 (so it must still be less than the end of span1).
                    Debug.Assert(lastEnd < span1.End);
                    spans.Add(Span.FromBounds(Math.Max(lastEnd, span1.Start), span1.End));
                    ++index1;
                }
                else
                {
                    // The spans intersect, so add anything from span1 that extends to the left of span2.
                    if (span1.Start < span2.Start)
                    {
                        //lastEnd is set to the end of the previously encountered intersecting span on span2, so it must
                        //be less than the start of the current span on span2.
                        Debug.Assert(lastEnd < span2.Start);
                        spans.Add(Span.FromBounds(Math.Max(lastEnd, span1.Start), span2.Start));
                    }

                    if (span1.End < span2.End)
                    {
                        ++index1;
                    }
                    else if (span1.End == span2.End)
                    {
                        //Both spans ended at the same place so we're done with both.
                        ++index1; ++index2;
                    }
                    else
                    {
                        //span2 ends before span1, so keep track of where it ended so that we don't try to add
                        //the excluded portion the next time we add a span.
                        lastEnd = span2.End;
                        ++index2;
                    }
                }
            }
            while ((index1 < left.Count) && (index2 < right.Count));

            while (index1 < left.Count)
            {
                Span span1 = left[index1++];
                spans.Add(Span.FromBounds(Math.Max(lastEnd, span1.Start), span1.End));
            }

            return CreateFromNormalizedSpans(spans);
        }

        /// <summary>
        /// Determines whether two span sets are the same. 
        /// </summary>
        /// <param name="left">The first set.</param>
        /// <param name="right">The second set.</param>
        /// <returns><c>true</c> if the two sets are equivalent, otherwise <c>false</c>.</returns>
        public static bool operator ==(NormalizedSpanCollection left, NormalizedSpanCollection right)
        {
            if (ReferenceEquals(left, right))
                return true;
            if (ReferenceEquals(left, null) || ReferenceEquals(right, null))
                return false;

            if (left.Count != right.Count)
                return false;

            for (int i = 0; (i < left.Count); ++i)
                if (left[i] != right[i])
                    return false;

            return true;
        }

        /// <summary>
        /// Determines whether two span sets are not the same.
        /// </summary>
        /// <param name="left">The first set.</param>
        /// <param name="right">The second set.</param>
        /// <returns><c>true</c> if the two sets are not equivalent, otherwise <c>false</c>.</returns>
        public static bool operator !=(NormalizedSpanCollection left, NormalizedSpanCollection right)
        {
            return !(left == right);
        }

        /// <summary>
        /// Determines whether this span set overlaps with another span set.
        /// </summary>
        /// <param name="set">The span set to test.</param>
        /// <returns><c>true</c> if the span sets overlap, otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="set"/> is null.</exception>
        public bool OverlapsWith(NormalizedSpanCollection set)
        {
            if (set == null)
            {
                throw new ArgumentNullException(nameof(set));
            }

            for (int index1 = 0, index2 = 0; (index1 < this.Count) && (index2 < set.Count) ;)
            {
                Span span1 = this[index1];
                Span span2 = set[index2];

                if (span1.OverlapsWith(span2))
                {
                    return true;
                }

                if (span1.End < span2.End)
                {
                    ++index1;
                }
                else if (span1.End == span2.End)
                {
                    ++index1; ++index2;
                }
                else
                {
                    ++index2;
                }
            }

            return false;
        }

        /// <summary>
        /// Determines whether this span set overlaps with another span.
        /// </summary>
        /// <param name="span">The span to test.</param>
        /// <returns><c>true</c> if this span set overlaps with the given span, otherwise <c>false</c>.</returns>
        public bool OverlapsWith(Span span)
        {
            // TODO: binary search
            for (int index = 0; index < this.Count; ++index)
            {
                if (this[index].OverlapsWith(span))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Determines wheher this span set intersects with another span set.
        /// </summary>
        /// <param name="set">Set to test.</param>
        /// <returns><c>true</c> if the span sets intersect, otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="set"/> is null.</exception>
        public bool IntersectsWith(NormalizedSpanCollection set)
        {
            if (set == null)
            {
                throw new ArgumentNullException(nameof(set));
            }

            for (int index1 = 0, index2 = 0; (index1 < this.Count) && (index2 < set.Count); )
            {
                Span span1 = this[index1];
                Span span2 = set[index2];

                if (span1.IntersectsWith(span2))
                {
                    return true;
                }

                if (span1.End < span2.End)
                {
                    ++index1;
                }
                else
                {
                    ++index2;
                }
            }

            return false;
        }

        /// <summary>
        /// Determines wheher this span set intersects with another span.
        /// </summary>
        /// <param name="set">The span to test.</param>
        /// <returns><c>true</c> if this span set intersects with the given span, otherwise <c>false</c>.</returns>
        public bool IntersectsWith(Span span)
        {
            // TODO: binary search
            for (int index = 0; index < this.Count; ++index)
            {
                if (this[index].IntersectsWith(span))
                {
                    return true;
                }
            }

            return false;
        }


        #region Overridden methods and operators

        /// <summary>
        /// Gets a unique hash code for the span set.
        /// </summary>
        /// <returns>A 32-bit hash code associated with the set.</returns>
        public override int GetHashCode()
        {
            int hc = 0;
            foreach (Span s in this)
                hc ^= s.GetHashCode();

            return hc;
        }

        /// <summary>
        /// Determines whether this span set is the same as another object.
        /// </summary>
        /// <param name="obj">The object to test.</param>
        /// <returns><c>true</c> if the two objects are equal, otherwise <c>false</c>.</returns>
        public override bool Equals(object obj)
        {
            NormalizedSpanCollection set = obj as NormalizedSpanCollection;

            return this == set;
        }

        /// <summary>
        /// Provides a string representation of the set.
        /// </summary>
        /// <returns>Thetring representation of the set.</returns>
        public override string ToString()
        {
            StringBuilder value = new StringBuilder("{");
            foreach (Span s in this)
                value.Append(s.ToString());
            value.Append("}");

            return value.ToString();
        }

        #endregion // Overridden methods and operators

        #region Private Helpers
        private static IList<Span> ListFromSpan(Span span)
        {
            IList<Span> list = new List<Span>(1);
            list.Add(span);
            return list;
        }

        private static void UpdateSpanUnion(Span span, IList<Span> spans, ref int start, ref int end)
        {
            if (end < span.Start)
            {
                spans.Add(Span.FromBounds(start, end));

                start = -1;
                end = int.MaxValue;
            }

            if (end == int.MaxValue)
            {
                start = span.Start;
                end = span.End;
            }
            else
            {
                end = Math.Max(end, span.End);
            }
        }

        private class SpanStartComparer : IComparer<Span>
        {
            public int Compare(Span s1, Span s2) { return s1.Start.CompareTo(s2.Start); }

            public static readonly IComparer<Span> Default = new SpanStartComparer();
        }

        private static IList<Span> NormalizeSpans(IEnumerable<Span> spans)
        {
            if (spans == null)
            {
                throw new ArgumentNullException(nameof(spans));
            }

            var sorted = new List<Span>(spans);
            if (sorted.Count <= 1)
            {
                return sorted;
            }
            else
            {
                sorted.Sort(SpanStartComparer.Default);

                int oldIndex = 0;
                int oldStart = sorted[0].Start;
                int oldEnd = sorted[0].End;
                for (int index = 1; (index < sorted.Count); ++index)
                {
                    int newStart = sorted[index].Start;
                    int newEnd = sorted[index].End;
                    if (oldEnd < newStart)
                    {
                        sorted[oldIndex++] = Span.FromBounds(oldStart, oldEnd);
                        oldStart = newStart;
                        oldEnd = newEnd;
                    }
                    else
                    {
                        oldEnd = Math.Max(oldEnd, newEnd);
                    }
                }
                sorted[oldIndex++] = Span.FromBounds(oldStart, oldEnd);

                sorted.RemoveRange(oldIndex, sorted.Count - oldIndex);

                //Only call TrimExcess() if the list is large enough that the memory saved is worth the cost
                //of another allocation.
                if (sorted.Capacity > 10)
                    sorted.TrimExcess();

                return sorted;
            }
        }
        #endregion // Private Helpers
    }
}
