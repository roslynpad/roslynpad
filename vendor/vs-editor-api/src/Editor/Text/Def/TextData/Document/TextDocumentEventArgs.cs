//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text
{
    using System;

    /// <summary>
    /// Provides information for events when an <see cref="ITextDocument"/> has been created or disposed.
    /// </summary>
    public class TextDocumentEventArgs : EventArgs
    {
        #region Private Members

        ITextDocument _textDocument;

        #endregion

        /// <summary>
        /// Initializes a new instance of a <see cref="TextDocumentEventArgs"/>.
        /// </summary>
        /// <param name="textDocument">The <see cref="ITextDocument"/> that was created or disposed.</param>
        public TextDocumentEventArgs(ITextDocument textDocument)
        {
            _textDocument = textDocument;
        }

        /// <summary>
        /// Gets the <see cref="ITextDocument"/> that was created or disposed.
        /// </summary>
        public ITextDocument TextDocument
        {
            get
            {
                return _textDocument;
            }
        }
    }
}
