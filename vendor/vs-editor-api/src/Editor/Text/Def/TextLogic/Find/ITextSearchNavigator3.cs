//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
using System;
#nullable enable

namespace Microsoft.VisualStudio.Text.Operations
{
    /// <summary>
    /// Provides a service to navigate between search results on a <see cref="ITextBuffer"/> and to
    /// perform replacements.
    /// </summary>
    public interface ITextSearchNavigator3 : IDisposable
    {
        /// <summary>
        /// The term to search for.
        /// </summary>
        /// <remarks>
        /// Modifying the <see cref="SearchTerm"/> does not perform a search. To do so, call the
        /// <see cref="Find"/> method.
        /// </remarks>
        string? SearchTerm { get; set; }

        /// <summary>
        /// The term to replace matches with.
        /// </summary>
        string? ReplaceTerm { get; set; }

        /// <summary>
        /// Sets or gets options used for the search.
        /// </summary>
        /// <remarks>
        /// Modifying the <see cref="SearchOptions"/> don't change the current search. To perform a search
        /// using the new options, call the <see cref="Find"/> method.
        /// </remarks>
        FindOptions SearchOptions { get; set; }

        /// <summary>
        /// Indicates the position in <see cref="ITextBuffer.CurrentSnapshot"/> at which the search should be started.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If <see cref="CurrentResult"/> is not null then <see cref="CurrentResult"/> will
        /// be used as the starting point for the next search or replace operation.
        /// </para>
        /// <para>
        /// If <see cref="CurrentResult"/> is null and this value is also null, then
        /// the beginning of the document will be used as the search's starting point.
        /// </para>
        /// StartPoint can be set to a snapshot point belonging to any <see cref="ITextSnapshot"/> belonging
        /// to this <see cref="ITextBuffer"/>. However, value returned by this property is always 
        /// translated to current snapshot.
        /// </remarks>
        SnapshotPoint? StartPoint { get; set; }

        /// <summary>
        /// Indicates the range that should be searched (if any).
        /// </summary>
        /// <remarks>
        /// If the <see cref="SearchSpan"/> is null then the entire document will be searched. Otherwise only results that
        /// are contained by the provided span will be returned.
        /// </remarks>
        ITrackingSpan? SearchSpan { get; set; }

        /// <summary>
        /// Returns the <see cref="SnapshotSpan"/> corresponding to the result of the last
        /// find operation. If no matches were found or if no search has been performed yet,
        /// null is returned.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If <see cref="CurrentResult"/> is not null, then the next find operation will search
        /// from either endpoint of the current result depending on the search direction.
        /// </para>
        /// </remarks>
        SnapshotSpan? CurrentResult { get; }

        /// <summary>
        /// Finds the next occurrence of the text matching the <see cref="SearchTerm"/>.
        /// </summary>
        /// <returns>
        /// Returns <c>true</c> if a match is found, <c>false</c> otherwise.
        /// </returns>
        bool Find();

        /// <summary>
        /// Replaces the current result with <see cref="ReplaceTerm"/>.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the edit succeeded, <c>false</c> otherwise.
        /// </returns>
        bool Replace();

        /// <summary>
        /// Clears the current result.
        /// </summary>
        /// <remarks>
        /// Searches will be performed starting from the <see cref="StartPoint"/> when
        /// no current result is available.
        /// </remarks>
        void ClearCurrentResult();

#pragma warning disable CA2227 // Collection properties should be read only
        /// <summary>
        /// Indicates the ranges that should be searched (if any).
        /// </summary>
        /// <remarks>
        /// If this value to a non-null value will effectively override the ITextSearchNavigator3.SearchSpan property.
        /// </remarks>
        NormalizedSnapshotSpanCollection? SearchSpans { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only
    }
}