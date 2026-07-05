//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text
{
    using System;

#pragma warning disable CA1066 // Type {0} should implement IEquatable<T> because it overrides Equals
    /// <summary>
    /// Represents two <see cref="VirtualSnapshotPoint" />s
    /// </summary>
    public struct VirtualSnapshotSpan
#pragma warning restore CA1066 // Type {0} should implement IEquatable<T> because it overrides Equals
    {
        private readonly VirtualSnapshotPoint _start;
        private readonly VirtualSnapshotPoint _end;

        /// <summary>
        /// Initializes a new instance of a <see cref="VirtualSnapshotSpan"/> at <paramref name="snapshotSpan"/>, with no virtual spaces.
        /// </summary>
        /// <param name="snapshotSpan">A snapshot span.</param>
        public VirtualSnapshotSpan(SnapshotSpan snapshotSpan)
        {
            _start = new VirtualSnapshotPoint(snapshotSpan.Start);
            _end = new VirtualSnapshotPoint(snapshotSpan.End);
        }

        /// <summary>
        /// Initializes a new instance of a <see cref="VirtualSnapshotSpan"/> from the given
        /// <see cref="VirtualSnapshotPoint" />s.
        /// </summary>
        /// <param name="start">The start point.</param>
        /// <param name="end">The end point, which must be from the same <see cref="ITextSnapshot"/>
        /// as the start point.</param>
        /// <exception cref="ArgumentException">The snapshot points belong to different 
        /// <see cref="ITextSnapshot"/> objects.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The end point comes before the start
        /// point.</exception>
        public VirtualSnapshotSpan(VirtualSnapshotPoint start, VirtualSnapshotPoint end)
        {
            if (start.Position.Snapshot == null || end.Position.Snapshot == null)
            {
                throw new ArgumentException("The VirtualSnapshotPoint is not initialized.");
            }
            if (start.Position.Snapshot != end.Position.Snapshot)
            {
                throw new ArgumentException("The specified VirtualSnapshotPoints belong to different ITextSnapshots.");
            }
            if (end < start)
            {
                throw new ArgumentOutOfRangeException(nameof(end));
            }

            _start = start;
            _end = end;
        }

        /// <summary>
        /// Gets the starting virtual point.
        /// </summary>
        public VirtualSnapshotPoint Start
        {
            get { return _start; }
        }

        /// <summary>
        /// Gets the ending virtual point.
        /// </summary>
        public VirtualSnapshotPoint End
        {
            get { return _end; }
        }

        /// <summary>
        /// The <see cref="ITextSnapshot"/> to which this snapshot span refers.
        /// </summary>
        public ITextSnapshot Snapshot
        {
            get { return _start.Position.Snapshot; }
        }

        /// <summary>
        /// The length of this span, taking into account virtual space.
        /// </summary>
        /// <remarks>
        /// If neither endpoint is in virtual space or only the start point is
        /// in virtual space, this will be equivalent to SnapshotSpan.Length.
        /// Otherwise, it will include virtual space.
        /// </remarks>
        public int Length
        {
            get
            {
                // If _start and _end are at the same virtual start point, then
                // the length of this span is the distance between them in virtual spaces.
                if (_start.Position == _end.Position)
                {
                    return _end.VirtualSpaces - _start.VirtualSpaces;
                }
                else
                {
                    return this.SnapshotSpan.Length + _end.VirtualSpaces;
                }
            }
        }

        /// <summary>
        /// The text contained by this virtual snapshot span.
        /// </summary>
        /// <returns>A non-null string.</returns>
        public string GetText()
        {
            return this.SnapshotSpan.GetText();
        }

        /// <summary>
        /// Gets the non-virtual SnapshotSpan that this corresponds to.
        /// </summary>
        public SnapshotSpan SnapshotSpan
        {
            get { return new SnapshotSpan(_start.Position, _end.Position); }
        }

        /// <summary>
        /// Determines whether the start or end points are in virtual space.
        /// </summary>
        public bool IsInVirtualSpace
        {
            get { return _start.IsInVirtualSpace || _end.IsInVirtualSpace; }
        }

        /// <summary>
        /// Determines whether the start and end points are in the same place.
        /// </summary>
        /// <remarks>
        /// Because the start and end can both be in virtual space, the non-virtual
        /// span that this corresponds to can be non-empty at the same time that this
        /// property returns <c>true</c>.
        /// </remarks>
        public bool IsEmpty
        {
            get { return _start == _end; }
        }

        /// <summary>
        /// Determines whether or not the given virtual point is contained
        /// within this virtual span.
        /// </summary>
        /// <param name="virtualPoint">
        /// The virtual point to check.
        /// </param>
        /// <returns>
        /// <c>true</c> if the position is greater than or equal to Start and strictly less 
        /// than End, otherwise <c>false</c>.
        /// </returns>
        public bool Contains(VirtualSnapshotPoint virtualPoint)
        {
            return (virtualPoint >= _start && virtualPoint < _end);
        }

        /// <summary>
        /// Determines whether <paramref name="virtualSpan"/> falls completely within 
        /// this virtual span.
        /// </summary>
        /// <param name="virtualSpan">
        /// The virtual span to check.
        /// </param>
        /// <returns>
        /// <c>true</c> if the specified span falls completely within this span,
        /// otherwise <c>false</c>.
        /// </returns>
        public bool Contains(VirtualSnapshotSpan virtualSpan)
        {
            return (virtualSpan._start >= _start && virtualSpan._end <= _end);
        }

        /// <summary>
        /// Determines whether <paramref name="virtualSpan"/> overlaps this span. Two spans are considered to overlap 
        /// if they have positions in common and neither is empty. Empty spans do not overlap with any 
        /// other span.
        /// </summary>
        /// <param name="virtualSpan">
        /// The virtual span to check.
        /// </param>
        /// <returns>
        /// <c>true</c> if the spans overlap, otherwise <c>false</c>.
        /// </returns>
        public bool OverlapsWith(VirtualSnapshotSpan virtualSpan)
        {
            VirtualSnapshotPoint overlapStart = (_start > virtualSpan._start) ? _start : virtualSpan._start;
            VirtualSnapshotPoint overlapEnd = (_end < virtualSpan._end) ? _end : virtualSpan._end;

            return overlapStart < overlapEnd;
        }

        /// <summary>
        /// Returns the overlap with the given virtual span, or null if there is no overlap.
        /// </summary>
        /// <param name="virtualSpan">
        /// The virtual span to check.
        /// </param>
        /// <returns>
        /// The overlap of the spans, or null if the overlap is empty.
        /// </returns>
        public VirtualSnapshotSpan? Overlap(VirtualSnapshotSpan virtualSpan)
        {
            VirtualSnapshotPoint overlapStart = (_start > virtualSpan._start) ? _start : virtualSpan._start;
            VirtualSnapshotPoint overlapEnd = (_end < virtualSpan._end) ? _end : virtualSpan._end;

            if (overlapStart < overlapEnd)
            {
                return new VirtualSnapshotSpan(overlapStart, overlapEnd);
            }

            return null;
        }

        /// <summary>
        /// Determines whether <paramref name="virtualSpan"/> intersects this span. Two spans are considered to 
        /// intersect if they have positions in common or the end of one span 
        /// coincides with the start of the other span.
        /// </summary>
        /// <param name="virtualSpan">
        /// The virtual span to check.
        /// </param>
        /// <returns>
        /// <c>true</c> if the spans intersect, otherwise <c>false</c>.
        /// </returns>
        public bool IntersectsWith(VirtualSnapshotSpan virtualSpan)
        {
            return (virtualSpan._start <= _end && virtualSpan._end >= _start);
        }

        /// <summary>
        /// Returns the intersection with the given virtual span, or null if there is no intersection.
        /// </summary>
        /// <param name="virtualSpan">
        /// The virtual span to check.
        /// </param>
        /// <returns>
        /// The intersection of the spans, or null if the intersection is empty.
        /// </returns>
        public VirtualSnapshotSpan? Intersection(VirtualSnapshotSpan virtualSpan)
        {
            VirtualSnapshotPoint intersectStart = (_start > virtualSpan._start) ? _start : virtualSpan._start;
            VirtualSnapshotPoint intersectEnd = (_end < virtualSpan._end) ? _end : virtualSpan._end;

            if (intersectStart <= intersectEnd)
            {
                return new VirtualSnapshotSpan(intersectStart, intersectEnd);
            }

            return null;
        }


        /// <summary>
        /// Gets the hash code for the object.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return _start.GetHashCode() ^ _end.GetHashCode();
        }

        /// <summary>
        /// Translates this span to the <paramref name="snapshot"/>.
        /// </summary>
        /// <param name="snapshot">The target snapshot.</param>
        /// <returns>The corresponding <see cref="VirtualSnapshotSpan"/> in <paramref name="snapshot"/>.</returns>
        /// <exception cref="ArgumentException"><paramref name="snapshot"/> is for an earlier snapshot.</exception>
        public VirtualSnapshotSpan TranslateTo(ITextSnapshot snapshot)
        {
            return TranslateTo(snapshot, SpanTrackingMode.EdgePositive);
        }

        /// <summary>
        /// Translates this span to the <paramref name="snapshot"/> with the given tracking mode.
        /// </summary>
        /// <param name="snapshot">The target snapshot.</param>
        /// <param name="trackingMode">The span tracking mode.</param>
        /// <returns>The corresponding <see cref="VirtualSnapshotSpan"/> in <paramref name="snapshot"/>.</returns>
        /// <exception cref="ArgumentException"><paramref name="snapshot"/> is for an earlier snapshot.</exception>
        /// <remarks>
        /// <para>
        /// See <see cref="VirtualSnapshotPoint.TranslateTo(ITextSnapshot, PointTrackingMode)" /> for a description of
        /// how <see cref="VirtualSnapshotPoint" /> translation behaves.
        /// </para>
        /// </remarks>
        public VirtualSnapshotSpan TranslateTo(ITextSnapshot snapshot, SpanTrackingMode trackingMode)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            if (snapshot.Version.VersionNumber < _start.Position.Snapshot.Version.VersionNumber)
            {
                throw new ArgumentException("VirtualSnapshotSpans can only be translated to later snapshots", nameof(snapshot));
            }
            else if (snapshot == _start.Position.Snapshot)
            {
                return this;
            }
            else
            {
                // Translate endpoints with the appropriate PointTrackingMode
                var newStart = _start.TranslateTo(snapshot, GetStartPointMode(trackingMode));
                var newEnd = _end.TranslateTo(snapshot, GetEndPointMode(trackingMode));

                // If the end point has tracked to before the start point, just create
                // an empty span at the start point.
                if (newStart <= newEnd)
                    return new VirtualSnapshotSpan(newStart, newEnd);
                else
                    return new VirtualSnapshotSpan(newStart, newStart);
            }
        }

        /// <summary>
        /// Get the equivalent PointTrackingMode for our start point for
        /// the given SpanTrackingMode.
        /// </summary>
        static PointTrackingMode GetStartPointMode(SpanTrackingMode trackingMode)
        {
            if (trackingMode == SpanTrackingMode.EdgeInclusive ||
                trackingMode == SpanTrackingMode.EdgeNegative)
                return PointTrackingMode.Negative;
            else
                return PointTrackingMode.Positive;
        }

        /// <summary>
        /// Get the equivalent PointTrackingMode for our end point for
        /// the given SpanTrackingMode.
        /// </summary>
        static PointTrackingMode GetEndPointMode(SpanTrackingMode trackingMode)
        {
            if (trackingMode == SpanTrackingMode.EdgeInclusive ||
                trackingMode == SpanTrackingMode.EdgePositive)
                return PointTrackingMode.Positive;
            else
                return PointTrackingMode.Negative;
        }

        /// <summary>
        /// Converts the object to a string.
        /// </summary>
        /// <returns>The string form of this object.</returns>
        public override string ToString()
        {
            return string.Format(System.Globalization.CultureInfo.CurrentCulture, "({0},{1})", _start, _end);
        }

        /// <summary>
        /// Determines whether two <see cref="VirtualSnapshotSpan"/> objects are the same.
        /// </summary>
        /// <param name="obj">The object to compare.</param>
        /// <returns><c>true</c> if the objects are the same, otherwise <c>false</c>.</returns>
        public override bool Equals(object obj)
        {
            if (obj is VirtualSnapshotSpan)
            {
                VirtualSnapshotSpan other = (VirtualSnapshotSpan)obj;
                return other == this;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Determines whether two <see cref="VirtualSnapshotSpan"/> objects are the same.
        /// </summary>
        /// <param name="left">The first object.</param>
        /// <param name="right">The second object.</param>
        /// <returns><c>true</c> if the two objects are the same, otherwise <c>false</c>.</returns>
        public static bool operator ==(VirtualSnapshotSpan left, VirtualSnapshotSpan right)
        {
            return left._start == right._start && left._end == right._end;
        }

        /// <summary>
        /// Determines whether two <see cref="VirtualSnapshotSpan"/> objects are different.
        /// </summary>
        /// <param name="left">The first object.</param>
        /// <param name="right">The second object.</param>
        /// <returns><c>true</c> if the two objects are different, otherwise <c>false</c>.</returns>
        public static bool operator !=(VirtualSnapshotSpan left, VirtualSnapshotSpan right)
        {
            return !(left == right);
        }
    }
}
