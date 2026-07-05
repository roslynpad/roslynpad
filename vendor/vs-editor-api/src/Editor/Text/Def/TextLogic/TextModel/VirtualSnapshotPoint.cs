//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text
{
    using System;

#pragma warning disable CA1066 // Type {0} should implement IEquatable<T> because it overrides Equals
    /// <summary>
    /// Represents a <see cref="SnapshotPoint"/> that may have virtual spaces.
    /// </summary>
    public struct VirtualSnapshotPoint : IComparable<VirtualSnapshotPoint>
#pragma warning restore CA1066 // Type {0} should implement IEquatable<T> because it overrides Equals
    {
        private readonly SnapshotPoint _position;
        private readonly int _virtualSpaces;

        /// <summary>
        /// Initializes a new instance of a <see cref="VirtualSnapshotPoint"/> at <paramref name="position"/>, with zero virtual spaces.
        /// </summary>
        /// <param name="position">The position the point in the snapshot.</param>
        public VirtualSnapshotPoint(SnapshotPoint position)
        {
            _position = position;
            _virtualSpaces = 0;
        }

        /// <summary>
        /// Initializes a new instance of a <see cref="VirtualSnapshotPoint"/> at <paramref name="position"/> in a <paramref name="snapshot"/>, with zero virtual spaces.
        /// </summary>
        /// <param name="snapshot">The snapshot to use.</param>
        /// <param name="position">The position of the snapshot point.</param>
        public VirtualSnapshotPoint(ITextSnapshot snapshot, int position)
        {
            _position = new SnapshotPoint(snapshot, position);
            _virtualSpaces = 0;
        }

        /// <summary>
        /// Initializes a new instance of a <see cref="VirtualSnapshotPoint"/> at <paramref name="position"/>, with the specified number of virtual spaces.
        /// </summary>
        /// <param name="position">The position of the virtual snapshot point.</param>
        /// <param name="virtualSpaces">The number of virtual spaces after <paramref name="position"/>.</param>
        /// <remarks><paramref name="virtualSpaces"/> must be zero unless 
        /// <paramref name="position"/> corresponds to a location at the end
        /// of a <see cref="ITextSnapshotLine"/>.</remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="virtualSpaces"/> is negative.</exception>
        /// <remarks>If <paramref name="position"/> specifies a location that is not at the end of a line, then <paramref name="virtualSpaces"/> is set to 0.</remarks>
        public VirtualSnapshotPoint(SnapshotPoint position, int virtualSpaces)
        {
            if (virtualSpaces < 0)
                throw new ArgumentOutOfRangeException(nameof(virtualSpaces));

            //Treat trying to set virtual spaces in the middle of a line as a soft error. It is easy to do if some 3rd party does an unexpected edit on a text change
            //and setting virtualSpaces to 0 is a reasonable fallback behavior.
            if ((virtualSpaces != 0) && (position.GetContainingLine().End != position))
                virtualSpaces = 0;  

            _position = position;
            _virtualSpaces = virtualSpaces;
        }

        /// <summary>
        /// Initializes a new instance of a <see cref="VirtualSnapshotPoint"/> 
        /// at <paramref name="offset"/> of <paramref name="line"/>, placing the point in virtual space if necessary.
        /// </summary>
        /// <param name="line">The line on which to place the point.</param>
        /// <param name="offset">The offset (zero-based) of the point.</param>
        /// <exception cref="ArgumentNullException"><paramref name="line"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> is negative.</exception>
        /// <remarks>
        /// <paramref name="offset"/> is a character offset from the start of the line. It does not correspond to a column position (for example, if the line consists of a single tab and the offset is 2, then
        /// the resulting VirtualSnapshotPoint will be one "space" past the end of the line).</remarks>
        public VirtualSnapshotPoint(ITextSnapshotLine line, int offset)
        {
            if (line == null)
                throw new ArgumentNullException(nameof(line));
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset));

            if (offset <= line.Length)
            {
                _position = line.Start + offset;
                _virtualSpaces = 0;
            }
            else
            {
                _position = line.End;
                _virtualSpaces = (offset - line.Length);
            }
        }

        /// <summary>
        /// Gets the position of the snapshot point.
        /// </summary>
        public SnapshotPoint Position
        {
            get { return _position; }
        }

        /// <summary>
        /// Gets the number of virtual spaces.
        /// </summary>
        public int VirtualSpaces
        {
            get { return _virtualSpaces; }
        }

        /// <summary>
        /// Determines whether the snapshot point has virtual spaces.
        /// </summary>
        public bool IsInVirtualSpace
        {
            get { return _virtualSpaces > 0; }
        }

        /// <summary>
        /// Gets the hash code for the object.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return _position.GetHashCode() ^ _virtualSpaces.GetHashCode();
        }

        /// <summary>
        /// Translates this point to the <paramref name="snapshot"/>.
        /// </summary>
        /// <param name="snapshot">The target snapshot.</param>
        /// <returns>The corresponding <see cref="VirtualSnapshotPoint"/> in <paramref name="snapshot"/>.</returns>
        /// <exception cref="ArgumentException"><paramref name="snapshot"/> is for an earlier snapshot.</exception>
        public VirtualSnapshotPoint TranslateTo(ITextSnapshot snapshot)
        {
            return TranslateTo(snapshot, PointTrackingMode.Positive);
        }

        /// <summary>
        /// Translates this point to the <paramref name="snapshot"/> with the given tracking mode.
        /// </summary>
        /// <param name="snapshot">The target snapshot.</param>
        /// <param name="trackingMode">The tracking mode to use.</param>
        /// <returns>The corresponding <see cref="VirtualSnapshotPoint"/> in <paramref name="snapshot"/>.</returns>
        /// <exception cref="ArgumentException"><paramref name="snapshot"/> is for an earlier snapshot.</exception>
        /// <remarks>
        /// <para>
        /// The tracking mode is relative to the virtual point, not the snapshot point.  If
        /// the point is in virtual space, it will behave as if the underlying (non-virtual)
        /// point is always tracking positive, as any text inserted at the point (at the
        /// end of the line it is on) will still be inserted "before" the virtual point.
        /// </para>
        /// </remarks>
        public VirtualSnapshotPoint TranslateTo(ITextSnapshot snapshot, PointTrackingMode trackingMode)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            if (snapshot.Version.VersionNumber < _position.Snapshot.Version.VersionNumber)
            {
                throw new ArgumentException("VirtualSnapshotPoints can only be translated to later snapshots", nameof(snapshot));
            }
            else if (snapshot == _position.Snapshot)
            {
                return this;
            }
            else
            {
                if (this.IsInVirtualSpace)
                {
                    SnapshotPoint newPosition = _position.TranslateTo(snapshot, PointTrackingMode.Positive);

                    //The new virtual snapshot point is only placed in virtual space if the old one was,
                    //it is still at the physical end of a line, and the character after _position wasn't deleted.
                    int newVirtualSpaces = (_virtualSpaces != 0) &&
                                           (newPosition.GetContainingLine().End == newPosition) &&
                                           !CharacterDeleted(_position, snapshot)
                                           ? _virtualSpaces : 0;

                    return new VirtualSnapshotPoint(newPosition, newVirtualSpaces);
                }
                else
                {
                    return new VirtualSnapshotPoint(_position.TranslateTo(snapshot, trackingMode));
                }
            }
        }

        /// <summary>
        /// Converts the object to a string.
        /// </summary>
        /// <returns>The string form of this object.</returns>
        public override string ToString()
        {
            return string.Format(System.Globalization.CultureInfo.CurrentCulture, "{0}+{1}", _position, _virtualSpaces);
        }

        /// <summary>
        /// Determines whether two <see cref="VirtualSnapshotPoint"/> objects are the same.
        /// </summary>
        /// <param name="obj">The object to compare.</param>
        /// <returns><c>true</c> if the objects are the same, otherwise <c>false</c>.</returns>
        public override bool Equals(object obj)
        {
            if (obj is VirtualSnapshotPoint)
            {
                VirtualSnapshotPoint other = (VirtualSnapshotPoint)obj;
                return other == this;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Determines whether two <see cref="VirtualSnapshotPoint"/> objects are the same.
        /// </summary>
        /// <param name="left">The first object.</param>
        /// <param name="right">The second object.</param>
        /// <returns><c>true</c> if the two objects are the same, otherwise <c>false</c>.</returns>
        public static bool operator ==(VirtualSnapshotPoint left, VirtualSnapshotPoint right)
        {
            return left._position == right._position && left._virtualSpaces == right._virtualSpaces;
        }

        /// <summary>
        /// Determines whether two <see cref="VirtualSnapshotPoint"/> objects are different.
        /// </summary>
        /// <param name="left">The first object.</param>
        /// <param name="right">The second object.</param>
        /// <returns><c>true</c> if the two objects are different, otherwise <c>false</c>.</returns>
        public static bool operator !=(VirtualSnapshotPoint left, VirtualSnapshotPoint right)
        {
            return !(left == right);
        }

        /// <summary>
        /// Determines whether the position of the left point is greater than the position of the right point.
        /// </summary>
        /// <returns><c>true</c> if left.Position is greater than right.Position, otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentException">The snapshots of the two points do not match.</exception>
        public static bool operator >(VirtualSnapshotPoint left, VirtualSnapshotPoint right)
        {
            return left.CompareTo(right) > 0;
        }

        /// <summary>
        /// Determines whether the position of the left point is greater than or equal to the position of the right point.
        /// </summary>
        /// <returns><c>true</c> if left.Position is greater than or equal to right.Position, otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentException">The snapshots of the two points do not match.</exception>
        public static bool operator >=(VirtualSnapshotPoint left, VirtualSnapshotPoint right)
        {
            return left.CompareTo(right) >= 0;
        }

        /// <summary>
        /// Determines whether the position of the left point is less than the position of the right point.
        /// </summary>
        /// <returns><c>true</c> if left.Position is less than right.Position, otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentException">If the snapshots of the points do not match.</exception>
        public static bool operator <(VirtualSnapshotPoint left, VirtualSnapshotPoint right)
        {
            return left.CompareTo(right) < 0;
        }

        /// <summary>
        /// Determines whether the position of the left point is less than or equal to the position of the right point.
        /// </summary>
        /// <returns><c>true</c> if left.Position is less than or equal to right.Position, otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentException">If the snapshots of the points do not match.</exception>
        public static bool operator <=(VirtualSnapshotPoint left, VirtualSnapshotPoint right)
        {
            return left.CompareTo(right) <= 0;
        }

        #region IComparable<VirtualSnapshotPoint>
        /// <summary>
        /// Compares one <see cref="VirtualSnapshotPoint"/> to another.
        /// </summary>
        /// <param name="other">The second <see cref="VirtualSnapshotPoint"/>.</param>
        /// <returns>Compares the position and number of virtual spaces of the two points.</returns>
        public int CompareTo(VirtualSnapshotPoint other)
        {
            int cmp = _position.CompareTo(other._position);
            return (cmp == 0) ? (_virtualSpaces - other._virtualSpaces) : cmp;
        }
        #endregion

        //Check to see if, when translating position to snapshot, the character after position was deleted.
        private static bool CharacterDeleted(SnapshotPoint position, ITextSnapshot snapshot)
        {
            int currentPosition = position.Position;

            ITextVersion version = position.Snapshot.Version;
            while (version.VersionNumber != snapshot.Version.VersionNumber)
            {
                foreach (ITextChange change in version.Changes)
                {
                    if (change.NewPosition > currentPosition)
                        break;
                    else
                    {
                        if (change.NewPosition + change.OldLength > currentPosition)
                        {
                            //Line break (or the first character of it) was deleted.
                            return true;
                        }
                        else
                        {
                            //Change occurs entirely before currentPosition so adjust.
                            currentPosition += change.Delta;
                        }
                    }
                }

                version = version.Next;
            }

            return false;
        }
    }
}
