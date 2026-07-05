//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
using System;

namespace Microsoft.VisualStudio.Text.Operations
{
#pragma warning disable CA1066 // Type {0} should implement IEquatable<T> because it overrides Equals
    /// <summary>
    /// Represents the set of data used in a search by the <see cref="ITextSearchService"/>.
    /// </summary>
    public struct FindData
#pragma warning restore CA1066 // Type {0} should implement IEquatable<T> because it overrides Equals
    {
        private string _searchString;
        private ITextSnapshot _textSnapshotToSearch;

        /// <summary>
        /// Initializes a new instance of <see cref="FindData"/> with the specified search pattern, text snapshot,
        /// find options, and text structure navigator.
        /// </summary>
        /// <param name="searchPattern">The search pattern.</param>
        /// <param name="textSnapshot">The <see cref="ITextSnapshot"/> to search.</param>
        /// <param name="findOptions">The <see cref="FindOptions"/> to use during the search.</param>
        /// <param name="textStructureNavigator">The <see cref="ITextStructureNavigator"/> to use during the search.</param>
        /// <exception cref="ArgumentNullException"><paramref name="searchPattern"/> or <paramref name="textSnapshot"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="searchPattern"/> is an empty string.</exception>
        public FindData(string searchPattern, ITextSnapshot textSnapshot, FindOptions findOptions, ITextStructureNavigator textStructureNavigator)
        {
            if (searchPattern == null)
            {
                throw new ArgumentNullException(nameof(searchPattern));
            }
            if (searchPattern.Length == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(searchPattern));
            }

            _searchString = searchPattern;
            _textSnapshotToSearch = textSnapshot ?? throw new ArgumentNullException(nameof(textSnapshot));
            FindOptions = findOptions;
            TextStructureNavigator = textStructureNavigator;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="FindData"/> with the specified search pattern and text snapshot.
        /// </summary>
        /// <param name="searchPattern">The search pattern.</param>
        /// <param name="textSnapshot">The <see cref="ITextSnapshot"/> to search.</param>
        public FindData(string searchPattern, ITextSnapshot textSnapshot)
            : this(searchPattern, textSnapshot, FindOptions.None, null)
        {
        }


        internal FindData(ITextSnapshot textSnapshot) // For unit testing
        {
            _searchString = null;
            _textSnapshotToSearch = textSnapshot;
            FindOptions = FindOptions.None;
            TextStructureNavigator = null;
        }

        /// <summary>
        /// Gets or sets the string to use in the search.
        /// </summary>
        /// <exception cref="ArgumentNullException">The value is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The value is an empty string.</exception>
        public string SearchString
        {
            get { return _searchString; }
            set 
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }
                if (value.Length == 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }
                _searchString = value;
            }
        }

        /// <summary>
        /// Determines whether two <see cref="FindData"/> objects are the same.
        /// </summary>
        /// <param name="obj">The object to compare.</param>
        /// <returns><c>true</c> if the objects are the same, otherwise <c>false</c>.</returns>
        public override bool Equals(object obj)
        {
            if (obj is FindData)
            {
                FindData other = (FindData)obj;

                return (string.Equals(_searchString, other._searchString, StringComparison.Ordinal)) &&
                       (FindOptions == other.FindOptions) &&
                       object.ReferenceEquals(_textSnapshotToSearch, other._textSnapshotToSearch) &&
                       object.ReferenceEquals(TextStructureNavigator, other.TextStructureNavigator);
            }
            return false;
        }

        /// <summary>
        /// Gets the hash code for the object.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
            return _searchString.GetHashCode();
        }

        /// <summary>
        /// Converts the <see cref="FindData"/> object to a string.
        /// </summary>
        /// <returns>The string representation of the <see cref="FindData"/> object.</returns>
        public override string ToString()
        {
            return base.ToString();
        }

        /// <summary>
        /// Determines whether two <see cref="FindData"/> objects are the same.
        /// </summary>
        /// <param name="data1">The first object.</param>
        /// <param name="data2">The second object.</param>
        /// <returns><c>true</c> if the objects are the same, otherwise <c>false</c>.</returns>
        public static bool operator ==(FindData data1, FindData data2)
        {
            return data1.Equals(data2);
        }

        /// <summary>
        /// Determines whether two <see cref="FindData"/> objects are different.
        /// </summary>
        /// <param name="data1">The first object.</param>
        /// <param name="data2">The second object.</param>
        /// <returns><c>true</c> if the two objects are different, otherwise <c>false</c>.</returns>
        public static bool operator !=(FindData data1, FindData data2)
        {
            return data1.Equals(data2);
        }

        /// <summary>
        /// Gets or sets the options that are used for the search.
        /// </summary>
        public FindOptions FindOptions { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="ITextSnapshot"/> on which to perform the search.
        /// </summary>
        ///  <exception cref="ArgumentNullException">The value is null.</exception>
        public ITextSnapshot TextSnapshotToSearch
        {
            get { return _textSnapshotToSearch; }
            set
            {
                _textSnapshotToSearch = value ?? throw new ArgumentNullException(nameof(value));
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="ITextStructureNavigator"/> to use in determining word boundaries.
        /// </summary>
        public ITextStructureNavigator TextStructureNavigator { get; set; }
    }
}
