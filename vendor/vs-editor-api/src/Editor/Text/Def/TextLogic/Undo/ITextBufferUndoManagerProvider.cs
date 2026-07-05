//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Operations
{
    using System;

    /// <summary>
    /// Provides an <see cref="ITextBufferUndoManager"/> for a given <see cref="ITextBuffer"/>.  This is a cached factory, and only
    /// one <see cref="ITextBufferUndoManager"/> will ever be created for a given <see cref="ITextBuffer"/>.
    /// </summary>
    /// <remarks>This is a MEF component part, and should be exported with the following attribute:
    /// [Export(NameSource=typeof(ITextBufferUndoManagerProvider))]
    /// </remarks>
    public interface ITextBufferUndoManagerProvider
    {
        /// <summary>
        /// Gets the <see cref="ITextBufferUndoManager"/> for the specified <see cref="ITextBuffer"/>. If no undo manager
        /// has been created for this text buffer, a new one is created.
        /// </summary>
        /// <param name="textBuffer">The <see cref="ITextBuffer"/> for which to get the <see cref="ITextBufferUndoManager"/>.</param>
        /// <returns>The <see cref="ITextBufferUndoManager"/> for <paramref name="textBuffer"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="textBuffer" /> is null.</exception>
        ITextBufferUndoManager GetTextBufferUndoManager(ITextBuffer textBuffer);

        /// <summary>
        /// Removes the <see cref="ITextBufferUndoManager"/>, if any, from <paramref name="textBuffer" />.
        /// </summary>
        /// <param name="textBuffer">The <see cref="ITextBuffer"/> to check.</param>
        /// <exception cref="ArgumentNullException"><paramref name="textBuffer" /> is null.</exception>
        void RemoveTextBufferUndoManager(ITextBuffer textBuffer);
    }
}
