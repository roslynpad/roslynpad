//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.IncrementalSearch
{

#pragma warning disable CA1066 // Type {0} should implement IEquatable<T> because it overrides Equals
    /// <summary>
    /// Consolidates the result of an incremental search operation.
    /// </summary>
    /// <remarks>
    /// This result indicates whether the item was found, whether the search
    /// caused the cursor to wrap around the beginning or end of the buffer, and
    /// the position of the first result.
    /// </remarks>
    public struct IncrementalSearchResult
#pragma warning restore CA1066 // Type {0} should implement IEquatable<T> because it overrides Equals
    {
        #region Public Properties

        /// <summary>
        /// Determines whether the search wrapped around the start of the buffer to its end.
        /// </summary>
        /// <remarks>This is applicable only if the search direction is backward.</remarks>
        public bool PassedStartOfBuffer { get; private set; }

        /// <summary>
        /// Determines whether the search wrapped around the end of the buffer to its beginning.
        /// </summary>
        /// <remarks>This is applicable only if the search direction is forward.</remarks>
        public bool PassedEndOfBuffer { get; private set; }

        /// <summary>
        /// Determines whether the search passed the first item found.
        /// </summary>
        public bool PassedStartOfSearch { get; private set; }

        /// <summary>
        /// Determines whether the search for the term was successful.
        /// </summary>
        public bool ResultFound { get; private set; }

        #endregion //Public Properties

        /// <summary>
        /// Initializes a new instance of <see cref="IncrementalSearchResult"/> with the specified properties.
        /// </summary>
        /// <param name="passedEndOfBuffer"></param>
        /// <param name="passedStartOfBuffer"></param>
        /// <param name="passedStartOfSearch"></param>
        /// <param name="resultFound"></param>
        public IncrementalSearchResult(bool passedEndOfBuffer, bool passedStartOfBuffer, bool passedStartOfSearch, bool resultFound) : this()
        {
            PassedEndOfBuffer = passedEndOfBuffer;
            PassedStartOfBuffer = passedStartOfBuffer;
            PassedStartOfSearch = passedStartOfSearch;
            ResultFound = resultFound;
        }

        #region Object Overrides

        /// <summary>
        /// Determines whether the contents of two <see cref="IncrementalSearchResult"/> objects are the same.
        /// </summary>
        /// <param name="obj">The object to be compared.</param>
        /// <returns><c>true</c> if both objects have the same content, otherwise <c>false</c>.</returns>
        public override bool Equals(object obj)
        {
            if (obj is IncrementalSearchResult)
            {
                return ((IncrementalSearchResult)obj) == this;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Determines whether the contents of two <see cref="IncrementalSearchResult"/> objects are the same.
        /// </summary>
        /// <returns><c>true</c> if both objects have the same content, otherwise <c>false</c>.</returns>
        public static bool operator == (IncrementalSearchResult first, IncrementalSearchResult second)
        {
            return (first.PassedEndOfBuffer == second.PassedEndOfBuffer &&
                first.PassedStartOfBuffer == second.PassedStartOfBuffer &&
                first.PassedStartOfSearch == second.PassedStartOfSearch &&
                first.ResultFound == second.ResultFound);
        }

        /// <summary>
        /// Determines whether the contents of two <see cref="IncrementalSearchResult"/> objects are different.
        /// </summary>
        /// <returns><c>true</c> if both objects have different content, otherwise <c>false</c>.</returns>
        public static bool operator != (IncrementalSearchResult first, IncrementalSearchResult second)
        {
            return !(first == second);
        }

        /// <summary>
        /// Gets the hash code for the object.
        /// </summary>
        /// <returns>base class' implementation</returns>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        #endregion //Object Overrides

    }
}
