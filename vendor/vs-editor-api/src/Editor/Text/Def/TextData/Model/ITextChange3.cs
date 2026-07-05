//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text
{
    /// <summary>
    /// Extends <see cref="ITextChange2"/> with an ability to efficiently get a substring of old and new text.
    /// </summary>
    public interface ITextChange3 : ITextChange2
    {
        /// <summary>
        /// Gets the text that was replaced, starting at the beginning of the span and having length equal to the length of the span.
        /// </summary>
        /// <param name="span">The span of text to return.</param>
        string GetOldText(Span span);

        /// <summary>
        /// The text that replaced the old text, starting at the beginning of the span and having length equal to the length of the span.
        /// </summary>
        /// <param name="span">The span of text to return.</param>
        string GetNewText(Span span);

        /// <summary>
        /// Gets a character at given position of the text that was replaced.
        /// </summary>
        /// <param name="position">The position of the character to return.</param>
        char GetOldTextAt(int position);

        /// <summary>
        /// Gets a character at given position of the text that replaced the old text.
        /// </summary>
        /// <param name="position">The position of the character to return.</param>
        char GetNewTextAt(int position);
    }
}
