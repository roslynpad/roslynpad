//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text
{
    using System;

    /// <summary>
    /// Information provided before content changes.
    /// </summary>
    public class PreContentChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the text snapshot before the change.
        /// </summary>
        public ITextSnapshot BeforeSnapshot { get; private set; }

        /// <summary>
        /// Gets the collection of changes.
        /// </summary>
        public INormalizedTextChangeCollection Changes { get; private set; }

        /// <summary>
        /// Initializes a new instance of <see cref="PreContentChangedEventArgs"/>.
        /// </summary>
        /// <param name="beforeSnapshot">A text snapshot before the change.</param>
        /// <param name="changes">The collection of changes.</param>
        public PreContentChangedEventArgs(ITextSnapshot beforeSnapshot, INormalizedTextChangeCollection changes)
        {
            this.BeforeSnapshot = beforeSnapshot;
            this.Changes = changes;
        }
    }
}
