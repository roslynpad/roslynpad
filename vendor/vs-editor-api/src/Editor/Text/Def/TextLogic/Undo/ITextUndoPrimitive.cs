//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Operations
{
    /// <summary>
    /// Represents an atomic operation that knows how to Do/Undo/Redo itself.
    /// </summary>
    public interface ITextUndoPrimitive
    {
        /// <summary>
        /// Gets or sets the <see cref="ITextUndoTransaction"/> that contains the primitive.
        /// </summary>
        ITextUndoTransaction Parent { get; set; }

        /// <summary>
        /// Determines whether it is currently possible to call Do() successfully.
        /// </summary>
        bool CanRedo { get; }

        /// <summary>
        /// Determines whether it is currently possible to call Undo() successfully.
        /// </summary>
        bool CanUndo { get; }

        /// <summary>
        /// Performs or redoes the operation.
        /// </summary>
        void Do();

        /// <summary>
        /// Performs rollback or undo on the operation.
        /// </summary>
        void Undo();

        /// <summary>
        /// Determines whether this undo primitive can merge with the specified undo primitive.
        /// </summary>
        /// <param name="older">The older primitive.</param>
        /// <returns><c>true</c> if the given primitive can merge with this one, <c>false</c> otherwise.</returns>
        bool CanMerge(ITextUndoPrimitive older);

        /// <summary>
        /// Performs the actual merge. 
        /// </summary>
        /// <param name="older">The older primitive to merge.</param>
        /// <returns>The replacement primitive.</returns>
        /// <remarks>
        /// The resulting <see cref="ITextUndoPrimitive"/> will be added to the transaction, and the
        /// two input primitives will be removed.
        /// </remarks>
        ITextUndoPrimitive Merge(ITextUndoPrimitive older);
    }
}