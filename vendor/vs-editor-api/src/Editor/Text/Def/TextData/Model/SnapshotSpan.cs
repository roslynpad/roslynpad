//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text
{
    using System;

#pragma warning disable CA1066 // Type {0} should implement IEquatable<T> because it overrides Equals
    /// <summary>
    /// An immutable text span in a particular text snapshot.
    /// </summary>
    public struct SnapshotSpan
#pragma warning restore CA1066 // Type {0} should implement IEquatable<T> because it overrides Equals
    {
        #region Private Members

        // Member must match order in the ctor, otherwise the COM tool gets confused.
        private SnapshotPoint start;
        private int length;

        #endregion // Private Members

        /// <summary>
        /// Initializes a new instance of a <see cref="SnapshotSpan"/> with the specified snapshot and span.
        /// </summary>
        /// <param name="snapshot">The <see cref="ITextSnapshot"/> on which to base the snapshot span.</param>
        /// <param name="span">The span of the snapshot span.</param>
        /// <exception cref="ArgumentNullException"><paramref name="snapshot"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="span"/>.End is greater than <paramref name="snapshot"/>.Length.</exception>
        public SnapshotSpan(ITextSnapshot snapshot, Span span)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }
            if (span.End > snapshot.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(span));
            }

            this.start = new SnapshotPoint(snapshot, span.Start);
            this.length = span.Length;
        }

        /// <summary>
        /// Initializes a new instance of a <see cref="SnapshotSpan"/> with the specified snapshot, start point, and length.
        /// </summary>
        /// <param name="snapshot">The text snapshot on which to base the snapshot span.</param>
        /// <param name="start">The starting point of the snapshot span.</param>
        /// <param name="length">The length of the snapshot span.</param>
        /// <exception cref="ArgumentNullException"><paramref name="snapshot"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="start"/> is negative or greater than <paramref name="snapshot"/>.Length, or
        /// <paramref name="length"/> is negative or <paramref name="start"/> + <paramref name="length"/> is greater than
        /// <paramref name="snapshot"/>.Length.</exception>
        public SnapshotSpan(ITextSnapshot snapshot, int start, int length)
            : this(snapshot, new Span(start, length))
        {
        }

        /// <summary>
        /// Initializes a new instance of a <see cref="SnapshotSpan"/> from two <see cref="SnapshotPoint"/> objects.
        /// </summary>
        /// <param name="start">The start point.</param>
        /// <param name="end">The end point, which must be from the same <see cref="ITextSnapshot"/>
        /// as the start point.</param>
        /// <exception cref="ArgumentException">The snapshot points belong to different 
        /// <see cref="ITextSnapshot"/> objects.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The end point comes before the start
        /// point.</exception>
        public SnapshotSpan(SnapshotPoint start, SnapshotPoint end)
        {
            if (start.Snapshot == null || end.Snapshot == null)
            {
                throw new ArgumentException(Strings.UninitializedSnapshotPoint);
            }
            if (start.Snapshot != end.Snapshot)
            {
                throw new ArgumentException(Strings.MismatchedSnapshotPoints);
            }
            if (end.Position < start.Position)
            {
                throw new ArgumentOutOfRangeException(nameof(end));
            }

            this.start = start;
            this.length = (end - start);
        }

        /// <summary>
        /// Initializes a new instance of a <see cref="SnapshotSpan"/> from an existing <see cref="SnapshotPoint"/> and a specified length.
        /// </summary>
        /// <param name="start">The starting snapshot point.</param>
        /// <param name="length">The length of the span.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="length"/> is negative or 
        /// <paramref name="start"/> + <paramref name="length"/> is greater than the length of the snapshot.
        /// </exception>
        public SnapshotSpan(SnapshotPoint start, int length)
        {
            if (length < 0 ||
                start.Position + length > start.Snapshot.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            this.start = start;
            this.length = length;
        }

        /// <summary>
        /// Implicitly converts a snapshot span to a span.
        /// </summary>
        public static implicit operator Span(SnapshotSpan snapshotSpan)
        {
            return snapshotSpan.Span;
        }

        /// <summary>
        /// The <see cref="ITextSnapshot"/> to which this snapshot span refers.
        /// </summary>
        public ITextSnapshot Snapshot
        {
            get { return this.start.Snapshot; }
        }

        /// <summary>
        /// The text contained by this snapshot span.
        /// </summary>
        /// <returns>A non-null string.</returns>
        public string GetText()
        {
            return this.Snapshot.GetText(this.Span);
        }

        /// <summary>
        /// Translates this snapshot span to a different snapshot of the same <see cref="ITextBuffer"/>.
        /// </summary>
        /// <param name="targetSnapshot">The snapshot to which to translate.</param>
        /// <param name="spanTrackingMode">The <see cref="SpanTrackingMode"/> to use in the translation.</param>
        /// <returns>A new snapshot span.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="targetSnapshot"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="targetSnapshot"/> does not refer to the same <see cref="ITextBuffer"/> as this snapshot span.</exception>
        public SnapshotSpan TranslateTo(ITextSnapshot targetSnapshot, SpanTrackingMode spanTrackingMode)
        {
            if (targetSnapshot == this.Snapshot)
            {
                return this;
            }
            else
            {
                if (targetSnapshot == null)
                {
                    throw new ArgumentNullException(nameof(targetSnapshot));
                }
                if (targetSnapshot.TextBuffer != this.Start.Snapshot.TextBuffer)
                {
                    throw new ArgumentException(Strings.InvalidSnapshot);
                }

                Span targetSpan = targetSnapshot.Version.VersionNumber > this.Snapshot.Version.VersionNumber
                                    ? Tracking.TrackSpanForwardInTime(spanTrackingMode, this.Span, this.Snapshot.Version, targetSnapshot.Version)
                                    : Tracking.TrackSpanBackwardInTime(spanTrackingMode, this.Span, this.Snapshot.Version, targetSnapshot.Version);

                return new SnapshotSpan(targetSnapshot, targetSpan);
            }
        }

        #region Reimplementation of Span methods and properties

        /// <summary>
        /// Gets the span covered by the snapshot span.
        /// </summary>
        public Span Span
        {
            get { return new Span(this.start, this.length); }
        }

        /// <summary>
        /// Gets the starting index of the snapshot span.
        /// </summary>
        public SnapshotPoint Start
        {
            get 
            { 
                return this.start;
            }
        }

        /// <summary>
        /// Gets the end of the snapshot span. The span is open-ended on the right side, which is to say
        /// that Start + Length = End.
        /// </summary>
        public SnapshotPoint End
        {
            get 
            {
                return this.start + this.length;
            }
        }

        /// <summary>
        /// Gets the length of the span, which is always non-negative.
        /// </summary>
        public int Length
        {
            get { return this.length; }
        }

        /// <summary>
        /// Determines whether or not this span is empty.
        /// </summary>
        /// <value><c>true</c> if the length of the span is zero, otherwise <c>false</c>.</value>
        public bool IsEmpty
        {
            get { return this.Length == 0; }
        }

        /// <summary>
        /// Determines whether the position lies within the span.
        /// </summary>
        /// <param name="position">
        /// The position to check.
        /// </param>
        /// <returns>
        /// <c>true</c> if the position is greater than or equal to parameter span.Start and strictly less than parameter span.End, otherwise <c>false</c>.
        /// </returns>
        public bool Contains(int position)
        {
            return this.Span.Contains(position);
        }

        /// <summary>
        /// Determines whether a given <see cref="SnapshotPoint"/> lies within the span.
        /// </summary>
        /// <param name="point">
        /// The point to check.
        /// </param>
        /// <returns>
        /// <c>true</c> if the position is greater than or equal to parameter span.Start and strictly less than parameter span.End, otherwise <c>false</c>.
        /// </returns>
        public bool Contains(SnapshotPoint point)
        {
            this.EnsureSnapshot(point.Snapshot);

            return this.Span.Contains(point.Position);
        }

        /// <summary>
        /// Determines whether <paramref name="simpleSpan"/> falls completely within this span.
        /// </summary>
        /// <param name="simpleSpan">
        /// The span to check.
        /// </param>
        /// <returns>
        /// <c>true</c> if the specified span falls completely within this span, otherwise <c>false</c>.
        /// </returns>
        public bool Contains(Span simpleSpan)
        {
            return this.Span.Contains(simpleSpan);
        }

        /// <summary>
        /// Determines whether <paramref name="snapshotSpan"/> falls completely within this span.
        /// </summary>
        /// <param name="snapshotSpan">
        /// The span to check.
        /// </param>
        /// <returns>
        /// <c>true</c> if the specified span falls completely within this span, otherwise <c>false</c>.
        /// </returns>
        public bool Contains(SnapshotSpan snapshotSpan)
        {
            this.EnsureSnapshot(snapshotSpan.Snapshot);

            return this.Span.Contains(snapshotSpan.Span);
        }

        /// <summary>
        /// Determines whether <paramref name="simpleSpan"/> overlaps this span. Two spans are considered to overlap if they have positions in common and are not empty. 
        /// Empty spans do not overlap with any other span.
        /// </summary>
        /// <param name="simpleSpan">
        /// The span to check.
        /// </param>
        /// <returns>
        /// <c>true</c> if the spans overlap, otherwise <c>false</c>.
        /// </returns>
        public bool OverlapsWith(Span simpleSpan)
        {
            return this.Span.OverlapsWith(simpleSpan);
        }

        /// <summary>
        /// Determines whether <paramref name="snapshotSpan"/> overlaps this span. 
        /// Two spans are considered to overlap if they have positions in common and are not empty. Empty spans do not overlap with any other span.
        /// </summary>
        /// <param name="snapshotSpan">
        /// The span to check for overlap.
        /// </param>
        /// <returns>
        /// <c>true</c> if the spans overlap, otherwise <c>false</c>.
        /// </returns>
        public bool OverlapsWith(SnapshotSpan snapshotSpan)
        {
            this.EnsureSnapshot(snapshotSpan.Snapshot);

            return this.Span.OverlapsWith(snapshotSpan.Span);
        }

        /// <summary>
        /// Returns the overlap with the given span, or null if there is no overlap.
        /// </summary>
        /// <param name="simpleSpan">The span to check.</param>
        /// <returns>The overlap of the spans, or null if the overlap is empty.</returns>
        public SnapshotSpan? Overlap(Span simpleSpan)
        {
            Span? overlap = this.Span.Overlap(simpleSpan);

            if (overlap != null)
            {
                return new SnapshotSpan(this.Snapshot, overlap.Value);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Returns the overlap with the given <see cref="SnapshotSpan"/>, or null if there is no overlap.
        /// </summary>
        /// <param name="snapshotSpan">The span to check.</param>
        /// <exception cref="ArgumentException"><paramref name="snapshotSpan"/> does not refer to the same
        /// <see cref="ITextSnapshot"/> as this snapshot span.</exception>
        /// <returns>The overlap of the spans, or null if the overlap is empty.</returns>
        public SnapshotSpan? Overlap(SnapshotSpan snapshotSpan)
        {
            this.EnsureSnapshot(snapshotSpan.Snapshot);

            return this.Overlap(snapshotSpan.Span);
        }

        /// <summary>
        /// Determines whether <paramref name="simpleSpan"/> intersects this span. Two spans are considered to 
        /// intersect if they have positions in common, or if the end of one span 
        /// coincides with the start of the other span, and neither is empty.
        /// </summary>
        /// <param name="simpleSpan">
        /// The span to check.
        /// </param>
        /// <returns>
        /// <c>true</c> if the spans intersect, otherwise <c>false</c>.
        /// </returns>
        public bool IntersectsWith(Span simpleSpan)
        {
            return this.Span.IntersectsWith(simpleSpan);
        }

        /// <summary>
        /// Determines whether <paramref name="snapshotSpan"/> intersects this span. Two spans are considered to 
        /// intersect if they have positions in common, or the end of one span 
        /// coincides with the start of the other span, and neither is empty.
        /// </summary>
        /// <param name="snapshotSpan">
        /// The span to check.
        /// </param>
        /// <returns>
        /// <c>true</c> if the spans intersect, otherwise <c>false</c>.
        /// </returns>
        public bool IntersectsWith(SnapshotSpan snapshotSpan)
        {
            this.EnsureSnapshot(snapshotSpan.Snapshot);

            return this.Span.IntersectsWith(snapshotSpan.Span);
        }

        /// <summary>
        /// Computes the intersection with the given span, or null if there is no intersection.
        /// </summary>
        /// <param name="simpleSpan">
        /// The span to check.
        /// </param>
        /// <returns>
        /// The intersection of the spans, or null if the intersection is empty.
        /// </returns>
        public SnapshotSpan? Intersection(Span simpleSpan)
        {
            Span? intersection = this.Span.Intersection(simpleSpan);
            
            if (intersection != null)
            {
                return new SnapshotSpan(this.Snapshot, intersection.Value);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Computes the intersection with the given <see cref="SnapshotSpan"/>, or null if there is no intersection.
        /// </summary>
        /// <param name="snapshotSpan">
        /// The span to check.
        /// </param>
        /// <exception cref="ArgumentException"><paramref name="snapshotSpan"/> does not refer to the same snapshot. </exception>
        /// <returns>
        /// The intersection of the spans, or null if the intersection is empty.
        /// </returns>
        public SnapshotSpan? Intersection(SnapshotSpan snapshotSpan)
        {
            this.EnsureSnapshot(snapshotSpan.Snapshot);

            return this.Intersection(snapshotSpan.Span);
        }

        #endregion // Reimplementation of Span methods and properties

        #region Overridden methods and operators

        /// <summary>
        /// Serves as a hash function for this type.
        /// </summary>
        public override int GetHashCode()
        {
            return (this.Snapshot != null) ? (this.Span.GetHashCode() ^ this.Snapshot.GetHashCode()) : 0;
        }

        /// <summary>
        /// Converts this snapshot span to a string, or to the string "uninit" if the <see cref="ITextSnapshot"/> is null.
        /// </summary>
        public override string ToString()
        {
            if (this.Snapshot == null)
            {
                return "uninit";
            }
            else
            {
                string tag;
                this.Snapshot.TextBuffer.Properties.TryGetProperty("tag", out tag);
                return string.Format(System.Globalization.CultureInfo.CurrentCulture,
                                     "{0}_v{1}_{2}_'{3}'",
                                     tag ?? "?",
                                     this.Snapshot.Version.VersionNumber,
                                     this.Span.ToString(),
                                     this.Length < 40
                                         ? this.GetText()
                                         : this.Snapshot.GetText(this.Start, 40));
            }
        }

        /// <summary>
        /// Determines whether two snapshot spans are the same.
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj is SnapshotSpan)
            {
                var other = (SnapshotSpan)obj;
                return other == this;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Determines whether two snapshot spans are the same.
        /// </summary>
        public static bool operator ==(SnapshotSpan left, SnapshotSpan right)
        {
            return left.Snapshot == right.Snapshot && left.Span == right.Span;
        }

        /// <summary>
        /// Determines whether two snapshot spans are different.
        /// </summary>
        public static bool operator !=(SnapshotSpan left, SnapshotSpan right)
        {
            return !(left == right);
        }

        #endregion // Overridden methods and operators

        #region Private helpers

        private void EnsureSnapshot(ITextSnapshot requestedSnapshot)
        {
            if (this.Snapshot != requestedSnapshot)
            {
                throw new ArgumentException(Strings.InvalidSnapshotSpan);
            }
        }

        #endregion
    }
}
