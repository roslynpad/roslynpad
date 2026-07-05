//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Operations
{
    /// <summary>
    /// Provides the merge policy for undo transactions. 
    /// </summary>
    /// <remarks>
    /// These policies are
    /// used when transactions are completed and pushed onto the undo stack. Only adjacent
    /// <see cref="ITextUndoTransaction"/> objects can be merged.
    /// </remarks>
    public interface IMergeTextUndoTransactionPolicy
    {
        /// <summary>
        /// Determines whether one <see cref="IMergeTextUndoTransactionPolicy"/> is compatible with another.
        /// </summary>
        /// <param name="other">The <see cref="IMergeTextUndoTransactionPolicy"/> to test.</param>
        /// <returns><c>true</c> if the merge should proceed, otherwise <c>false</c>.</returns>
        /// <remarks>
        /// Merging happens only when merge policies in primitives are compatible. This function should be symmetric
        /// and ideally constant time. For instance, (this.GetType() == other.GetType()).
        /// </remarks>
        bool TestCompatiblePolicy(IMergeTextUndoTransactionPolicy other);

        /// <summary>
        /// Determines whether two <see cref="ITextUndoTransaction"/> objects can be merged.
        /// </summary>
        /// <param name="newerTransaction">The newer transaction.</param>
        /// <param name="olderTransaction">The older transaction.</param>
        /// <returns><c>true</c> of the merge should proceed, otherwise <c>false</c>.</returns>
        /// <summary>
        /// If this method returns <c>true</c>, then the merge can proceed, given specific knowledge of the transactions in question. CanMerge
        /// is  called only when TestCompatiblePolicy succeeds.
        /// </summary>
        bool CanMerge(ITextUndoTransaction newerTransaction, ITextUndoTransaction olderTransaction);

        /// <summary>
        /// Merges a new <see cref="ITextUndoTransaction"/> with an existing one.
        /// </summary>
        /// <param name="existingTransaction">The existing transaction.</param>
        /// <param name="newTransaction">The new transaction.</param>
        /// <remarks>
        /// Merges newTransaction into existingTransaction by adding, removing, or modifying the
        /// primitives in existingTransaction.UndoPrimitives.  A simple implementation could be to add
        /// each primitive in newTransaction.UndoPrimitives to existingTransaction.UndoPrimitives.
        /// </remarks>
        void PerformTransactionMerge(ITextUndoTransaction existingTransaction, ITextUndoTransaction newTransaction);
    }
}
