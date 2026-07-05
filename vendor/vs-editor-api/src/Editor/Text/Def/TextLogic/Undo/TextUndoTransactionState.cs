//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Operations
{
    /// <summary>
    /// Holds the state of the transaction. 
    /// </summary>
    /// <remarks>
    /// There are five rough groups of transactions.
    /// Open transactions are being defined. Canceled transactions have been aborted and are empty. Completed and undone
    /// transactions have been defined and are ready for undo and redo, respectively. Undoing and redoing are
    /// transient states as the transaction passes between completed and undone. Invalid is a state for transactions that
    /// have expired.
    /// </remarks>
    public enum UndoTransactionState
    {
        /// <summary>
        /// Represents the initial state of the transaction, after it has been created and before it is canceled or completed.
        /// </summary>
        Open,

        /// <summary>
        /// Indicates that the transaction is no longer being defined, and is eligible for undo.
        /// </summary>
        Completed,
        
        /// <summary>
        /// Indicates that the transaction is no longer being defined, but has been aborted and cleared.
        /// </summary>
        Canceled,

        /// <summary>
        /// Indicates a transient state set by Do(), between the undone state and the completed state.
        /// </summary>
        Redoing,

        /// <summary>
        /// Indicates a transient state set by Undo(), between the completed state and the Undone state.
        /// </summary>
        Undoing,

        /// <summary>
        /// Indicates that Undo() was called after completion.
        /// </summary>
        Undone,

        /// <summary>
        /// Indicates that the transaction has been removed the undo history stack, for example because it was on the redo stack when
        /// a new operation cleared the redo stack. Once a transaction is invalid it should not be used for anything.
        /// </summary>
        Invalid
    }
}