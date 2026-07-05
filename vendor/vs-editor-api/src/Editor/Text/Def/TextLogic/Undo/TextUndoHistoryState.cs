//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Operations
{
    /// <summary>
    /// Provides information about the <see cref="ITextUndoHistory"/>.
    /// </summary>
    public enum TextUndoHistoryState
    {
        /// <summary>
        /// The <see cref="ITextUndoHistory"/> is not in the process of performing an undo or redo.
        /// </summary>
        /// <remarks>
        /// If you care whether the <see cref="ITextUndoHistory"/> is altering its contents, be sure to check CurrentTransaction also.
        /// </remarks>
        Idle,

        /// <summary>
        /// The <see cref="ITextUndoHistory"/> is in the process of executing its Undo method.
        /// </summary>
        Undoing,

        /// <summary>
        /// The <see cref="ITextUndoHistory"/> is in the process of executing its Redo method.
        /// </summary>
        Redoing,
    }
}
