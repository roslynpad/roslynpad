//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Operations
{
    /// <summary>
    /// Registers the <see cref="ITextUndoHistory"/> for a <see cref="TextBuffer"/>,  
    /// listens for change events on a <see cref="TextBuffer"/>, 
    /// and adds <see cref="ITextUndoPrimitive"/> objects to the <see cref="ITextUndoHistory"/>.
    /// </summary>
    public interface ITextBufferUndoManager
    {
        /// <summary>
        /// Gets the <see cref="ITextBuffer"/> for which this <see cref="ITextBufferUndoManager"/> manages undo operations.
        /// </summary>
        ITextBuffer TextBuffer { get; }

        /// <summary>
        /// Gets the <see cref="ITextUndoHistory"/> for the underlying <see cref="ITextBuffer"/>.
        /// </summary>
        ITextUndoHistory TextBufferUndoHistory { get; }

        /// <summary>
        /// Unregisters the <see cref="ITextUndoHistory"/> for the underlying <see cref="ITextBuffer"/> from the <see cref="ITextUndoHistoryRegistry"/>.
        /// </summary>
        void UnregisterUndoHistory();
    }
}
