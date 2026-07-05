//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Operations
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Provides methods for searching contents of a <see cref="ITextSnapshot"/>. Additionally, provides
    /// helper methods for performing replace operations.
    /// </summary>
    public interface ITextSearchService2 : ITextSearchService
    {
        /// <summary>
        /// Searches for the next occurrence of the search string.
        /// </summary>
        /// <param name="startingPosition">
        /// The position from which to begin the search. The search will be performed on the <see cref="ITextSnapshot"/> to which
        /// this parameter belongs.
        /// </param>
        /// <param name="searchPattern">
        /// The pattern to search for.
        /// </param>
        /// <param name="options">
        /// Specifies options used for the search operation.
        /// </param>
        /// <returns>
        /// A <see cref="SnapshotSpan"/> containing the match if a match was found, or null if no matches were found.
        /// </returns>
        /// <remarks>
        /// This method is safe to be executed from any thread.
        /// </remarks>
        /// <exception cref="ArgumentException">
        /// The <see cref="FindOptions.UseRegularExpressions"/> flag is set and the search string is an invalid regular expression.
        /// </exception>
        SnapshotSpan? Find(SnapshotPoint startingPosition, string searchPattern, FindOptions options);

        /// <summary>
        /// Searches for the next occurrence of the search string.
        /// </summary>
        /// <param name="searchRange">
        /// The range of text to search in.
        /// </param>
        /// <param name="startingPosition">
        /// The position from which to begin the search. The search will be performed on the <see cref="ITextSnapshot"/> to which
        /// this parameter belongs.
        /// </param>
        /// <param name="options">
        /// Specifies options used for the search operation.
        /// </param>
        /// <param name="searchPattern">
        /// The pattern to search for.
        /// </param>
        /// <returns>
        /// A <see cref="SnapshotSpan"/> containing the match if a match was found, or null if no matches were found.
        /// </returns>
        /// <remarks>
        /// This method can be executed from any thread.
        /// </remarks>
        /// <exception cref="ArgumentException">
        /// The <see cref="FindOptions.UseRegularExpressions"/> flag is set and the search string is an invalid regular expression.
        /// </exception>
        SnapshotSpan? Find(SnapshotSpan searchRange, SnapshotPoint startingPosition, string searchPattern, FindOptions options);

        /// <summary>
        /// Searches for the next occurrence of <paramref name="searchPattern"/> and sets <paramref name="expandedReplacePattern"/> to the result of
        /// the text replacement.
        /// </summary>
        /// <param name="startingPosition">
        /// The position from which search is started. The search will be performed on the <see cref="ITextSnapshot"/> to which this
        /// parameter belongs.
        /// </param>
        /// <param name="searchPatterh">
        /// The pattern to look for.
        /// </param>
        /// <param name="replacePattern">
        /// The pattern to replace the found text with.
        /// </param>
        /// <param name="options">
        /// Options used to perform the search.
        /// </param>
        /// <param name="expandedReplacePattern">
        /// The result of the replacement. This output parameter will be useful when performing regular expression searches. Will be empty
        /// if no matches are found.
        /// </param>
        /// <returns>
        /// A <see cref="SnapshotSpan"/> pointing to the search result found. If no matches are found, null is returned.
        /// </returns>
        /// <remarks>
        /// <para>
        /// This function does not perform any edits. The consumers would need to create an <see cref="ITextEdit"/> to perform the actual text
        /// replacement if desired. This method is safe to be executed from any thread.
        /// </para>
        /// <para>
        /// Note that <paramref name="expandedReplacePattern"/> will always equal <paramref name="replacePattern"/> if the search is not using regular
        /// expressions. In those scenarios you can utilize the more lightweight <see cref="Find(SnapshotSpan, SnapshotPoint, string, FindOptions)"/>.
        /// </para>
        /// </remarks>
        SnapshotSpan? FindForReplace(SnapshotPoint startingPosition, string searchPattern, string replacePattern, FindOptions options, out string expandedReplacePattern);

        /// <summary>
        /// Searches for the next occurrence of <paramref name="searchPattern"/> and sets <paramref name="expandedReplacePattern"/> to the result of
        /// the text replacement.
        /// </summary>
        /// <param name="searchRange">
        /// The range of text to search in.
        /// </param>
        /// <param name="searchPatterh">
        /// The pattern to look for.
        /// </param>
        /// <param name="replacePattern">
        /// The pattern to replace the found text with.
        /// </param>
        /// <param name="options">
        /// Options used to perform the search.
        /// </param>
        /// <param name="expandedReplacePattern">
        /// The result of the replacement. This output parameter will be useful when performing regular expression searches. Will be empty
        /// if no matches are found.
        /// </param>
        /// <returns>
        /// A <see cref="SnapshotSpan"/> pointing to the search result found. If no matches are found, null is returned.
        /// </returns>
        /// <remarks>
        /// <para>
        /// This function does not perform any edits. The consumers would need to create an <see cref="ITextEdit"/> to perform the actual text
        /// replacement if desired. This method is safe to be executed from any thread.
        /// </para>
        /// <para>
        /// Note that <paramref name="expandedReplacePattern"/> will always equal <paramref name="replacePattern"/> if search is not using regular
        /// expressions. In those scenarios you can utilize the more lightweight <see cref="Find(SnapshotSpan, SnapshotPoint, string, FindOptions)"/>.
        /// </para>
        /// </remarks>
        SnapshotSpan? FindForReplace(SnapshotSpan searchRange, string searchPattern, string replacePattern, FindOptions options, out string expandedReplacePattern);

        /// <summary>
        /// Finds all occurrences of the <paramref name="searchPattern"/> in <paramref name="searchRange"/>.
        /// </summary>
        /// <param name="searchRange">
        /// The range to search in.
        /// </param>
        /// <param name="searchPattern">
        /// The pattern to search for.
        /// </param>
        /// <param name="options">
        /// The options to use while performing the search operation.
        /// </param>
        /// <returns>
        /// An <see cref="IEnumerable{SnapshotSpan}"/> containing all occurrences of the <paramref name="searchPattern"/>.
        /// </returns>
        /// <remarks>
        /// This method is safe to execute on any thread.
        /// </remarks>
        IEnumerable<SnapshotSpan> FindAll(SnapshotSpan searchRange, string searchPattern, FindOptions options);

        /// <summary>
        /// Finds all occurrences of the <paramref name="searchPattern"/> in <paramref name="searchRange"/> starting from
        /// <paramref name="startingPosition"/>.
        /// </summary>
        /// <param name="searchRange">
        /// The range to search in.
        /// </param>
        /// <param name="startingPosition">
        /// The location from which the search should be started.
        /// </param>
        /// <param name="searchPattern">
        /// The pattern to search for.
        /// </param>
        /// <param name="options">
        /// The options to use while performing the search operation.
        /// </param>
        /// <returns>
        /// An <see cref="IEnumerable{SnapshotSpan}"/> containing all occurrences of the <paramref name="searchPattern"/>.
        /// </returns>
        /// <remarks>
        /// This method is safe to execute on any thread.
        /// </remarks>
        IEnumerable<SnapshotSpan> FindAll(SnapshotSpan searchRange, SnapshotPoint startingPosition, string searchPattern, FindOptions options);

        /// <summary>
        /// Searches for all occurrences of the <paramref name="searchPattern"/> and calculates all
        /// the corresponding replacement results for every match according to the <paramref name="replacePattern"/>.
        /// </summary>
        /// <param name="searchRange">
        /// The range of text to search in.
        /// </param>
        /// <param name="searchPattern">
        /// The pattern to search for.
        /// </param>
        /// <param name="replacePattern">
        /// The replace pattern to use for the operation.
        /// </param>
        /// <param name="options">
        /// The options to use while performing the search operation.
        /// </param>
        /// <returns>
        /// An <see cref="IEnumerable{T}"/> containing all matches found and their corresponding replacement values.
        /// </returns>
        /// <remarks>
        /// <para>
        /// The returned <see cref="IEnumerable{T}"/> will contain a collection of tuples indicating all the matches. Each
        /// <see cref="Tuple"/> will contain a <see cref="SnapshotSpan"/> referencing the location of the match and a <see cref="string"/>
        /// containing the calculated replacement text for the match. 
        /// </para>
        /// <para>
        /// If you are not using regular expressions then the calculated replacement text will always 
        /// equal the <paramref name="replacePattern"/>. In that scenario, you can use the
        /// <see cref="ITextSearchService2.FindAll(SnapshotSpan, string, FindOptions)"/> method to only obtain the search results.
        /// </para>
        /// </remarks>
        IEnumerable<Tuple<SnapshotSpan, string>> FindAllForReplace(SnapshotSpan searchRange, string searchPattern, string replacePattern, FindOptions options);
    }
}
