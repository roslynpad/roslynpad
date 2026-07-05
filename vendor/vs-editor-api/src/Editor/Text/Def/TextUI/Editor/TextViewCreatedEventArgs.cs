//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Editor
{
    using System;

    /// <summary>
    /// Provides information for newly created <see cref="ITextView"/>.
    /// </summary>
    public class TextViewCreatedEventArgs : EventArgs
    {
        /// <summary>
        /// The newly created <see cref="ITextView"/>.
        /// </summary>
        public ITextView TextView { get; private set; }

        /// <summary>
        /// Constructs a <see cref="TextViewCreatedEventArgs"/>.
        /// </summary>
        /// <param name="textView">The <see cref="ITextView"/> that was created.</param>
        public TextViewCreatedEventArgs(ITextView textView)
        {
            if (textView == null)
            {
                throw new ArgumentNullException(nameof(textView));
            }
            TextView = textView;
        }
    }
}
