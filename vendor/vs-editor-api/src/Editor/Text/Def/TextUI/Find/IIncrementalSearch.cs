//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.IncrementalSearch
{

    /// <summary>
    /// Defines an incremental search operation. 
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <see cref="IIncrementalSearch"/> interface is associated
    /// with a <see cref="Morgania.Text.Editor.ITextView"/>.
    /// </para>
    /// <para>
    /// After the Start() method is called, the current caret position is marked as the start of the search, 
    /// and the <see cref="AppendCharAndSearch"/> and <see cref="DeleteCharAndSearch"/> operations can be used to change the search term. 
    /// The direction of the search is set to forward by default, although this setting can be changed with the <see cref="SearchDirection"/> property.
    /// If a matching term is found, it is selected and the caret is moved to the end of the selected word. 
    /// </para>
    /// <para>
    /// Every search operation returns an <see cref="IncrementalSearchResult"/>, which includes 
    /// information about the search, such as whether the search looped around the start or 
    /// end of the buffer, whether the search looped around the starting position of the search,
    /// and whether the item was found. It is the responsibility of the caller
    /// to pass this information to the end user.
    /// </para>
    /// <para>
    /// Incremental search performs its search on the text snapshot of the <see cref="Morgania.Text.Editor.ITextView"/>. As a result, if the
    /// result falls within a collapsed outlining region, the region will be expanded before the result is selected.
    /// </para>
    /// </remarks>
    public interface IIncrementalSearch
    {

        #region Methods

        /// <summary>
        /// Starts an incremental search operation, and marks the position of the caret
        /// as the starting position for the search.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">
        /// An incremental search session is in progress.
        /// To avoid raising this exception, check the <see cref="IsActive"/> property before calling
        /// <c>Start</c>.
        /// </exception>
        void Start();

        /// <summary>
        /// Terminates an incremental search operation.
        /// </summary>
        /// <exception creg="System.InvalidOperationException">
        /// <see cref="Dismiss"/> was called before <see cref="Start"/>. A search must be
        /// started before it can be terminated.
        /// </exception>
        void Dismiss();

        /// <summary>
        /// Extends the current term being searched for by one character. If a new term is matched, it 
        /// is selected. The selection can be used to access the match.
        /// </summary>
        /// <param name="toAppend">
        /// The character to append to the current search term.
        /// </param>
        /// <returns>
        /// An <see cref="IncrementalSearchResult"/> that contains information about whether the search term was found and whether
        /// the search wrapped around the beginning or end of the buffer.
        /// </returns>
        IncrementalSearchResult AppendCharAndSearch(char toAppend);

        /// <summary>
        /// Removes the last character of the current search term and updates the
        /// search results based on the new term. 
        /// </summary>
        /// <returns>
        /// An <see cref="IncrementalSearchResult"/> that indicates whether the new search term was found
        /// and whether the search wrapped around the beginning or end of 
        /// the buffer.
        /// </returns>
        /// <exception cref="System.InvalidOperationException">
        /// The search string is empty. To avoid this exception,
        /// check the <see cref="SearchString"/> property before calling this method.
        /// </exception>
        IncrementalSearchResult DeleteCharAndSearch();

        /// <summary>
        /// Selects the next result in an incremental search operation. 
        /// The matched term will be selected.
        /// </summary>
        /// <returns>
        /// An <see cref="IncrementalSearchResult"/> indicating whether the newly selected item caused a
        /// wrap around the end or beginning of the document and whether the search looped around the first item found.
        /// </returns>
        IncrementalSearchResult SelectNextResult();

        /// <summary>
        /// Clears the existing search term without changing the selection.
        /// </summary>
        void Clear();

        #endregion //Methods

        #region Public Properties

        /// <summary>
        /// Gets or sets the current search term.
        /// </summary>
        string SearchString { get; set; }

        /// <summary>
        /// Determines whether an incremental search is in process.
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// Gets or sets the direction of the incremental search.
        /// </summary>
        IncrementalSearchDirection SearchDirection { get; set; }

        /// <summary>
        /// Gets the <see cref="Microsoft.VisualStudio.Text.Editor.ITextView"/> associated with this search.
        /// </summary>
        Microsoft.VisualStudio.Text.Editor.ITextView TextView { get; }

        #endregion //Public Properties
    }
}
