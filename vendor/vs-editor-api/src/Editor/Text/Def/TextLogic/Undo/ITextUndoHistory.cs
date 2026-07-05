//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.Text.Operations
{
    /// <summary>
    /// Contains undo transactions.
    /// </summary>
    /// <remarks>
    /// Typically only one undo transaction history at a time is availbble to the user.
    /// </remarks>
    public interface ITextUndoHistory : IPropertyOwner
    {
        /// <summary>
        /// The undo stack for this history. It does not include any currently open or redo transactions.        
        /// </summary>
        /// <remarks>
        /// This stack includes the most recent transaction (the top item of the stack) to the oldest transaction (the bottom
        /// item of the stack).
        /// </remarks>
        IEnumerable<ITextUndoTransaction> UndoStack { get; }

        /// <summary>
        /// The redo stack for this history. It does not include any currently open or undo transactions.
        /// </summary>
        /// <remarks>
        /// This stack includes the most recent transaction (the top item of the stack) to the oldest transaction (the bottom
        /// item of the stack).
        /// </remarks>
        IEnumerable<ITextUndoTransaction> RedoStack { get; }

        /// <summary>
        /// Gets the most recent (top) item of the <see cref="ITextUndoHistory.UndoStack"/>, or <c>null</c> if the stack is
        /// empty.
        /// </summary>
        ITextUndoTransaction LastUndoTransaction { get; }

        /// <summary>
        /// Gets the most recent (top) item of the <see cref="ITextUndoHistory.RedoStack"/>, or <c>null</c> if the stack is
        /// empty.
        /// </summary>
        ITextUndoTransaction LastRedoTransaction { get; }

        /// <summary>
        /// Determines whether a single undo is possible.        
        /// </summary>
        /// <remarks>
        /// This property corresponds to CanUndo for the most recent visible undo <see cref="ITextUndoTransaction"/>. 
        /// If there are hidden transactions on top of the visible transaction, 
        /// this property returns true only if they are 
        /// undoable as well.
        /// </remarks>
        bool CanUndo { get; }

        /// <summary>
        /// Determines whether a single redo is possible.
        /// </summary>
        /// <remarks>
        /// This property corresponds to CanRedo for the most recent visible redo <see cref="ITextUndoTransaction"/>. 
        /// If there are hidden transactions on top of the visible transaction, this property returns <c>true</c> only if they are 
        /// redoable as well.
        /// </remarks>
        bool CanRedo { get; }

        /// <summary>
        /// Gets the description of the most recent visible undo <see cref="ITextUndoTransaction"/>.
        /// </summary>
        string UndoDescription { get; }

        /// <summary>
        /// Gets the description of the most recent visible redo <see cref="ITextUndoTransaction"/>.
        /// </summary>
        string RedoDescription { get; }

        /// <summary>
        /// Gets the current UndoTransaction in progress.
        /// </summary>
        ITextUndoTransaction CurrentTransaction { get; }

        /// <summary>
        /// Gets the current state of the UndoHistory.
        /// </summary>
        TextUndoHistoryState State { get; }        

        /// <summary>
        /// Creates a new transaction, nests it in the previously current transaction, and marks it current.
        /// </summary>
        /// <param name="description">The description of the transaction.</param>
        /// <returns>The new transaction.</returns>
        ITextUndoTransaction CreateTransaction(string description);

        /// <summary>
        /// Performs the specified number of undo operations and places the transactions on the redo stack.        
        /// </summary>
        /// <param name="count">
        /// The number of undo operations to perform. 
        /// </param>        
        /// <remarks>
        /// At the end of the operation, the specified number of visible
        /// transactions are undone. Therefore, the actual number of transactions undone might be more than this number if there are 
        /// hidden transactions above or below the visible ones.
        /// After the last visible transaction is undone, the hidden transactions left on top the stack are undone as well, until a 
        /// visible or linked transaction is encountered, or the stack is completely emptied.
        /// </remarks>
        void Undo(int count);

        /// <summary>
        /// Performs the specified number of redo operation and places the transactions on the undo stack.        
        /// </summary>
        /// <param name="count">The number of redo operations to perform. At the end of the operation, the specified number of visible
        /// transactions are redone. Therefore, the actual number of transactions redone might be more than this number, if there are 
        /// hidden transactions above or below the visible ones.
        /// </param>        
        /// <remarks>
        /// After the last visible transaction is redone, the hidden transactions left on top the stack are redone as well, until a 
        /// visible or linked transaction is encountered, or the stack is completely emptied.
        /// </remarks>
        void Redo(int count);

        /// <summary>
        /// Notifies consumers when an undo
        /// or a redo has happened on this history. 
        /// </summary>
        /// <remarks>
        /// The sender object is the <see cref="ITextUndoHistory"/> that originated
        /// it, and the event arguments are empty. The UndoHistory raises this event whenever an Undo() or
        /// Redo() is initiated properly, regardless of whether one of the particular transactions or
        /// primitives fails to perform that undo.
        /// </remarks>
        event EventHandler<TextUndoRedoEventArgs> UndoRedoHappened;

        /// <summary>
        /// Notifies consumers when an 
        /// <see cref="ITextUndoTransaction"/> is completed and added to the <see cref="ITextUndoHistory.UndoStack"/>. 
        /// </summary>
        /// <remarks>
        /// The sender object is the <see cref="ITextUndoHistory"/> that originated it, and the event argumentss are an 
        /// instance of <see cref="TextUndoTransactionCompletedEventArgs"/> class. This event is fired for the 
        /// topmost <see cref="ITextUndoTransaction"/> objects only. Completion of nested transactions does not generate 
        /// this event.
        /// </remarks>
        event EventHandler<TextUndoTransactionCompletedEventArgs> UndoTransactionCompleted;
    }
}