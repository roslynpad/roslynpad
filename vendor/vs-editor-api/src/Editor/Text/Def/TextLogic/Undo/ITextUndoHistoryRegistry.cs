//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Operations
{
    /// <summary>
    /// Maps context objects to <see cref="ITextUndoHistory"/> objects and is meant to be exposed by a component part.
    /// </summary>
    public interface ITextUndoHistoryRegistry
    {
        /// <summary>
        /// Gets, and if necessary creates, a history associated with the context. 
        /// </summary>
        /// <param name="context">An arbitrary context object.</param>
        /// <returns>A (possibly new) <see cref="ITextUndoHistory"/> associated with the context object.</returns>
        /// <remarks>Only a weak reference is held to the context.</remarks>
        ITextUndoHistory RegisterHistory(object context);

        /// <summary>
        /// Gets a history associated with the context, but does not create a new one.
        /// </summary>
        /// <param name="context">An arbitrary context object.</param>
        /// <returns>An <see cref="ITextUndoHistory"/> associated with the context object.</returns>
        ITextUndoHistory GetHistory(object context);

        /// <summary>
        /// Gets a history associated with the context, but does not create a new one.
        /// </summary>
        /// <param name="context">An arbitrary context object.</param>
        /// <param name="history">An <see cref="ITextUndoHistory"/> associated with the context object.</param>
        /// <returns><c>true</c> if a relevant <see cref="ITextUndoHistory"/> exists in this registry, otherwise <c>false</c>.</returns>
        bool TryGetHistory(object context, out ITextUndoHistory history);

        /// <summary>
        /// Attaches an existing <see cref="ITextUndoHistory"/> to a new context. The context must not already be mapped in this registry. 
        /// </summary>
        /// <param name="context">An arbitrary context object.</param>
        /// <param name="history">An <see cref="ITextUndoHistory"/> object to associate with the context.</param>
        /// <remarks>Only a weak reference is held to the context.</remarks>
        void AttachHistory(object context, ITextUndoHistory history);

        /// <summary>
        /// Removes all mappings to a given <see cref="ITextUndoHistory"/> in this registry, if any exist.
        /// </summary>
        /// <param name="history">The <see cref="ITextUndoHistory"/> to remove from the registry.</param>
        void RemoveHistory(ITextUndoHistory history);
    }
}