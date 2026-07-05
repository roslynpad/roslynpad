//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text
{
    using System;
    using Microsoft.VisualStudio.Utilities;

    /// <summary>
    /// Provides information about a change to the <see cref="IContentType"/> of an <see cref="ITextDataModel"/>.
    /// </summary>
    public class TextDataModelContentTypeChangedEventArgs : EventArgs
    {
        /// <summary>
        /// The <see cref="IContentType"/> of the <see cref="ITextDataModel"/> before the change.
        /// </summary>
        public IContentType BeforeContentType { get; private set; }

        /// <summary>
        /// The <see cref="IContentType"/> of the <see cref="ITextDataModel"/> after the change.
        /// </summary>
        public IContentType AfterContentType { get; private set; }

        /// <summary>
        /// Constructs a <see cref="TextDataModelContentTypeChangedEventArgs"/>.
        /// </summary>
        /// <param name="beforeContentType">The <see cref="IContentType"/> before the change.</param>
        /// <param name="afterContentType">The <see cref="IContentType"/> after the change.</param>
        public TextDataModelContentTypeChangedEventArgs(IContentType beforeContentType, IContentType afterContentType)
        {
            BeforeContentType = beforeContentType;
            AfterContentType = afterContentType;
        }
    }
}
