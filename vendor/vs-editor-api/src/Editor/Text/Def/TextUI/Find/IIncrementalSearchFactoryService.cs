//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.IncrementalSearch
{

    using Microsoft.VisualStudio.Text.Editor;

    /// <summary>
    /// Used to get or create an incremental search service for a given <see cref="ITextView"/>.
    /// There will always be a maximum of one <see cref="IIncrementalSearch"/>
    /// for a given <see cref="ITextView"/>.
    /// </summary>
    /// <remarks>This is a MEF component part, and should be imported as follows:
    /// [Import]
    /// IIncrementalSearchFactoryService factory = null;
    /// </remarks>
    public interface IIncrementalSearchFactoryService
    {
        /// <summary>
        /// Gets an <see cref="IIncrementalSearch" /> for the specified <see cref="ITextView" />.
        /// If there is no <see cref="IIncrementalSearch" /> for the view, one
        /// will be created.
        /// </summary>
        /// <param name="textView">
        /// The <see cref="ITextView"/> over which the incremental search is to be performed.
        /// </param>
        /// <returns>
        /// An <see cref="IIncrementalSearch"/> associated with the <see cref="ITextView"/>.
        /// </returns>
        IIncrementalSearch GetIncrementalSearch(ITextView textView);
    }
}
