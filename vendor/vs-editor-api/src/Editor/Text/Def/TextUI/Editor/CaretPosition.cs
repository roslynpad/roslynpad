//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Editor
{
    using System;

#pragma warning disable CA1066 // Type {0} should implement IEquatable<T> because it overrides Equals
    /// <summary>
    /// Represents the position of a caret in an <see cref="ITextView"/>.
    /// </summary>
    public struct CaretPosition
#pragma warning restore CA1066 // Type {0} should implement IEquatable<T> because it overrides Equals
    {
        #region Private Members
        VirtualSnapshotPoint _bufferPosition;
        PositionAffinity _affinity;
        IMappingPoint _mappingPoint;
        #endregion

        /// <summary>
        /// Initializes a new instance of a <see cref="CaretPosition"/>.
        /// </summary>
        /// <param name="bufferPosition">The index of the caret. This corresponds to a gap between two characters in the underlying <see cref="ITextBuffer"/>.</param>
        /// <param name="mappingPoint">A mapping point for the caret that can be used to find its position in any buffer.</param>
        /// <param name="caretAffinity">The <see cref="PositionAffinity"/> of the caret. The caret can have an affinity with 
        /// the preceding edge of the gap or the following edge of the gap.</param>
        public CaretPosition(VirtualSnapshotPoint bufferPosition, IMappingPoint mappingPoint, PositionAffinity caretAffinity)
        {
            if (mappingPoint == null)
            {
                throw new ArgumentNullException(nameof(mappingPoint));
            }

            _bufferPosition = bufferPosition;
            _mappingPoint = mappingPoint;
            _affinity = caretAffinity;
        }
        #region CaretPosition Members

        /// <summary>
        /// Gets the position of the caret, corresponding to a gap between two characters in the <see cref="ITextBuffer"/> of the view.
        /// </summary>
        /// <remarks>
        /// This property gets the buffer position at the end of a line if the caret is positioned in virtual space.
        /// </remarks>
        public SnapshotPoint BufferPosition
        {
            get
            {
                return _bufferPosition.Position;
            }
        }

        /// <summary>
        /// Gets the <see cref="IMappingPoint"/>. This marks the position of the caret in the buffer.
        /// </summary>
        public IMappingPoint Point
        {
            get
            {
                return _mappingPoint;
            }
        }

        /// <summary>
        /// Gets the affinity of the caret. 
        /// <see cref="PositionAffinity.Predecessor"/> indicates that the caret is bound to the preceding edge of the gap. 
        /// <see cref="PositionAffinity.Successor"/> indicates that the caret is bound to the following edge of the gap.
        /// </summary>
        public PositionAffinity Affinity
        {
            get
            {
                return _affinity;
            }
        }

        /// <summary>
        /// Gets the virtual buffer position as a <see cref="VirtualSnapshotPoint"/>.
        /// </summary>
        public VirtualSnapshotPoint VirtualBufferPosition
        {
            get { return _bufferPosition; }
        }

        /// <summary>
        /// Gets the number of spaces past the physical end of the line of the caret position.
        /// </summary>
        public int VirtualSpaces
        {
            get
            {
                return _bufferPosition.VirtualSpaces;
            }
        }
        #endregion

        #region Overrides

        /// <summary>
        /// Provides a string representation of the caret position.
        /// </summary>
        /// <returns>The string representation of the caret position.</returns>
        public override string ToString()
        {
            if (_affinity == PositionAffinity.Predecessor)
                return string.Format(System.Globalization.CultureInfo.InvariantCulture, "|{0}", _bufferPosition);
            else
                return string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}|", _bufferPosition);
        }

        /// <summary>
        /// Gets the hash code for the <see cref="CaretPosition"/>.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
            return _bufferPosition.GetHashCode() ^ _affinity.GetHashCode();
        }

        /// <summary>
        /// Determines whether two <see cref="CaretPosition"/> objects are the same
        /// </summary>
        /// <returns><c>true</c> if the two objects are the same, otherwise <c>false</c>.</returns>
        public override bool Equals(object obj)
        {
            if (obj is CaretPosition)
            {
                CaretPosition caretPosition = (CaretPosition)obj;
                return caretPosition == this;
            }
            else
            {
                return false;
            }
        }

        #endregion // Overrides

        /// <summary>
        /// Determines whether two <see cref="CaretPosition"/> objects are the same.
        /// </summary>
        /// <returns><c>true</c> if the two objects are the same, otherwise <c>false.</c></returns>
        public static bool operator ==(CaretPosition caretPosition1, CaretPosition caretPosition2)
        {
            return (caretPosition1._bufferPosition == caretPosition2._bufferPosition) &&
                   (caretPosition1.Affinity == caretPosition2.Affinity);
        }

        /// <summary>
        /// Determines whether two <see cref="CaretPosition"/> objects are different.
        /// </summary>
        /// <returns><c>true</c> if the two objects are different, otherwise <c>false.</c></returns>
        public static bool operator !=(CaretPosition caretPosition1, CaretPosition caretPosition2)
        {
            return !(caretPosition1 == caretPosition2);
        }
    }
}
