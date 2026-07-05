//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//

using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.Text
{
    /// <summary>
    /// Used to create new multi caret brokers.
    /// <remarks>
    /// This is a MEF component part, and should be imported as follows:
    /// [Import]
    /// IMultiSelectionBrokerFactory factory = null;
    /// </remarks>

    public interface IMultiSelectionBrokerFactory
    {
        /// <summary>
        /// Gets a new <see cref="IMultiSelectionBroker"/> for the specified <see cref="ITextView"/>.
        /// To get an existing one, use <see cref="TextViewExtensions.GetMultiSelectionBroker(ITextView)"/>.
        /// </summary>
        /// <param name="textView">The view for which to obtain an <see cref="IMultiSelectionBroker"/>.</param>
        /// <returns>
        /// An <see cref="IMultiSelectionBroker"/> associated with the <see cref="ITextView"/>
        /// <see cref="ITextView"/>.
        /// </returns>
        IMultiSelectionBroker CreateBroker(ITextView textView);
    }
}
