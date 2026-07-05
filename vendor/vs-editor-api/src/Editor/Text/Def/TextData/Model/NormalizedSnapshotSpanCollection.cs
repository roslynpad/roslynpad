//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// A read-only collection of <see cref="SnapshotSpan"/> objects, all from the same snapshot. 
    /// </summary>
    /// <remarks>
    /// The snapshot spans are sorted by start position, 
    /// with adjacent and overlapping spans combined.
    /// </remarks>
    public sealed class NormalizedSnapshotSpanCollection : IList<SnapshotSpan>, ICollection<SnapshotSpan>, IEnumerable<SnapshotSpan>, IList, ICollection, IEnumerable
    {
        // imitate platform implementation of ReadOnlyCollection. Methods that write throw NotSupportException, and use explicit
        // interface implementation to keep them from being easily called or participating in intellisense.

        #region State and Construction

        // To save space and reduce allocations, we do not create an inner NormalizedSpanCollection if this collection is 
        // of size zero or one. If this.snapshot is null, the collection has size zero. If this.snapshot is nonnull and
        // this.spans is null, the collection is size one and this.span contains its single element (this accounts for
        // over 95% of the instances of this class). If this.spans is nonnull, the collection is size two or greater.
        // We can't do this by subclassing because of backward compatibility with the concrete constructor.

        private readonly ITextSnapshot snapshot;
        private readonly NormalizedSpanCollection spans;
        private readonly Span span;

        [SuppressMessage("Microsoft.Security", "CA2104", Justification = "Type is immutable")]
        public readonly static NormalizedSnapshotSpanCollection Empty = new NormalizedSnapshotSpanCollection();

        /// <summary>
        /// Initializes an empty <see cref="NormalizedSnapshotSpanCollection"/>.
        /// </summary>
        public NormalizedSnapshotSpanCollection()
        {
            // empty collection, signalled by this.snapshot == null
        }

        /// <summary>
        /// Initializes a new instance of a <see cref="NormalizedSnapshotSpanCollection"/> with a single element.
        /// </summary>
        /// <param name="span">The sole member of the collection.</param>
        /// <exception cref="ArgumentException"><paramref name="span"/> is not initialized.</exception>
        public NormalizedSnapshotSpanCollection(SnapshotSpan span)
        {
            if (span.Snapshot == null)
            {
                throw new ArgumentException(Strings.UninitializedSnapshotSpan);
            }
            this.snapshot = span.Snapshot;
            this.span = span;
        }

        /// <summary>
        /// Initializes a new instance of a <see cref="NormalizedSnapshotSpanCollection"/> from a <see cref="NormalizedSpanCollection"/> and a <see cref="ITextSnapshot"/>.
        /// </summary>
        /// <param name="snapshot">The <see cref="ITextSnapshot"/> to apply to <paramref name="spans"/>.</param>
        /// <param name="spans">The normalized spans.</param>
        /// <exception cref="ArgumentNullException"><paramref name="snapshot"/> or <paramref name="spans"/> is null.</exception>
        /// <exception cref="ArgumentException">The spans in <paramref name="spans"/> extend beyond the end of <paramref name="snapshot"/>.</exception>
        public NormalizedSnapshotSpanCollection(ITextSnapshot snapshot, NormalizedSpanCollection spans)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }
            if (spans == null)
            {
                throw new ArgumentNullException(nameof(spans));
            }
            if (spans.Count > 0 && spans[spans.Count - 1].End > snapshot.Length)
            {
                throw new ArgumentException(Strings.SpansBeyondEnd);
            }
            if (spans.Count == 1)
            {
                this.snapshot = snapshot;
                this.span = spans[0];
            }
            else if (spans.Count > 1)
            {
                this.snapshot = snapshot;
                this.spans = spans;
            }
        }

        /// <summary>
        /// Initializes a new instance of a <see cref="NormalizedSnapshotSpanCollection"/> from a list of <see cref="Span"/>s and a <see cref="ITextSnapshot"/>.
        /// </summary>
        /// <param name="snapshot">The <see cref="ITextSnapshot"/> to apply to <paramref name="spans"/>.</param>
        /// <param name="spans">An arbitrary set of <see cref="Span"/> objects.</param>
        /// <exception cref="ArgumentNullException"><paramref name="snapshot"/> or <paramref name="spans"/> is null.</exception>
        /// <exception cref="ArgumentException">The spans in <paramref name="spans"/> extend beyond the end of <paramref name="snapshot"/>.</exception>
        public NormalizedSnapshotSpanCollection(ITextSnapshot snapshot, IEnumerable<Span> spans)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }
            if (spans == null)
            {
                throw new ArgumentNullException(nameof(spans));
            }

            using (IEnumerator<Span> spanEnumerator = spans.GetEnumerator())
            {
                if (!spanEnumerator.MoveNext())
                {
                    // empty collection
                }
                else
                {
                    this.snapshot = snapshot;
                    Span span = spanEnumerator.Current;
                    if (!spanEnumerator.MoveNext())
                    {
                        // length one
                        this.span = span;
                        if (span.End > snapshot.Length)
                        {
                            throw new ArgumentException(Strings.SpansBeyondEnd);
                        }
                    }
                    else
                    {
                        // length at least two
                        this.spans = new NormalizedSpanCollection(spans);
                        if (this.spans[this.spans.Count - 1].End > snapshot.Length)
                        {
                            throw new ArgumentException(Strings.SpansBeyondEnd);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of a <see cref="NormalizedSnapshotSpanCollection"/> from a list of <see cref="Span"/>s and a <see cref="ITextSnapshot"/>.
        /// </summary>
        /// <param name="snapshot">The <see cref="ITextSnapshot"/> to apply to <paramref name="spans"/>.</param>
        /// <param name="spans">An arbitrary set of <see cref="Span"/> objects.</param>
        /// <exception cref="ArgumentNullException"><paramref name="snapshot"/> or <paramref name="spans"/> is null.</exception>
        /// <exception cref="ArgumentException">The spans in <paramref name="spans"/> extend beyond the end of <paramref name="snapshot"/>.</exception>
        public NormalizedSnapshotSpanCollection(ITextSnapshot snapshot, IList<Span> spans)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }
            if (spans == null)
            {
                throw new ArgumentNullException(nameof(spans));
            }

            if (spans.Count == 0)
            {
                // empty collection
            }
            else
            {
                this.snapshot = snapshot;
                if (spans.Count == 1)
                {
                    // length one
                    this.span = spans[0];
                    if (this.span.End > snapshot.Length)
                    {
                        throw new ArgumentException(Strings.SpansBeyondEnd);
                    }
                }
                else
                {
                    // length at least two
                    this.spans = new NormalizedSpanCollection(spans);
                    if (this.spans[this.spans.Count - 1].End > snapshot.Length)
                    {
                        throw new ArgumentException(Strings.SpansBeyondEnd);
                    }
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of a <see cref="NormalizedSnapshotSpanCollection"/> from a list of <see cref="SnapshotSpan"/> objects.
        /// </summary>
        /// <param name="snapshotSpans">An arbitrary set of <see cref="SnapshotSpan"/> objects.</param>
        /// <exception cref="ArgumentNullException"><paramref name="snapshotSpans"/> is null.</exception>
        /// <exception cref="ArgumentException">A <see cref="SnapshotSpan"/> is uninitialized, or it
        /// does not refer to the same <see cref="ITextSnapshot"/> as the other snapshot spans, or it refers to an uninitialized <see cref="ITextSnapshot"/>.</exception>
        public NormalizedSnapshotSpanCollection(IEnumerable<SnapshotSpan> snapshotSpans)
        {
            if (snapshotSpans == null)
            {
                throw new ArgumentNullException(nameof(snapshotSpans));
            }

            using (IEnumerator<SnapshotSpan> spanEnumerator = snapshotSpans.GetEnumerator())
            {
                if (!spanEnumerator.MoveNext())
                {
                    // empty
                }
                else
                {
                    SnapshotSpan firstSpan = spanEnumerator.Current;
                    this.snapshot = firstSpan.Snapshot;
                    if (!spanEnumerator.MoveNext())
                    {
                        // length one
                        this.span = firstSpan.Span;
                    }
                    else
                    {
                        // length at least two
                        bool alreadyNormalized = true;
                        List<Span> spans = new List<Span>();
                        Span currentSpan = firstSpan.Span;
                        spans.Add(currentSpan);
                        int lastEnd = currentSpan.End;
                        do
                        {
                            SnapshotSpan snapshotSpan = spanEnumerator.Current;
                            if (snapshotSpan.Snapshot != this.snapshot)
                            {
                                if (snapshotSpan.Snapshot == null)
                                {
                                    throw new ArgumentException(Strings.UninitializedSnapshotSpan);
                                }
                                else
                                {
                                    throw new ArgumentException(Strings.InvalidSnapshot);
                                }
                            }
                            currentSpan = snapshotSpan.Span;
                            spans.Add(currentSpan);
                            if (currentSpan.Start <= lastEnd)
                            {
                                alreadyNormalized = false;
                            }
                            lastEnd = currentSpan.End;
                        } while (spanEnumerator.MoveNext()) ;
                        this.spans = alreadyNormalized
                                        ? NormalizedSpanCollection.CreateFromNormalizedSpans(spans)
                                        : new NormalizedSpanCollection(spans);
                    }
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of a <see cref="NormalizedSnapshotSpanCollection"/> from a list of <see cref="SnapshotSpan"/> objects.
        /// </summary>
        /// <param name="snapshotSpans">An arbitrary set of <see cref="SnapshotSpan"/> objects.</param>
        /// <exception cref="ArgumentNullException"><paramref name="snapshotSpans"/> is null.</exception>
        /// <exception cref="ArgumentException">A <see cref="SnapshotSpan"/> is uninitialized, or it
        /// does not refer to the same <see cref="ITextSnapshot"/> as the other snapshot spans, or it refers to an uninitialized <see cref="ITextSnapshot"/>.</exception>
        public NormalizedSnapshotSpanCollection(IList<SnapshotSpan> snapshotSpans)
        {
            // TODO: possibly eliminate based on slight usage?
            if (snapshotSpans == null)
            {
                throw new ArgumentNullException(nameof(snapshotSpans));
            }

            if (snapshotSpans.Count == 0)
            {
                // empty collection
            }
            else
            {
                this.snapshot = snapshotSpans[0].Snapshot;
                if (this.snapshot == null)
                {
                    throw new ArgumentException(Strings.UninitializedSnapshotSpan);
                }
                if (snapshotSpans.Count == 1)
                {
                    // length one
                    this.span = snapshotSpans[0].Span;
                }
                else
                {
                    // length at least two
                    bool alreadyNormalized = true;
                    List<Span> spans = new List<Span>(snapshotSpans.Count);
                    Span currentSpan = snapshotSpans[0].Span;
                    spans.Add(currentSpan);
                    int lastEnd = currentSpan.End;
                    for (int s = 1; s < snapshotSpans.Count; ++s)
                    {
                        if (snapshotSpans[s].Snapshot != this.snapshot)
                        {
                            if (snapshotSpans[s].Snapshot == null)
                            {
                                throw new ArgumentException(Strings.UninitializedSnapshotSpan);
                            }
                            else
                            {
                                throw new ArgumentException(Strings.InvalidSnapshot);
                            }
                        }
                        currentSpan = snapshotSpans[s].Span;
                        spans.Add(currentSpan);
                        if (currentSpan.Start <= lastEnd)
                        {
                            alreadyNormalized = false;
                        }
                        lastEnd = currentSpan.End;
                    }
                    this.spans = alreadyNormalized
                                    ? NormalizedSpanCollection.CreateFromNormalizedSpans(spans)
                                    : new NormalizedSpanCollection(spans);
                }
            }
        }

        public NormalizedSnapshotSpanCollection(ITextSnapshot snapshot, Span span)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            if (span.End > snapshot.Length)
            {
                throw new ArgumentException(Strings.SpansBeyondEnd);
            }

            this.snapshot = snapshot;
            this.span = span;
        }
        #endregion

        public NormalizedSnapshotSpanCollection CloneAndTrackTo(ITextSnapshot targetSnapshot, SpanTrackingMode mode)
        {
            if (targetSnapshot == null)
            {
                throw new ArgumentNullException(nameof(targetSnapshot));
            }
            if (mode < SpanTrackingMode.EdgeExclusive || mode > SpanTrackingMode.Custom)
            {
                throw new ArgumentOutOfRangeException(nameof(mode));
            }

            if (this.snapshot == null)
            {
                return NormalizedSnapshotSpanCollection.Empty;
            }
            else if (targetSnapshot.TextBuffer != this.snapshot.TextBuffer)
            {
                throw new ArgumentException("this.Snapshot and targetSnapshot must be from the same ITextBuffer");
            }
            else if (this.snapshot == targetSnapshot)
            {
                return this;
            }
            else if (this.spans == null)
            {
                Span targetSpan = targetSnapshot.Version.VersionNumber > this.snapshot.Version.VersionNumber
                                  ? Tracking.TrackSpanForwardInTime(mode, this.span, this.snapshot.Version, targetSnapshot.Version)
                                  : Tracking.TrackSpanBackwardInTime(mode, this.span, this.snapshot.Version, targetSnapshot.Version);

                return new NormalizedSnapshotSpanCollection(targetSnapshot, targetSpan);
            }
            else
            {
                var targetSpans = new Span[this.spans.Count];
                for (int i = 0; (i < this.spans.Count); ++i)
                {
                    targetSpans[i] = targetSnapshot.Version.VersionNumber > this.snapshot.Version.VersionNumber
                                     ? Tracking.TrackSpanForwardInTime(mode, this.spans[i], this.snapshot.Version, targetSnapshot.Version)
                                     : Tracking.TrackSpanBackwardInTime(mode, this.spans[i], this.snapshot.Version, targetSnapshot.Version);
                }

                return new NormalizedSnapshotSpanCollection(targetSnapshot, targetSpans);
            }
        }

        #region Implicit Conversion
        /// <summary>
        /// Converts the specified <see cref="NormalizedSnapshotSpanCollection"/> to a <see cref="NormalizedSpanCollection"/>.
        /// </summary>
        /// <param name="spans">The collection to convert.</param>
        /// <returns>A <see cref="NormalizedSpanCollection"/> containing the corresponding normalized collection of <see cref="Span"/> objects.</returns>
        public static implicit operator NormalizedSpanCollection(NormalizedSnapshotSpanCollection spans)
        {
            if (spans == null)
            {
                return null;
            }
            else if (spans.spans != null)
            {
                // length greater than one
                return spans.spans;
            }
            else if (spans.snapshot != null)
            {
                // length one
                return new NormalizedSpanCollection(spans.span);
            }
            else
            {
                // length zero;
                return NormalizedSpanCollection.Empty;
            }
        }
        #endregion

        #region Set Operations
        /// <summary>
        /// Computes the union of two snapshot span collections and normalizes the result.
        /// </summary>
        /// <param name="left">The first <see cref="NormalizedSnapshotSpanCollection"/>.</param>
        /// <param name="right">The second <see cref="NormalizedSnapshotSpanCollection"/>.</param>
        /// <returns>The normalized union of the input collections.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="left"/> or <paramref name="right"/> is null.</exception>
        /// <exception cref="ArgumentException">The collections refer to different snapshots.</exception>
        public static NormalizedSnapshotSpanCollection Union(NormalizedSnapshotSpanCollection left, NormalizedSnapshotSpanCollection right)
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

            if (left.snapshot != right.snapshot)
            {
                throw new ArgumentException(Strings.MismatchedSnapshots);
            }

            NormalizedSpanCollection leftSpans = left.spans ?? new NormalizedSpanCollection(left.span);
            NormalizedSpanCollection rightSpans = right.spans ?? new NormalizedSpanCollection(right.span);

            return new NormalizedSnapshotSpanCollection(left[0].Snapshot, NormalizedSpanCollection.Union(leftSpans, rightSpans));
        }

        /// <summary>
        /// Computes the overlap of two normalized snapshot span collections and normalizes the result.
        /// </summary>
        /// <param name="left">The first <see cref="NormalizedSnapshotSpanCollection"/>.</param>
        /// <param name="right">The second <see cref="NormalizedSnapshotSpanCollection"/></param>
        /// <returns>The normalized set of overlapping snapshot spans.</returns>
        /// <remarks>Empty SnapshotSpans never overlap any other SnapshotSpan.</remarks>
        /// <exception cref="ArgumentNullException"><paramref name="left"/> or <paramref name="right"/> is null.</exception>
        /// <exception cref="ArgumentException">The input collections refer to different snapshots.</exception>
        public static NormalizedSnapshotSpanCollection Overlap(NormalizedSnapshotSpanCollection left, NormalizedSnapshotSpanCollection right)
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

            if (left.snapshot != right.snapshot)
            {
                throw new ArgumentException(Strings.MismatchedSnapshots);
            }

            NormalizedSpanCollection leftSpans = left.spans ?? new NormalizedSpanCollection(left.span);
            NormalizedSpanCollection rightSpans = right.spans ?? new NormalizedSpanCollection(right.span);

            return new NormalizedSnapshotSpanCollection(left[0].Snapshot, NormalizedSpanCollection.Overlap(leftSpans, rightSpans));
        }

        /// <summary>
        /// Computes the intersection of two normalized snapshot span collections and normalizes the result.
        /// </summary>
        /// <param name="left">The first <see cref="NormalizedSnapshotSpanCollection"/>.</param>
        /// <param name="right">The second<see cref="NormalizedSnapshotSpanCollection"/>.</param>
        /// <returns>The normalized set of intersecting spans.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="left"/> or <paramref name="right"/> is null.</exception>
        /// <exception cref="ArgumentException">The collections refer to different snapshots.</exception>
        public static NormalizedSnapshotSpanCollection Intersection(NormalizedSnapshotSpanCollection left, NormalizedSnapshotSpanCollection right)
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

            if (left.snapshot != right.snapshot)
            {
                throw new ArgumentException(Strings.MismatchedSnapshots);
            }

            NormalizedSpanCollection leftSpans = left.spans ?? new NormalizedSpanCollection(left.span);
            NormalizedSpanCollection rightSpans = right.spans ?? new NormalizedSpanCollection(right.span);

            return new NormalizedSnapshotSpanCollection(left[0].Snapshot, NormalizedSpanCollection.Intersection(leftSpans, rightSpans));
        }

        /// <summary>
        /// Computes the difference between two normalized snapshot span collections and normalizes the result.
        /// </summary>
        /// <param name="left">The collection from which to subtract <paramref name="right"/>.</param>
        /// <param name="right">The collection to subtract from <paramref name="left"/>.</param>
        /// <returns>The normalized set difference.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="left"/> or <paramref name="right"/> is null.</exception>
        /// <exception cref="ArgumentException">The input collections refer to different snapshots.</exception>
        public static NormalizedSnapshotSpanCollection Difference(NormalizedSnapshotSpanCollection left, NormalizedSnapshotSpanCollection right)
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

            if (left.snapshot != right.snapshot)
            {
                throw new ArgumentException(Strings.MismatchedSnapshots);
            }

            NormalizedSpanCollection leftSpans = left.spans ?? new NormalizedSpanCollection(left.span);
            NormalizedSpanCollection rightSpans = right.spans ?? new NormalizedSpanCollection(right.span);

            return new NormalizedSnapshotSpanCollection(left[0].Snapshot, NormalizedSpanCollection.Difference(leftSpans, rightSpans));
        }

        /// <summary>
        /// Determines whether this collection overlaps with another normalized snapshot span collection.
        /// </summary>
        /// <param name="set">The collection.</param>
        /// <returns><c>true</c> if the collections refer to the same snapshot and their spans overlap, otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="set"/> is null.</exception>
        /// <exception cref="ArgumentException">The collections refer to different snapshots.</exception>
        public bool OverlapsWith(NormalizedSnapshotSpanCollection set)
        {
            if (set == null)
            {
                throw new ArgumentNullException(nameof(set));
            }
            else if (set.Count == 0 || this.Count == 0)
            {
                return false;
            }
            else if (set.snapshot != this.snapshot)
            {
                throw new ArgumentException(Strings.MismatchedSnapshots);
            }
            else
            {
                NormalizedSpanCollection thisSpans = this.spans ?? new NormalizedSpanCollection(this.span);
                return thisSpans.OverlapsWith(set);
            }
        }

        /// <summary>
        /// Determines whether this collection overlaps with a snapshot span.
        /// </summary>
        /// <param name="span">The snapshot span to test.</param>
        /// <returns><c>true</c> if the collection and the span refer to the same snapshot and their spans overlap, otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentException">The collection and the span refer to different snapshots.</exception>
        public bool OverlapsWith(SnapshotSpan span)
        {
            if (this.snapshot == null)
            {
                // we are size zero
                return false;
            }
            else if (span.Snapshot != this.snapshot)
            {
                throw new ArgumentException(Strings.MismatchedSnapshots);
            }
            else if (this.spans != null)
            {
                return this.spans.OverlapsWith(span.Span);
            }
            else
            {
                return this.span.OverlapsWith(span.Span);
            }
        }

        /// <summary>
        /// Determines whether this collection intersects with another normalized snapshot span collection.
        /// </summary>
        /// <param name="set">The colllection.</param>
        /// <returns><c>true</c> if the collections intersect, otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="set"/> is null.</exception>
        /// <exception cref="ArgumentException">The input collections refer to different snapshots.</exception>
        public bool IntersectsWith(NormalizedSnapshotSpanCollection set)
        {
            if (set == null)
            {
                throw new ArgumentNullException(nameof(set));
            }
            else if (set.Count == 0 || this.Count == 0)
            {
                return false;
            }
            else if (set.snapshot != this.snapshot)
            {
                throw new ArgumentException(Strings.MismatchedSnapshots);
            }
            else
            {
                NormalizedSpanCollection thisSpans = this.spans ?? new NormalizedSpanCollection(this.span);
                return thisSpans.IntersectsWith(set);
            }
        }

        /// <summary>
        /// Determines whether this collection overlaps with a snapshot span.
        /// </summary>
        /// <param name="span">The snapshot span to test.</param>
        /// <returns><c>true</c> if the collection and the span refer to the same snapshot and their spans overlap, otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentException">The collection and the span refer to different snapshots.</exception>
        public bool IntersectsWith(SnapshotSpan span)
        {
            if (this.snapshot == null)
            {
                // we are size zero
                return false;
            }
            else if (span.Snapshot != this.snapshot)
            {
                throw new ArgumentException(Strings.MismatchedSnapshots);
            }
            else if (this.spans != null)
            {
                return this.spans.IntersectsWith(span);
            }
            else
            {
                return this.span.IntersectsWith(span.Span);
            }
        }
        #endregion
        
        #region IList<SnapshotSpan> Members

        /// <summary>
        /// Gets the index of the specified <see cref="SnapshotSpan"/>.
        /// </summary>
        /// <param name="item">The <see cref="SnapshotSpan"/>.</param>
        /// <returns>The index of the snapshot span.</returns>
        public int IndexOf(SnapshotSpan item)
        {
            if (this.snapshot == item.Snapshot)
            {
                if (this.spans != null)
                {
                    return this.spans.IndexOf(item.Span);
                }
                else if (this.snapshot != null && this.span == item.Span)
                {
                    return 0;
                }
            }
            return -1;
        }

        /// <summary>
        /// Inserts a snapshot span into the list. This method throws a <see cref="NotSupportedException"/>.
        /// </summary>
        /// <param name="index">The location at which to insert the snapshot span.</param>
        /// <param name="item">The snapshot span to insert.</param>
        void IList<SnapshotSpan>.Insert(int index, SnapshotSpan item)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Removes a snapshot span at the specified location. This method throws a <see cref="NotSupportedException"/>.
        /// </summary>
        /// <param name="index">The location at which to remove the snapshot span.</param>
        void IList<SnapshotSpan>.RemoveAt(int index)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Gets the snapshot span at the specified location. The setter throws a <see cref="NotSupportedException"/>.
        /// </summary>
        /// <param name="index">The location at which to get the snapshot span.</param>
        /// <returns>The snapshot span.</returns>
        public SnapshotSpan this[int index]
        {
            get
            {
                if (this.spans != null)
                {
                    return new SnapshotSpan(this.snapshot, this.spans[index]);
                }
                else if (this.snapshot != null && index == 0)
                {
                    return new SnapshotSpan(this.snapshot, this.span);
                }
                else
                {
                    // Analyzer has a bug where it is giving a false positive in this location.
#pragma warning disable CA1065 // Do not raise exceptions in unexpected locations
                    throw new ArgumentOutOfRangeException(nameof(index));
#pragma warning restore CA1065 // Do not raise exceptions in unexpected locations
                }
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        #endregion

        #region ICollection<SnapshotSpan> Members

        /// <summary>
        /// Adds a snapshot span to the collection. This method throws a <see cref="NotSupportedException"/>.
        /// </summary>
        /// <param name="item">The snapshot span.</param>
        void ICollection<SnapshotSpan>.Add(SnapshotSpan item)
        {
            throw new NotSupportedException();
        }


        /// <summary>
        /// Clears the collection. This method throws a <see cref="NotSupportedException"/>.
        /// </summary>
        void ICollection<SnapshotSpan>.Clear()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Determines whether the collection contains the specified snapshot span.
        /// </summary>
        /// <param name="item">The snapshot span.</param>
        /// <returns><c>true</c> if the collection contains the snapshot span, otherwise <c>false</c>.</returns>
        public bool Contains(SnapshotSpan item)
        {
            if (this.spans != null)
            {
                return item.Snapshot == this.snapshot && this.spans.Contains(item);
            }
            else if (this.snapshot == null)
            {
                return false;
            }
            else
            {
                return item.Snapshot == this.snapshot && item.Span == this.span;
            }
        }

        /// <summary>
        /// Copies the collection to an array of snapshot spans at the specified location.
        /// </summary>
        /// <param name="array">The array of snapshot spans.</param>
        /// <param name="arrayIndex">The location to which to copy the snapshot spans.</param>
        /// <exception cref="ArgumentNullException"><paramref name="array"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="arrayIndex"/> is negative or greater than the array length, 
        /// or the number of spans in the collection is greater than the length of the array minus the array index.</exception>
        public void CopyTo(SnapshotSpan[] array, int arrayIndex)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }
            if (arrayIndex < 0 || arrayIndex > array.Length || this.Count > array.Length - arrayIndex)
            {
                throw new ArgumentOutOfRangeException(nameof(arrayIndex));
            }
            if (this.spans != null)
            {
                for (int s = 0; s < this.spans.Count; ++s)
                {
                    array[arrayIndex++] = new SnapshotSpan(this.snapshot, this.spans[s]);
                }
            }
            else if (this.snapshot != null)
            {
                array[arrayIndex] = new SnapshotSpan(this.snapshot, this.span);
            }
        }

        /// <summary>
        /// Gets the number of spans in the collection.
        /// </summary>
        public int Count
        {
            get 
            {
                if (this.spans != null)
                {
                    return this.spans.Count;
                }
                else
                {
                    return this.snapshot == null ? 0 : 1;
                }
            }
        }

        /// <summary>
        /// Determines whether the collection is read-only. Always returns <c>true</c>.
       /// </summary>
        bool ICollection<SnapshotSpan>.IsReadOnly
        {
            get { return true; }
        }

        /// <summary>
        /// Removes the specified span from the collection. This method throws a <see cref="NotSupportedException"/>.
        /// </summary>
        /// <param name="item">The snapshot span.</param>
        /// <returns><c>true</c> if it was possible to remove the span.</returns>
        bool ICollection<SnapshotSpan>.Remove(SnapshotSpan item)
        {
            throw new NotSupportedException();
        }

        #endregion

        #region IEnumerable<SnapshotSpan> Members

        /// <summary>
        /// Gets an enumerator for the collection.
        /// </summary>
        /// <returns>The enumerator.</returns>
        public IEnumerator<SnapshotSpan> GetEnumerator()
        {
            if (this.spans != null)
            {
                foreach (Span span in this.spans)
                {
                    yield return new SnapshotSpan(this.snapshot, span);
                }
            }
            else if (this.snapshot != null)
            {
                yield return new SnapshotSpan(this.snapshot, this.span);
            }
        }

        #endregion

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            if (this.spans != null)
            {
                foreach (Span span in this.spans)
                {
                    yield return new SnapshotSpan(this.snapshot, span);
                }
            }
            else if (this.snapshot != null)
            {
                yield return new SnapshotSpan(this.snapshot, this.span);
            }
        }

        #endregion

        #region IList Members

        /// <summary>
        /// Adds an object to the list. This method throws a <see cref="NotSupportedException"/>.
        /// </summary>
        /// <param name="value">The object to add.</param>
        /// <returns>The location at which the object was added.</returns>
        int IList.Add(object value)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Clears the list. This method throws a <see cref="NotSupportedException"/>.
        /// </summary>
        void IList.Clear()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Determines whether the collection contains the specified snapshot span.
        /// </summary>
        /// <param name="value">The snapshot span.</param>
        /// <returns><c>true</c> if the snapshot span is contained in the collection, otherwise <c>false</c>.</returns>
        public bool Contains(object value)
        {
            if (value is SnapshotSpan)
            {
                SnapshotSpan val = (SnapshotSpan)value;
                if (this.spans != null)
                {
                    return this.snapshot == val.Snapshot && this.spans.Contains(val.Span);
                }
                else if (this.snapshot == null)
                {
                    return false;
                }
                else
                {
                    return this.snapshot == val.Snapshot && this.span == val.Span;
                }
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Gets the index of the specified snapshot span.
        /// </summary>
        /// <param name="value">The snapshot span.</param>
        /// <returns>The location of the snapshot span.</returns>
        public int IndexOf(object value)
        {
            if (value is SnapshotSpan)
            {
                SnapshotSpan val = (SnapshotSpan)value;
                if (this.snapshot == val.Snapshot)
                {
                    if (this.spans != null)
                    {
                        return this.spans.IndexOf(val.Span);
                    }
                    else if (this.snapshot != null && this.span == val.Span)
                    {
                        return 0;
                    }
                }
            }
            return -1;
        }

        /// <summary>
        /// Inserts a snapshot span into the list at the specified location. This method throws a <see cref="NotSupportedException"/>.
        /// </summary>
        /// <param name="index">The location.</param>
        /// <param name="value">The snapshot span.</param>
        void IList.Insert(int index, object value)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Determines whether the collection is of fixed size. Always returns <c>true</c>.
        /// </summary>
        bool IList.IsFixedSize
        {
            get { return true; }
        }

        /// <summary>
        /// Removes the specified snapshot span. This method throws a <see cref="NotSupportedException"/>.
        /// </summary> 
        /// <param name="value">The snapshot span.</param>
        void IList.Remove(object value)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Removes a snapshot span at the specified location. This method throws a <see cref="NotSupportedException"/>.
        /// </summary>
        /// <param name="index">The location.</param>
        void IList.RemoveAt(int index)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Gets the snapshot span at the specified location. The setter throws a <see cref="NotSupportedException"/>.
        /// </summary>
        /// <param name="index">The location.</param>
        /// <returns>The snapshot span.</returns>
        object IList.this[int index]
        {
            get
            {
                if (this.spans != null)
                {
                    return new SnapshotSpan(this.snapshot, this.spans[index]);
                }
                else if (this.snapshot != null && index == 0)
                {
                    return new SnapshotSpan(this.snapshot, this.span);
                }
                else
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        #endregion

        #region ICollection Members

        /// <summary>
        /// Copies the snapshot spans in this collection to the specified array, starting at the specified index.
        /// </summary>
        /// <param name="array">The array.</param>
        /// <param name="index">The location at which to start copying.</param>
        /// <exception cref="ArgumentNullException"><paramref name="array"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is negative, or greater than
        /// the length of the array, or the number of spans is greater than the length of the array less the index.</exception>
        /// <exception cref="ArgumentException"><paramref name="array"/> is not one-dimensional.</exception>
        public void CopyTo(Array array, int index)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }
            if (index < 0 || index > array.Length || this.Count > array.Length - index)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            if (array.Rank != 1)
            {
                throw new ArgumentException(Strings.ArrayRankNotOne);
            }
            if (this.spans != null)
            {
                for (int s = 0; s < this.spans.Count; ++s)
                {
                    array.SetValue(new SnapshotSpan(this.snapshot, this.spans[s]), index++);
                }
            }
            else if (this.snapshot != null)
            {
                array.SetValue(new SnapshotSpan(this.snapshot, this.span), index);
            }
        }

        /// <summary>
        /// Determines whether this collection is read-only. This property always returns <c>true</c>.
        /// </summary>
        bool IList.IsReadOnly
        {
            get { return true; }
        }

        /// <summary>
        /// Determines whether this collection is synchronized.
        /// </summary>
        bool ICollection.IsSynchronized
        {
            get { return true; }
        }

        /// <summary>
        /// Gets an object that can be used to synchronize access to this collection.
        /// </summary>
        object ICollection.SyncRoot
        {
            get 
            {
                return this.spans != null ? (this.spans as IList).SyncRoot : this;
            }
        }

        #endregion

        #region Operators and Overrides
        /// <summary>
        /// Determines whether two <see cref="NormalizedSnapshotSpanCollection"/> objects are the same.
        /// </summary>
        /// <param name="left">The first collection.</param>
        /// <param name="right">The second collection.</param>
        /// <returns><c>true</c> if the two sets are the same, otherwise <c>false</c>.</returns>
        public static bool operator ==(NormalizedSnapshotSpanCollection left, NormalizedSnapshotSpanCollection right)
        {
            if (object.ReferenceEquals(left, right))
            {
                return true;
            }
            if (object.ReferenceEquals(left, null) || object.ReferenceEquals(right, null))
            {
                return false;
            }

            if (left.Count != right.Count)
            {
                return false;
            }

            for (int i = 0; i < left.Count; ++i)
            {
                if (left[i] != right[i])
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Determines whether two <see cref="NormalizedSnapshotSpanCollection"/> are different..
        /// </summary>
        /// <param name="left">The first collection.</param>
        /// <param name="right">The second collection.</param>
        /// <returns><c>true</c> if the two collections are different.</returns>
        public static bool operator !=(NormalizedSnapshotSpanCollection left, NormalizedSnapshotSpanCollection right)
        {
            return !(left == right);
        }

        /// <summary>
        /// Gets a hash code for the collection.
        /// </summary>
        /// <returns>A 32-bit hash code associated with the collection.</returns>
        public override int GetHashCode()
        {
            return this.spans != null ? this.spans.GetHashCode() : this.span.GetHashCode();
        }

        /// <summary>
        /// Determines whether two snapshot span collections are equal
        /// </summary>
        /// <param name="obj">The second collection.</param>
        /// <returns><c>true</c> if the two collections are equal, otherwise <c>false</c>.</returns>
        public override bool Equals(object obj)
        {
            NormalizedSnapshotSpanCollection set = obj as NormalizedSnapshotSpanCollection;

            return this == set;
        }

        /// <summary>
        /// Converts the spans to a string..
        /// </summary>
        /// <returns>The string representation.</returns>
        public override string ToString()
        {
            return this.spans != null ? this.spans.ToString() : this.span.ToString();
        }
        #endregion

    }
}
