//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
using System;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.Text.Operations
{
    /// <summary>
    /// Represents a container for <see cref="ITextUndoPrimitive"/> objects. UndoTransactions are tracked in an UndoHistory.
    /// </summary>
    public interface ITextUndoTransaction : IDisposable
    {
        /// <summary>
        /// Gets or sets the description
        /// </summary>
        string Description { get; set; }

        /// <summary>
        /// Gets the <see cref="UndoTransactionState"/> for the <see cref="ITextUndoTransaction"/>.
        /// </summary>
        UndoTransactionState State { get; }

        /// <summary>
        ///Gets the <see cref="ITextUndoHistory"/> that contains this transaction.
        /// </summary>
        ITextUndoHistory History { get; }

        /// <summary>
        /// Gets the collection of <see cref="ITextUndoPrimitive"/> objects in this container.
        /// </summary>
        /// <remarks>
        /// <para>You should try to get these primitives only after the transaction has been completed.</para>
        /// <para>You cannot modify the list except during merging 
        /// (i.e. from your <see cref="IMergeTextUndoTransactionPolicy.PerformTransactionMerge"/> implementation).</para>
        /// </remarks>
        IList<ITextUndoPrimitive> UndoPrimitives { get; }

        /// <summary>
        /// Marks the transaction as finished and eligible for undo.
        /// </summary>
        void Complete();

        /// <summary>
        /// Marks an open transaction as canceled, and undoes and clears any primitives that have been added.
        /// </summary>
        void Cancel();

        /// <summary>
        /// Adds a new primitive to the end of the list when the transaction is open.
        /// </summary>
        /// <param name="undo"></param>
        void AddUndo(ITextUndoPrimitive undo);

        /// <summary>
        /// Gets the <see cref="ITextUndoTransaction"/> that contains this transaction. 
        /// </summary>
        /// <remarks>
        /// This property can be null if this is a root transaction. It is transient, since completed transactions are not nested.
        /// </remarks>
        ITextUndoTransaction Parent { get; }

        /// <summary>
        /// Determines whether it is currently possible to call Do() successfully.
        /// </summary>
        bool CanRedo { get; }

        /// <summary>
        /// Determines whether it is currently possible to call Undo() successfully.
        /// </summary>
        bool CanUndo { get; }

        /// <summary>
        /// Performs a do or redo.
        /// </summary>
        void Do();

        /// <summary>
        /// Performs a rollback or undo.
        /// </summary>
        void Undo();

        /// <summary>
        /// Gets the <see cref="IMergeTextUndoTransactionPolicy"/> associated with this transaction.
        /// </summary>
        IMergeTextUndoTransactionPolicy MergePolicy { get; set; }
    }
}
