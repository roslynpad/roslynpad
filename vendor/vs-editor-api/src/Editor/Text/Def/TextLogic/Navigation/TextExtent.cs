//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Operations
{
#pragma warning disable CA1066 // Type {0} should implement IEquatable<T> because it overrides Equals
    /// <summary>
    /// Represents the extent of a word.
    /// </summary>
    public struct TextExtent
#pragma warning restore CA1066 // Type {0} should implement IEquatable<T> because it overrides Equals
    {
        #region Private Members

        SnapshotSpan _span;
        bool _isSignificant;

        #endregion // Private Members

        /// <summary>
        /// Initializes a new instance of <see cref="TextExtent"/>.
        /// </summary>
        /// <param name="span">
        /// The <see cref="SnapshotSpan"/> that includes the extent.
        /// </param>
        /// <param name="isSignificant">
        /// <c>false</c> if the extent contains whitespace, unless whitespace should be treated like any other character.
        /// </param>
        public TextExtent(SnapshotSpan span, bool isSignificant)
        {
            _span = span;
            _isSignificant = isSignificant;
        }

        /// <summary>
        /// Initializes a new instance of a <see cref="TextExtent"/> from the specified <see cref="TextExtent"/>.
        /// </summary>
        /// <param name="textExtent">The <see cref="TextExtent"/> from which to copy.
        /// </param>
        public TextExtent(TextExtent textExtent)
        {
            _span = textExtent.Span;
            _isSignificant = textExtent.IsSignificant;
        }

        #region Public Properties

        /// <summary>
        /// Gets the <see cref="SnapshotSpan"/>.
        /// </summary>
        public SnapshotSpan Span
        {
            get { return _span; }
        }

        /// <summary>
        /// Determines whether the extent is significant.  <c>false</c> for whitespace or other 
        /// insignificant characters that should be ignored during navigation.
        /// </summary>
        public bool IsSignificant
        {
            get { return _isSignificant; }
        }

        #endregion // Public Properties

        #region Overrides and operators
        /// <summary>
        /// Determines whether two <see cref="TextExtent"/> objects are the same.
        /// </summary>
        /// <param name="obj">The <see cref="TextExtent"/> to compare.</param>
        /// <returns><c>true</c> if the two objects are the same, otherwise <c>false</c>.</returns>
        public override bool Equals(object obj)
        {
            if (obj != null && obj is TextExtent)
            {
                return this == (TextExtent)obj;
            }
            else
            {
                return false;
            }
        }
        /// <summary>
        /// Gets the hash code of the object.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        /// <summary>
        /// Determines whether two <see cref="TextExtent"/> objects are the same.
        /// </summary>
        /// <param name="extent1">The first object.</param>
        /// <param name="extent2">The second object.</param>
        /// <returns><c>true</c> if the objects are the same, otherwise false.</returns>
        public static bool operator ==(TextExtent extent1, TextExtent extent2)
        {
            return extent1._span == extent2._span && extent1._isSignificant == extent2._isSignificant;
        }

        /// <summary>
        /// Determines whether two <see cref="TextExtent"/> objects are different.
        /// </summary>
        /// <param name="extent1">The first object.</param>
        /// <param name="extent2">The second object.</param>
        /// <returns><c>true</c> if the two objects are different, otherwise <c>false</c>.</returns>
        public static bool operator !=(TextExtent extent1, TextExtent extent2)
        {
            return !(extent1 == extent2);
        }
        #endregion
    }
}
