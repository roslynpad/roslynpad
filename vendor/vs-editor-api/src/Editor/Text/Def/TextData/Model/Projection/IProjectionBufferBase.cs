//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Projection
{
    using System;
    using System.Collections.Generic;

    //TODO: rename IProjectionBuffer when ready for API changes

    /// <summary>
    /// A text buffer that contains projections of other text buffers, composed
    /// of a list of text spans of those buffers. The other buffers that contribute to
    /// the projection buffer are called source buffers, and the text spans that describe
    /// the contributed regions are called source spans.
    /// </summary>
    public interface IProjectionBufferBase : ITextBuffer
    {
        /// <summary>
        /// The current snapshot of the contents of the projection buffer.
        /// </summary>
        /// <returns></returns>
        new IProjectionSnapshot CurrentSnapshot { get; }

        /// <summary>
        /// The set of <see cref="ITextBuffer"/> objects that directly contribute to the projection buffer.
        /// </summary>
        IList<ITextBuffer> SourceBuffers { get; }

        #region Editing shortcuts
        /// <summary>
        /// Inserts the given <paramref name="text"/> at the specified <paramref name="position"/> in the <see cref="ITextBuffer"/>.
        /// </summary>
        /// <param name="position">The buffer position at which the first character of the text will appear.</param>
        /// <param name="text">The text to be inserted.</param>
        /// <remarks>
        /// This is a shortcut for creating a new <see cref="ITextEdit"/> object, using it to insert the text, and then applying it. If the insertion
        /// fails on account of a read-only region, the snapshot returned will be the same as the current snapshot of the buffer before
        /// the attempted insertion.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="position"/> is less than zero or greater than the length of the buffer.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is null.</exception>
        /// <exception cref="InvalidOperationException">A TextEdit is currently active.</exception>
        new IProjectionSnapshot Insert(int position, string text);

        /// <summary>
        /// Deletes a span of characters from the buffer.
        /// </summary>
        /// <param name="deleteSpan">The span of characters to delete.</param>
        /// <remarks>
        /// This is a shortcut for creating a new <see cref="ITextEdit"/> object, using it to delete the text, and then applying it. If the deletion
        /// fails on account of a read-only region, the snapshot returned will be the same as the current snapshot of the buffer before
        /// the attempted deletion.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="deleteSpan"/>.The end of the span is greater than the length of the buffer.</exception>
        /// <exception cref="InvalidOperationException">A TextEdit is currently active.</exception>
        new IProjectionSnapshot Delete(Span deleteSpan);

        /// <summary>
        /// Replaces a span of characters with different text. This is equivalent to first deleting the text to be replaced and then
        /// inserting the new text.
        /// </summary>
        /// <param name="replaceSpan">The span of characters to replace.</param>
        /// <param name="replaceWith">The new text.</param>
        /// <remarks>
        /// This is a shortcut for creating a new <see cref="ITextEdit"/> object, using it to replace the text, and then applying it. If the replacement
        /// fails on account of a read-only region, the snapshot returned will be the same as the current snapshot of the buffer before
        /// the attempted replacement.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="replaceSpan"/>.The end of the span is greater than the length of the buffer.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="replaceWith"/>is null.</exception>
        /// <exception cref="InvalidOperationException">A TextEdit is currently active.</exception>
        new IProjectionSnapshot Replace(Span replaceSpan, string replaceWith);
        #endregion
    }
}
