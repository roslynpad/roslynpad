//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Operations
{
    /// <summary>
    /// Defines operations relating to the editor, in addition to operations defined by <see cref="IEditorOperations"/>.
    /// </summary>
    public interface IEditorOperations2 : IEditorOperations
    {
        /// <summary>
        /// Moves the selected lines up above the line bordering the selection on top. 
        /// Moving up from the top of the file will return true, however no changes will be made.
        /// Collapsed regions being moved, and being moved over, will remain collapsed.
        /// Moves involving readonly regions will result in no changes being made.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the edit succeeded or no change was needed, otherwise <c>false</c>.
        /// </returns>
        bool MoveSelectedLinesUp();

        /// <summary>
        /// Moves the selected lines below the line bordering the selection on the bottom.
        /// Moving down from the bottom of the file will return true, however no changes will be made.
        /// Collapsed regions being moved, and being moved over, will remain collapsed.
        /// Moves involving readonly regions will result in no changes being made.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the edit succeeded or no change was needed, otherwise <c>false</c>.
        /// </returns>
        bool MoveSelectedLinesDown();
    }
}
