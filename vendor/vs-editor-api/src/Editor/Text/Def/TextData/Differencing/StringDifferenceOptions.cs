//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
using System;
using System.Globalization;

namespace Microsoft.VisualStudio.Text.Differencing
{
    // Ignore the warnings about deprecated properties
#pragma warning disable 0618

#pragma warning disable CA1066 // Type {0} should implement IEquatable<T> because it overrides Equals
    /// <summary>
    /// Options to use in computing string differences.
    /// </summary>
    public struct StringDifferenceOptions
#pragma warning restore CA1066 // Type {0} should implement IEquatable<T> because it overrides Equals
    {

        /// <summary>
        /// The type of string differencing to do, as a combination
        /// of line, word, and character differencing.
        /// </summary>
        public StringDifferenceTypes DifferenceType { get; set; }

        /// <summary>
        /// The greatest distance a differencing element (line, span, or character) can move 
        /// and still be considered part of the same source.  A value of 0 disables locality checking.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The use of locality in the default differencing implementation is now deprecated, and
        /// the value of this field is ignored (the same as being <c>0</c>).
        /// </para>
        /// <para>
        /// For example, if Locality is set to 100, a line is considered the same line 
        /// if it is separated by 100 or fewer lines from its neighboring lines. 
        /// If it is separated by more than 100 lines, it is considered a different line.
        /// </para>
        /// </remarks>
        [Obsolete("This value is no longer used and will be ignored.")]
        public int Locality { get; set; }

        /// <summary>
        /// Gets or sets whether to ignore white space.
        /// </summary>
        public bool IgnoreTrimWhiteSpace { get; set; }

        /// <summary>
        /// The behavior to use when splitting words, if word differencing is requested
        /// by the <see cref="DifferenceType" />.
        /// </summary>
        public WordSplitBehavior WordSplitBehavior { get; set; }

        /// <summary>
        /// An optional callback to override the locality for a specific round of differencing.
        /// </summary>
        /// <remarks>
        /// This callback is no longer used by the default <see cref="ITextDifferencingService"/>, and is not
        /// required to be used by an extensions that provide an implementation of a text differencing service.
        /// </remarks>
        [Obsolete("This callback is no longer used and will be ignored.")]
        public DetermineLocalityCallback DetermineLocalityCallback { get; set; }

        /// <summary>
        /// An optional predicate that allows clients to cancel differencing before it has completely finished.
        /// </summary>
        public ContinueProcessingPredicate<string> ContinueProcessingPredicate { get; set; }

        /// <summary>
        /// Constructs a <see cref="StringDifferenceOptions"/>.
        /// </summary>
        /// <param name="differenceType">The type of string differencing to do, as a combination of line, word, and character differencing.</param>
        /// <param name="locality">The greatest distance a differencing element (line, span, or character) can move and still be considered part of the same source.  A value of 0 disables locality checking.</param>
        /// <param name="ignoreTrimWhiteSpace">Determines whether whitespace should be ignored.</param>
        public StringDifferenceOptions(StringDifferenceTypes differenceType, int locality, bool ignoreTrimWhiteSpace) : this()
        {
            this.DifferenceType = differenceType;
            this.Locality = locality;
            this.IgnoreTrimWhiteSpace = ignoreTrimWhiteSpace;
        }

        /// <summary>
        /// Constructs a <see cref="StringDifferenceOptions"/> from a given <see cref="StringDifferenceOptions"/>.
        /// </summary>
        /// <param name="other">The <see cref="StringDifferenceOptions"/> to use in constructing a new <see cref="StringDifferenceOptions"/>.</param>
        public StringDifferenceOptions(StringDifferenceOptions other) : this()
        {
            this.DifferenceType = other.DifferenceType;
            this.Locality = other.Locality;
            this.IgnoreTrimWhiteSpace = other.IgnoreTrimWhiteSpace;
            this.WordSplitBehavior = other.WordSplitBehavior;
            this.DetermineLocalityCallback = other.DetermineLocalityCallback;
            this.ContinueProcessingPredicate = other.ContinueProcessingPredicate;
        }

        #region Overridden methods and operators

        /// <summary>
        /// Provides a string representation of these difference options.
        /// </summary>
        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture,
                    "Type: {0}, Locality: {1}, IgnoreTrimWhiteSpace: {2}, WordSplitBehavior: {3}, DetermineLocalityCallback: {4}, ContinueProcessingPredicate: {5}",
                    this.DifferenceType, this.Locality, this.IgnoreTrimWhiteSpace, this.WordSplitBehavior, this.DetermineLocalityCallback, this.ContinueProcessingPredicate);
        }

        /// <summary>
        /// Provides a hash function for the type.
        /// </summary>
        public override int GetHashCode()
        {
            int callbackHashCode = (this.DetermineLocalityCallback != null) ? this.DetermineLocalityCallback.GetHashCode() : 0;
            int predicateHashCode = (this.ContinueProcessingPredicate != null)? this.ContinueProcessingPredicate.GetHashCode() : 0;
            return (this.DifferenceType.GetHashCode() ^ this.Locality.GetHashCode() ^ this.IgnoreTrimWhiteSpace.GetHashCode() ^ this.WordSplitBehavior.GetHashCode() ^ callbackHashCode ^ predicateHashCode);
        }

        /// <summary>
        /// Determines whether two StringDifferenceOptions are the same.
        /// </summary>
        /// <param name="obj">The object to compare.</param>
        public override bool Equals(object obj)
        {
            if (!(obj is StringDifferenceOptions))
                return false;

            return this == (StringDifferenceOptions)obj;
        }

        /// <summary>
        /// Determines whether two StringDifferenceOptions are the same
        /// </summary>
        public static bool operator ==(StringDifferenceOptions left, StringDifferenceOptions right)
        {
            if (ReferenceEquals(left, right))
                return true;

            if ((object)left == null || (object)right == null)
                return false;

            return left.DifferenceType == right.DifferenceType &&
                   left.Locality == right.Locality &&
                   left.IgnoreTrimWhiteSpace == right.IgnoreTrimWhiteSpace &&
                   left.WordSplitBehavior == right.WordSplitBehavior &&
                   left.DetermineLocalityCallback == right.DetermineLocalityCallback &&
                   left.ContinueProcessingPredicate == right.ContinueProcessingPredicate;
        }

        /// <summary>
        /// Determines whether two StringDifferenceOptions are different.
        /// </summary>
        public static bool operator !=(StringDifferenceOptions left, StringDifferenceOptions right)
        {
            return !(left == right);
        }

        #endregion // Overridden methods and operators

    }

#pragma warning restore 0618
}
