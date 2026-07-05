//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text
{
    using System;

    /// <summary>
    /// Provides information about an edit transaction on an <see cref="ITextBuffer"/>.
    /// </summary>
    public class TextContentChangedEventArgs : TextSnapshotChangedEventArgs
    {
        #region Private Members and Construction

        private EditOptions options;

        /// <summary>
        /// Initializes an new instance of <see cref="TextContentChangedEventArgs"/> for a Change event.
        /// </summary>
        /// <param name="beforeSnapshot">The most recent <see cref="ITextSnapshot"/> before the change occurred.</param>
        /// <param name="afterSnapshot">The <see cref="ITextSnapshot"/> immediately after the change occurred.</param>
        /// <param name="options">Edit options that were applied to this change.</param>
        /// <param name="editTag">An arbitrary object associated with this change.</param>
        /// <exception cref="ArgumentNullException"><paramref name="beforeSnapshot"/> or
        /// <paramref name="afterSnapshot"/> or
        /// <paramref name="options"/> is null.</exception>
        public TextContentChangedEventArgs(ITextSnapshot beforeSnapshot,
                                           ITextSnapshot afterSnapshot,
                                           EditOptions options,
                                           Object editTag) : base(beforeSnapshot, afterSnapshot, editTag)
        {
            this.options = options;
        }
        #endregion // Private Members and Construction

        #region Public Properties

        /// <summary>
        /// Gets the set of changes that occurred.
        /// </summary>
        public INormalizedTextChangeCollection Changes
        {
            get { return this.Before.Version.Changes; }
        }

        /// <summary>
        /// Gets the edit options that were applied to this change.
        /// </summary>
        public EditOptions Options
        {
            get { return this.options; }
        }
        #endregion
    }
}
