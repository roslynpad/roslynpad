//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Operations
{
    /// <summary>
    /// Defines operations relating to the editor, in addition to operations defined by <see cref="IEditorOperations2"/>.
    /// </summary>
    public interface IEditorOperations3 : IEditorOperations2
    {
        /// <summary>
        /// Inserts a new line at the end of the document if it's not there yet.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the edit succeeded, otherwise <c>false</c>.
        /// </returns>
        bool InsertFinalNewLine();

        /// <summary>
        /// Deletes all white space from ends of the selected lines.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the edit succeeded, otherwise <c>false</c>.
        /// </returns>
        /// <remarks>
        /// <para>
        /// The algorithm for this operation follows these rules:
        /// </para>
        /// <para>
        /// If there is no selection, the trailing white space is deleted on all lines in the document.
        /// </para>
        /// <para>
        /// If there is a selection, then the trailing white space is deleted on all lines the selection spans.
        /// </para>
        /// </remarks>
        bool TrimTrailingWhiteSpace();

        /// <summary>
        /// Duplicates the current selection, or the whole line (if there is no selection), without changing the clipboard.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the edit succeeded, otherwise <c>false</c>.
        /// </returns>
        /// <remarks>
        /// Multiple selection cases like block selection will treat each selection independently.
        /// </remarks>
        bool DuplicateSelection();
    }
}
