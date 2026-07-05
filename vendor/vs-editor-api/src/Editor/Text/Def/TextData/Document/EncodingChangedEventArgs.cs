//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text
{
    using System;
    using System.Text;

    /// <summary>
    /// Provides information for the <see cref="ITextDocument.EncodingChanged"/> event.
    /// </summary>
    public sealed class EncodingChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of <see cref="EncodingChangedEventArgs"/>
        /// </summary>
        /// <param name="oldEncoding">The previous encoding.</param>
        /// <param name="newEncoding">The new encoding.</param>
        public EncodingChangedEventArgs(Encoding oldEncoding, Encoding newEncoding)
        {
            OldEncoding = oldEncoding;
            NewEncoding = newEncoding;
        }

        /// <summary>
        /// Gets the previous encoding.
        /// </summary>
        public Encoding OldEncoding { get; private set; }

        /// <summary>
        /// Gets the new encoding.
        /// </summary>
        public Encoding NewEncoding { get; private set; }
    }
}
