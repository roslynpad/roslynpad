//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
using System;

namespace Microsoft.VisualStudio.Text.Operations
{
    /// <summary>
    /// Provides information for the UndoRedoHappened event raised by <see cref="ITextUndoHistory"/>, about the effect of the undo or redo operation.
    /// </summary>
    public class TextUndoRedoEventArgs : EventArgs
    {
        private TextUndoHistoryState state;
        private ITextUndoTransaction transaction;

        /// <summary>
        /// Initializes a new instance of <see cref="TextUndoRedoEventArgs"/>.
        /// </summary>
        /// <param name="state">The <see cref="TextUndoHistoryState"/>.</param>
        /// <param name="transaction">The <see cref="ITextUndoTransaction"/>.</param>
        public TextUndoRedoEventArgs(TextUndoHistoryState state, ITextUndoTransaction transaction)
        {
            this.state = state;
            this.transaction = transaction;
        }

        /// <summary>
        /// Gets the transaction that was processed in this undo or redo.
        /// </summary>
        public ITextUndoTransaction Transaction
        {
            get { return this.transaction; }
        }

        /// <summary>
        /// Gets the state of the transaction.
        /// </summary>
        /// <remarks>
        /// The state is either UndoTransactionState.Undoing or UndoTransactionState.Redoing.
        /// </remarks>
        public TextUndoHistoryState State
        {
            get { return this.state; }
        }
    }
}
