//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text
{
    using System;

#pragma warning disable CA1066 // Type {0} should implement IEquatable<T> because it overrides Equals
#pragma warning disable CA1036 // Override methods on comparable types
    /// <summary>
    /// An immutable text position in a particular text snapshot.
    /// </summary>
    public struct SnapshotPoint : IComparable<SnapshotPoint>
#pragma warning restore CA1036 // Override methods on comparable types
#pragma warning restore CA1066 // Type {0} should implement IEquatable<T> because it overrides Equals
    {

        /// <summary>
        /// Initializes a new instance of a <see cref="SnapshotPoint"/> with respect to a particular snapshot and position.
        /// </summary>
        /// <param name="snapshot">The <see cref="ITextSnapshot"> that contains the new point.</see></param>
        /// <param name="position">The position of the point.</param>
        public SnapshotPoint(ITextSnapshot snapshot, int position)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }
            if (position < 0 || position > snapshot.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(position));
            }
            this.Snapshot = snapshot;
            this.Position = position;
        }

        /// <summary>
        /// Gets the position of the point.
        /// </summary>
        /// <value>A non-negative integer less than or equal to the length of the snapshot.</value>
        public int Position { get; }

        /// <summary>
        /// Gets the <see cref="ITextSnapshot"/> to which this snapshot point refers.
        /// </summary>
        public ITextSnapshot Snapshot { get; }

        /// <summary>
        /// Implicitly converts the snapshot point to an integer equal to the position of the snapshot point in the snapshot.
        /// </summary>
        public static implicit operator int(SnapshotPoint snapshotPoint)
        {
            return snapshotPoint.Position;
        }

        /// <summary>
        /// The <see cref="ITextSnapshotLine"/> containing this snapshot point.
        /// </summary>
        /// <returns></returns>
        public ITextSnapshotLine GetContainingLine()
        {
            return this.Snapshot.GetLineFromPosition(this.Position);
        }

        /// <summary>
        /// The number of the line containing this snapshot point.
        /// </summary>
        /// <remarks>Equivalent to <c>GetContainingLine().LineNumber</c> without instantiating the line.</remarks>
        public int GetContainingLineNumber()
        {
            return this.Snapshot.GetLineNumberFromPosition(this.Position);
        }

        /// <summary>
        /// Gets the character at the position of this snapshot point.
        /// </summary>
        /// <returns>The character at the position of this snapshot point.</returns>
        /// <exception cref="ArgumentOutOfRangeException"> if the position of this point is equal to the length of the snapshot.</exception>
        public char GetChar()
        {
            return this.Snapshot[this.Position];
        }

        /// <summary>
        /// Translates this snapshot Point to a different snapshot of the same <see cref="ITextBuffer"/>.
        /// </summary>
        /// <param name="targetSnapshot">The snapshot to which to translate.</param>
        /// <param name="trackingMode">The <see cref="PointTrackingMode"/> to use in the translation.</param>
        /// <returns>A new snapshot point that has been mapped to the requested snapshot.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="targetSnapshot"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="targetSnapshot"/> does not refer to the same <see cref="ITextBuffer"/> as this snapshot point.</exception>
        public SnapshotPoint TranslateTo(ITextSnapshot targetSnapshot, PointTrackingMode trackingMode)
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
                if (targetSnapshot.TextBuffer != this.Snapshot.TextBuffer)
                {
                    throw new ArgumentException(Strings.InvalidSnapshot);
                }

                int targetPosition = targetSnapshot.Version.VersionNumber > this.Snapshot.Version.VersionNumber 
                                        ? Tracking.TrackPositionForwardInTime(trackingMode, this.Position, this.Snapshot.Version, targetSnapshot.Version)
                                        : Tracking.TrackPositionBackwardInTime(trackingMode, this.Position, this.Snapshot.Version, targetSnapshot.Version);

                return new SnapshotPoint(targetSnapshot, targetPosition);
            }
        }

        /// <summary>
        /// Serves as a hash function for this type.
        /// </summary>
        public override int GetHashCode()
        {
            return (this.Snapshot != null) ? (this.Position.GetHashCode() ^ this.Snapshot.GetHashCode()) : 0;
        }

        /// <summary>
        /// Converts this snapshot point to a string, or to the string "uninit" if the <see cref="ITextSnapshot"/> is null.
        /// </summary>
        public override string ToString()
        {
            if (this.Snapshot == null)
            {
                return "uninit";
            }
            else
            {
                this.Snapshot.TextBuffer.Properties.TryGetProperty("tag", out string tag);
                return string.Format(System.Globalization.CultureInfo.CurrentCulture, "{0}_v{1}_{2}_'{3}'",
                                     tag ?? "?",
                                     this.Snapshot.Version.VersionNumber,
                                     this.Position,
                                     this.Position == this.Snapshot.Length ? "<end>" : this.Snapshot.GetText(this.Position, 1));
            }
        }

        /// <summary>
        /// Determines whether this snapshot point is the same as a second snapshot point.
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj is SnapshotPoint)
            {
                SnapshotPoint other = (SnapshotPoint)obj;
                return other == this;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Creates a new snapshot point at the specified offset from this point.
        /// </summary>
        /// <param name="offset">The offset of the new point.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The new point is less than zero or greater than Snapshot.Length.
        /// </exception>
        public SnapshotPoint Add(int offset)
        {
            return new SnapshotPoint(this.Snapshot, this.Position + offset);
        }

        /// <summary>
        /// Creates a new snapshot point at the specified negative offset from this point.
        /// </summary>
        /// <param name="offset">The offset of the new point.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The new point is less than zero or greater than Snapshot.Length.
        /// </exception>
        public SnapshotPoint Subtract(int offset)
        {
            return Add(-offset);
        }

        /// <summary>
        /// Computes the offset between this snapshot point and another snapshot point.
        /// </summary>
        /// <param name="other">The point from which to compute the offset.</param>
        /// <exception cref="ArgumentException">The two points do not belong to the same
        /// snapshot.</exception>
        /// <returns>The offset between the two points, equivalent to other.Position -
        /// this.Position.</returns>
        public int Difference(SnapshotPoint other)
        {
            return other - this;
        }

        #region Operator overloads

        /// <summary>
        /// Decrements the position of a snapshot point.
        /// </summary>
        /// <param name="point">The point from which to calculate the new position.</param>
        /// <param name="offset">The offset of the new point.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The new point is less than zero
        /// or greater than Snapshot.Length.
        /// </exception>
        public static SnapshotPoint operator -(SnapshotPoint point, int offset)
        {
            return point.Add(-offset);
        }

        /// <summary>
        /// Computes the offset between two <see cref="SnapshotPoint"/> objects.
        /// </summary>
        /// <param name="start">The starting point.</param>
        /// <param name="other">The point from which to compute the offset.</param>
        /// <exception cref="ArgumentException">The two points do not belong to the same
        /// snapshot.</exception>
        /// <returns>The offset between the two points, equivalent to start.Position -
        /// other.Position.</returns>
        /// <remarks>The following should always be true:
        /// start == other + (start - other).</remarks>
        public static int operator -(SnapshotPoint start, SnapshotPoint other)
        {
            if (start.Snapshot != other.Snapshot)
            {
                throw new ArgumentException(Strings.InvalidSnapshotPoint);
            }

            return start.Position - other.Position;
        }

        /// <summary>
        /// Determines whether this snapshot point is the same as a second snapshot point.
        /// </summary>
        public static bool operator ==(SnapshotPoint left, SnapshotPoint right)
        {
            return left.Snapshot == right.Snapshot && left.Position == right.Position;
        }

        /// <summary>
        /// Determines whether this snapshot point is different from a second snapshot point.
        /// </summary>
        public static bool operator !=(SnapshotPoint left, SnapshotPoint right)
        {
            return !(left == right);
        }

        /// <summary>
        /// Increments the position of a snapshot point.
        /// </summary>
        /// <param name="point">The point from which to calculate the new position.</param>
        /// <param name="offset">The offset of the new point.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The new point is less than zero
        /// or greater than Snapshot.Length.
        /// </exception>
        public static SnapshotPoint operator +(SnapshotPoint point, int offset)
        {
            return point.Add(offset);
        }

        /// <summary>
        /// Determines whether the position of one snapshot point is greater than the position of a second snapshot point.
        /// </summary>
        /// <returns><c>true</c> if the first position is greater than the second position, otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentException">The two points do not belong to the same snapshot.</exception>
        public static bool operator >(SnapshotPoint left, SnapshotPoint right)
        {
            return left.CompareTo(right) > 0;
        }

        /// <summary>
        /// Determine if the position of the left point is less than the position of the right point.
        /// </summary>
        /// <returns><c>true</c> if left.Position is greater than right.Position, otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentException">The two points do not belong to the same snapshot.</exception>
        public static bool operator <(SnapshotPoint left, SnapshotPoint right)
        {
            return left.CompareTo(right) < 0;
        }

        #endregion

        #region IComparable<SnapshotPoint>
        /// <summary>
        /// Determines whether this snapshot is the same as a second snapshot point.
        /// </summary>
        /// <param name="other">The snapshot point to which to compare.</param>
        /// <returns>A negative integer if the position of this snapshot point occurs before the second snapshot point, 
        /// a positive integer if the position of this snapshot point occurs before the second snapshot point, and 
        /// zero if the positions are the same.</returns>
        public int CompareTo(SnapshotPoint other)
        {
            if (this.Snapshot != other.Snapshot)
            {
                throw new ArgumentException(Strings.InvalidSnapshotPoint);
            }

            return this.Position.CompareTo(other.Position);
        }

        #endregion
    }
}
