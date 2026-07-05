//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain internal APIs that are subject to change without notice.
// Use at your own risk.
//

using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;

namespace Microsoft.VisualStudio.Text
{
    public static class TextBufferOperationHelpers
    {
        /// <summary>
        /// Checks if the given <see cref="ITextSnapshotLine"/> has any non-whitespace characters
        /// </summary>
        /// <param name="line">The <see cref="ITextSnapshotLine"/> on which the check is performed</param>
        /// <returns>True if the <see cref="ITextSnapshotLine"/>  contains any non-whitespace characters</returns>
        public static bool HasAnyNonWhitespaceCharacters(ITextSnapshotLine line)
        {
            return line.IndexOfPreviousNonWhiteSpaceCharacter(line.End.Position - line.Start.Position ) != -1;
        }

        /// <summary>
        /// For a given <see cref="ITextSnapshotLine"/> gets the new line character to be inserted to the line based on
        /// either the given line, or the second last line or the default new line charcter provided by <see cref="IEditorOptions"/>
        /// </summary>
        /// <param name="line">The <see cref="ITextSnapshotLine"/> for whcih the new line character is to be decied for</param>
        /// <param name="editorOptions">The current set of <see cref="IEditorOptions"/> applicable for the given <see cref="ITextSnapshotLine"/></param>
        /// <returns>The new line character to be inserted</returns>
        public static string GetNewLineCharacterToInsert(ITextSnapshotLine line, IEditorOptions editorOptions)
        {
            string lineBreak = null;
            var snapshot = line.Snapshot;

            if (editorOptions.GetReplicateNewLineCharacter())
            {
                if (line.LineBreakLength > 0)
                {
                    // use the same line ending as the current line
                    lineBreak = line.GetLineBreakText();
                }
                else
                {
                    if (snapshot.LineCount > 1)
                    {
                        // use the same line ending as the penultimate line in the buffer
                        lineBreak = snapshot.GetLineFromLineNumber(snapshot.LineCount - 2).GetLineBreakText();
                    }
                }
            }
            string textToInsert = lineBreak ?? editorOptions.GetNewLineCharacter();
            return textToInsert;
        }

        /// <summary>
        /// Inserts a final new line for the given <see cref="ITextBuffer"/> based on 
        /// whether the option to insert it is enabled in the current set of <see cref="IEditorOptions"/> applicable to the buffer
        /// </summary>
        /// <param name="buffer">The <see cref="ITextBuffer"/> in which the final new line has to be inserted in</param>
        /// <param name="editorOptions">The current set of <see cref="IEditorOptions"/> applicable to the buffer</param>
        /// <returns>Whether the operation on the buffer succeded or not</returns>
        public static bool TryInsertFinalNewLine(ITextBuffer buffer, IEditorOptions editorOptions)
        {
            var currentSnapshot = buffer.CurrentSnapshot;
            var lineCount = currentSnapshot.LineCount;
            var lastLine = currentSnapshot.GetLineFromLineNumber(lineCount - 1);
            ITextSnapshot changedSnapshot = null;

            if (lastLine.Start.Position != lastLine.EndIncludingLineBreak.Position) // Check if final new line is not already present
            {
                var IsTrimTrailingWhitespacesSetExplicitlyToFalse = editorOptions.IsOptionDefined<bool>(DefaultOptions.TrimTrailingWhiteSpaceOptionId, true) && !editorOptions.GetOptionValue<bool>(DefaultOptions.TrimTrailingWhiteSpaceOptionId);

                if (!IsTrimTrailingWhitespacesSetExplicitlyToFalse && !HasAnyNonWhitespaceCharacters(lastLine)) // Last Line contains only of whitespace and trim trailing whitespaces is set to false
                {
                    var spanToDelete = lastLine.ExtentIncludingLineBreak;
                    changedSnapshot = buffer.Delete(spanToDelete);
                }
                else  // Non empty last line or empty last line with trim trailing whitespaces set to false. Insert a new line after the current line
                {
                    string lineBreakToInsert = GetNewLineCharacterToInsert(lastLine, editorOptions);
                    var positionToInsertNewLine = lastLine.End.Position;
                    changedSnapshot = buffer.Insert(positionToInsertNewLine, lineBreakToInsert);
                }
                // Edits were successfull
                if (changedSnapshot != null && currentSnapshot != changedSnapshot)
                    return true;
                return false;
            }
            return true;
        }
    }
}
