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
    public interface ITextUndoHistory2 : ITextUndoHistory
    {
        /// <summary>
        /// Creates a new transaction, invisible, nests it in the previously current transaction, and marks it current.
        /// </summary>
        /// <param name="description">The description of the transaction.</param>
        /// <returns>The new transaction.</returns>
        /// <remarks>
        /// <para>Invisible transactions are like normal undo transactions except that they are effectively invisible to the end user. They won't be displayed
        /// in the undo stack and if the user does an "undo" then all the invisible transactions leading up to the 1st non-invisible transaction are "skipped".</para>
        /// <para>Invisible transactions can only contain simple text edits (other types of undo actions will be lost and potentially corrupt the undo stack).</para>
        /// </remarks>
        ITextUndoTransaction CreateInvisibleTransaction(string description);
    }
}
