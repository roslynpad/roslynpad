//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Operations
{
    using System;
    using System.Collections.ObjectModel;

    /// <summary>
    /// Searches a <see cref="ITextSnapshot"/> with different search options.
    /// </summary>
    /// <remarks>This is a MEF component part, and should be imported as follows:
    /// [Import]
    /// ITextSearchService textSearch = null;
    /// </remarks>
    public interface ITextSearchService
    {
        /// <summary>
        /// Searches for the next occurrence of the search string.
        /// </summary>
        /// <param name="startIndex">
        /// The index from which to begin the search.
        /// </param>
        /// <param name="wraparound">
        /// Determines whether the search wraps to the beginning of the buffer when it reaches the end of the buffer.
        /// </param>
        /// <param name="findData">
        /// The data to use for this search.
        /// </param>
        /// <returns>
        /// The <see cref="SnapshotSpan"/> containing the match if a match was found, or null if no matches were found.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="startIndex"/> is less than zero or greater than the length of the data.</exception>
        /// <exception cref="ArgumentException"> The UseRegularExpressions flag is set and the search string is an invalid regular expression.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="findData"/> is null.</exception>
        SnapshotSpan? FindNext(int startIndex, bool wraparound, FindData findData);

        /// <summary>
        /// Searches for all the occurrences of the search string.
        /// </summary>
        /// <param name="findData">
        /// The data to use for this search.
        /// </param>
        /// <returns>
        /// A list of all the matches, or null if no matches were found.
        /// </returns>
        /// <exception cref="ArgumentException"> The UseRegularExpressions flag of the find options is set and the search string is an invalid regular expression.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="findData"/> is null.</exception>
        Collection<SnapshotSpan> FindAll(FindData findData);
    }
}
