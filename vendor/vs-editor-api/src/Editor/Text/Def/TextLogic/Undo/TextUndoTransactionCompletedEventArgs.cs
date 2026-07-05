//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
using System;

namespace Microsoft.VisualStudio.Text.Operations
{
    /// <summary>
    /// Provides information for the <see cref="ITextUndoHistory.UndoTransactionCompleted"/> event raised by the <see cref="ITextUndoHistory"/>. 
    /// </summary>
    /// <remarks>
    /// These event arguments contain the <see cref="ITextUndoTransaction"/> that has been added 
    /// and the result of the completion. This event is fired only for
    /// the topmost <see cref="ITextUndoTransaction"/> that is placed on the <see cref="ITextUndoHistory.UndoStack"/>. Completion of nested
    /// transactions does not raise this event.
    /// </remarks>
    public class TextUndoTransactionCompletedEventArgs : EventArgs
    {        
        private ITextUndoTransaction transaction;
        private TextUndoTransactionCompletionResult result;

        /// <summary>
        /// Initializes a new instance of <see cref="TextUndoTransactionCompletedEventArgs"/>.
        /// </summary>        
        /// <param name="transaction">The <see cref="ITextUndoTransaction"/>.</param>
        /// <param name="result">The <see cref="TextUndoTransactionCompletionResult"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="transaction"/> is null.</exception>
        public TextUndoTransactionCompletedEventArgs(ITextUndoTransaction transaction, TextUndoTransactionCompletionResult result)
        {
            this.transaction = transaction;
            this.result = result;
        }

        /// <summary>
        /// Gets the transaction that was added to the <see cref="ITextUndoHistory"/>.
        /// </summary>
        public ITextUndoTransaction Transaction
        {
            get { return this.transaction; }
        }

        /// <summary>
        /// Gets the result of the completed transaction. 
        /// </summary>
        /// <remarks>
        /// See <see cref="TextUndoTransactionCompletionResult"/> for the possible outcomes.
        /// </remarks>
        public TextUndoTransactionCompletionResult Result
        {
            get { return this.result; }
        }
    }

    /// <summary>
    /// Describes the possible results of a transaction completion for an <see cref="ITextUndoHistory"/>.
    /// </summary>
    public enum TextUndoTransactionCompletionResult
    {
        /// <summary>
        /// The most recent transaction is added to the <see cref="ITextUndoHistory.UndoStack"/> of the <see cref="ITextUndoHistory"/>.
        /// </summary>
        TransactionAdded,

        /// <summary>
        /// The most recent transaction is merged with the transaction on the top of the <see cref="ITextUndoHistory.UndoStack"/> of 
        /// the associated <see cref="ITextUndoHistory"/>.
        /// </summary>
        TransactionMerged,
    }
}
