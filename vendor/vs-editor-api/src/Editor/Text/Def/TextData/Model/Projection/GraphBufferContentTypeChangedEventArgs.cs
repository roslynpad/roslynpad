//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Projection
{
    using System;
    using Microsoft.VisualStudio.Utilities;

    /// <summary>
    /// Provides data about a change of <see cref="IContentType"/> on a member of a <see cref="IBufferGraph"/>.
    /// </summary>
    public class GraphBufferContentTypeChangedEventArgs : EventArgs
    {
        private ITextBuffer textBuffer;
        private IContentType beforeContentType;
        private IContentType afterContentType;

        /// <summary>
        /// Initializes a new instance of <see cref="GraphBufferContentTypeChangedEventArgs"/> with the specified
        /// text buffer and the old and new content types.
        /// </summary>
        /// <param name="textBuffer">The <see cref="ITextBuffer"/> whose <see cref="IContentType"/> has changed.</param>
        /// <param name="beforeContentType">The <see cref="IContentType"/> before the change.</param>
        /// <param name="afterContentType">The <see cref="IContentType"/> after the change.</param>
        /// <exception cref="ArgumentNullException">One of <paramref name="textBuffer"/>, <paramref name="beforeContentType"/>, 
        /// or <paramref name="afterContentType"/> is null.</exception>
        public GraphBufferContentTypeChangedEventArgs(ITextBuffer textBuffer, IContentType beforeContentType, IContentType afterContentType)
        {
            if (textBuffer == null)
            {
                throw new ArgumentNullException(nameof(textBuffer));
            }
            if (beforeContentType == null)
            {
                throw new ArgumentNullException(nameof(beforeContentType));
            }
            if (afterContentType == null)
            {
                throw new ArgumentNullException(nameof(afterContentType));
            }
            this.textBuffer = textBuffer;
            this.beforeContentType = beforeContentType;
            this.afterContentType = afterContentType;
        }

        /// <summary>
        /// The <see cref="ITextBuffer"/> whose <see cref="IContentType"/> has changed.
        /// </summary>
        public ITextBuffer TextBuffer
        {
            get { return this.textBuffer; }
        }

        /// <summary>
        /// The <see cref="IContentType"/> before the change.
        /// </summary>
        public IContentType BeforeContentType
        {
            get { return this.beforeContentType; }
        }

        /// <summary>
        /// The <see cref="IContentType"/> after the change.
        /// </summary>
        public IContentType AfterContentType
        {
            get { return this.afterContentType; }
        }
    }
}
