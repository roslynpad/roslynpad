//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Editor
{
    /// <summary>
    /// Listens for when <see cref="ITextView"/>s are created.
    /// </summary>
    /// <remarks>This is a MEF component part, and should be exported with the following attribute:
    /// [Export(typeof(ITextViewCreationListener))]
    /// [ContentType("...")]
    /// [TextViewRole("...")]
    /// </remarks>
    public interface ITextViewCreationListener
    {
        /// <summary>
        /// Called when a text view having matching roles is created over a text data model having a matching content type.
        /// </summary>
        /// <param name="textView">The newly created text view.</param>
        void TextViewCreated(ITextView textView);
    }
}
