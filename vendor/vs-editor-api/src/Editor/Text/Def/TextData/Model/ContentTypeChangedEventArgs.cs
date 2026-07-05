//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text
{
    using System;
    using Microsoft.VisualStudio.Utilities;

    /// <summary>
    /// Provides information about a change to the <see cref="IContentType"/> on an <see cref="ITextBuffer"/>.
    /// </summary>
    public class ContentTypeChangedEventArgs : TextSnapshotChangedEventArgs
    {

        #region Private Members

        #endregion

        /// <summary>
        /// Initializes a new instance of <see cref="ContentTypeChangedEventArgs"/>
        /// with the specified before and after snapshots and before and after content types.
        /// </summary>
        /// <param name="beforeSnapshot">The most recent <see cref="ITextSnapshot"/> before the change occurred.</param>
        /// <param name="afterSnapshot">The <see cref="ITextSnapshot"/> immediately after the change occurred.</param>
        /// <param name="beforeContentType">The <see cref="IContentType"/> before the change occurred.</param>
        /// <param name="afterContentType">The <see cref="IContentType"/> after the change occurred.</param>
        /// <param name="editTag">An arbitrary object associated with this change.</param>
        /// <exception cref="ArgumentNullException"> One of <paramref name="beforeSnapshot"/>, 
        /// <paramref name="afterSnapshot"/>, <paramref name="beforeContentType"/>, or
        /// <paramref name="afterContentType"/> is null.</exception>
        public ContentTypeChangedEventArgs(ITextSnapshot beforeSnapshot,
                                           ITextSnapshot afterSnapshot,
                                           IContentType beforeContentType,
                                           IContentType afterContentType,
                                           object editTag)
            : base(beforeSnapshot, afterSnapshot, editTag)
        {
            BeforeContentType = beforeContentType ?? throw new ArgumentNullException(nameof(beforeContentType));
            AfterContentType = afterContentType ?? throw new ArgumentNullException(nameof(afterContentType));
        }

        /// <summary>
        /// The <see cref="IContentType"/> before the change occurred.
        /// </summary>
        public IContentType BeforeContentType { get; }

        /// <summary>
        /// The <see cref="IContentType"/> after the change occurred.
        /// </summary>
        public IContentType AfterContentType { get; }
    }
}
