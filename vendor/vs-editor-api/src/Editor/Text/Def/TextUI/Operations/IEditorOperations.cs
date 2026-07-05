//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Operations
{
    using System;
    using System.IO;
    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.Text.Formatting;

    /// <summary>
    /// Defines operations relating to the editor.
    /// </summary>
    public interface IEditorOperations
    {
        #region Navigation Operations

        /// <summary>
        /// Selects from the given anchor point to active point, moving the caret to the new active
        /// point of the selection.  The selected span will be made visible.
        /// </summary>
        /// <param name="anchorPoint">The anchor point of the new selection.</param>
        /// <param name="activePoint">The active point of the new selection and position of the caret.</param>
        /// <remarks>This puts the selection in stream selection mode and does the minimum amount of required scrolling to ensure the selected span is visible.</remarks>
        void SelectAndMoveCaret(VirtualSnapshotPoint anchorPoint, VirtualSnapshotPoint activePoint);

        /// <summary>
        /// Selects from the given anchor point to active point, moving the caret to the new active
        /// point of the selection.  Additionally, ensure the selection is in the given selection
        /// mode, and make the selected span visible.
        /// </summary>
        /// <param name="anchorPoint">The anchor point of the new selection.</param>
        /// <param name="activePoint">The active point of the new selection and position of the caret.</param>
        /// <param name="selectionMode">The selection mode of the new selection.</param>
        /// <remarks>This does the minimum amount of required scrolling to ensure the selected span is visible.</remarks>
        void SelectAndMoveCaret(VirtualSnapshotPoint anchorPoint, VirtualSnapshotPoint activePoint, TextSelectionMode selectionMode);

        /// <summary>
        /// Selects from the given anchor point to active point, moving the caret to the new active
        /// point of the selection.  Additionally, ensure the selection is in the given selection
        /// mode, and make the selected span visible.
        /// </summary>
        /// <param name="anchorPoint">The anchor point of the new selection.</param>
        /// <param name="activePoint">The active point of the new selection and position of the caret.</param>
        /// <param name="selectionMode">The selection mode of the new selection.</param>
        /// <param name="scrollOptions">What, if any, scrolling is done in the view after the selection is made. If null, no scrolling is done.</param>
        void SelectAndMoveCaret(VirtualSnapshotPoint anchorPoint, VirtualSnapshotPoint activePoint, TextSelectionMode selectionMode, EnsureSpanVisibleOptions? scrollOptions);

        /// <summary>
        /// Moves the caret to the next character.
        /// </summary>
        /// <param name="extendSelection">
        /// If <c>true</c>, the selection is extended when the caret is moved; if <c>false</c>, the selection is not extended.
        /// </param>
        void MoveToNextCharacter(bool extendSelection);

        /// <summary>
        /// Moves the caret to the previous character.
        /// </summary>
        /// <param name="extendSelection">
        /// If <c>true</c>, the selection is extended when the caret is moved; if <c>false</c>, the selection is not extended.
        /// </param>
        void MoveToPreviousCharacter(bool extendSelection);

        /// <summary>
        /// Moves the caret to the next word.
        /// </summary>
        /// <param name="extendSelection">
        /// If <c>true</c>, the selection is extended when the caret is moved; if <c>false</c>, the selection is not extended.
        /// </param>
        void MoveToNextWord(bool extendSelection);

        /// <summary>
        /// Moves the caret to the previous word.
        /// </summary>
        /// <param name="extendSelection">
        /// If <c>true</c>, the selection is extended when the caret is moved; if <c>false</c>, the selection is not extended.
        /// </param>
        void MoveToPreviousWord(bool extendSelection);

        /// <summary>
        /// Moves the caret one line up.
        /// </summary>
        /// <param name="extendSelection">
        /// If <c>true</c>, the selection is extended when the caret is moved; if <c>false</c>, the selection is not extended.
        /// </param>
        void MoveLineUp(bool extendSelection);

        /// <summary>
        /// Moves the caret one line down.
        /// </summary>
        /// <param name="extendSelection">
        /// If <c>true</c>, the selection is extended when the caret is moved; if <c>false</c>, the selection is not extended.
        /// </param>
        void MoveLineDown(bool extendSelection);

        /// <summary>
        /// Moves the caret one page up.
        /// </summary>
        /// <param name="extendSelection">
        /// If <c>true</c>, the selection is extended when the caret is moved; if <c>false</c>, the selection is not extended.
        /// </param>
        void PageUp(bool extendSelection);

        /// <summary>
        /// Moves the caret one page down.
        /// </summary>
        /// <param name="extendSelection">
        /// If <c>true</c>, the selection is extended when the caret is moved; if <c>false</c>, the selection is not extended.
        /// </param>
        void PageDown(bool extendSelection);

        /// <summary>
        /// Moves the caret to the end of the line.
        /// </summary>
        /// <param name="extendSelection">
        /// If <c>true</c>, the selection is extended when the caret is moved; if <c>false</c>, the selection is not extended.
        /// </param>
        void MoveToEndOfLine(bool extendSelection);

        /// <summary>
        /// Moves the caret to the first column on the current line.
        /// </summary>
        /// <param name="extendSelection">
        /// If <c>true</c>, the selection is extended when the caret is moved; if <c>false</c>, the selection is not extended.
        /// </param>
        void MoveToStartOfLine(bool extendSelection);

        /// <summary>
        /// Moves the caret to the first text column on the line; if the caret is already
        /// at the first text column or there is no text, move the caret to the first column
        /// on the line.
        /// </summary>
        /// <param name="extendSelection">
        /// If <c>true</c>, the selection is extended when the caret is moved; if <c>false</c>, the selection is not extended.
        /// </param>
        /// <remarks>This is effectively the behavior of pressing the Home key</remarks>
        void MoveToHome(bool extendSelection);

        /// <summary>
        /// Moves the caret to the start of the specified line.
        /// </summary>
        /// <param name="lineNumber">
        /// The line number to which to move the caret.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="lineNumber"/> is less than zero 
        /// or greater than the line number of the last line in the text buffer.</exception>
        void GotoLine(int lineNumber);

        /// <summary>
        /// Moves the caret to the start of the document.
        /// </summary>
        /// <param name="extendSelection">
        /// If <c>true</c>, the selection is extended when the caret is moved; if <c>false</c>, the selection is not extended.
        /// </param>
        void MoveToStartOfDocument(bool extendSelection);

        /// <summary>
        /// Moves the caret at the end of the document.
        /// </summary>
        /// <param name="extendSelection">
        /// If <c>true</c>, the selection is extended when the caret is moved; if <c>false</c>, the selection is not extended.
        /// </param>
        void MoveToEndOfDocument(bool extendSelection);

        /// <summary>
        /// Moves the current line to the top of the view.
        /// </summary>
        void MoveCurrentLineToTop();

        /// <summary>
        /// Moves the current line to the bottom of the view.
        /// </summary>
        void MoveCurrentLineToBottom();

        /// <summary>
        /// Moves the caret to the start of the line after all white space.
        /// </summary>
        /// <param name="extendSelection">
        /// If <c>true</c>, the selection is extended when the caret is moved; if <c>false</c>, the selection is not extended.
        /// </param>
        void MoveToStartOfLineAfterWhiteSpace(bool extendSelection);

        /// <summary>
        /// Moves the caret to the start of the next line after all white space.
        /// </summary>
        /// <param name="extendSelection">
        /// If <c>true</c>, the selection is extended when the caret is moved; if <c>false</c>, the selection is not extended.
        /// </param>
        /// <remarks>
        /// <para>
        /// If the caret is on the last line, this method moves it to the start of the line after all white space.
        /// </para>
        /// </remarks>
        void MoveToStartOfNextLineAfterWhiteSpace(bool extendSelection);

        /// <summary>
        /// Moves the caret to the start of the previous line after all white space.
        /// </summary>
        /// <param name="extendSelection">
        /// If <c>true</c>, the selection is extended when the caret is moved; if <c>false</c>, the selection is not extended.
        /// </param>
        /// <remarks>
        /// <para>
        /// If the caret is on the first line, this method moves it to the start of the ine after all white space.
        /// </para>
        /// </remarks>
        void MoveToStartOfPreviousLineAfterWhiteSpace(bool extendSelection);

        /// <summary>
        /// Moves the caret to just before the last non-white space character in the line.
        /// </summary>
        /// <param name="extendSelection">
        /// If <c>true</c>, the selection is extended when the caret is moved; if <c>false</c>, the selection is not extended.
        /// </param>
        /// <remarks>
        /// If the line is blank, the caret is moved to the start of the line.
        /// </remarks>
        void MoveToLastNonWhiteSpaceCharacter(bool extendSelection);

        /// <summary>
        /// Moves the caret to the first fully-visible line of the view.
        /// </summary>
        /// <param name="extendSelection">
        /// If <c>true</c>, the selection is extended when the caret is moved; if <c>false</c>, the selection is not extended.
        /// </param>
        void MoveToTopOfView(bool extendSelection);

        /// <summary>
        /// Moves the caret to the last fully-visible line of the view.
        /// </summary>
        /// <param name="extendSelection">
        /// If <c>true</c>, the selection is extended when the caret is moved; if <c>false</c>, the selection is not extended.
        /// </param>
        void MoveToBottomOfView(bool extendSelection);

        /// <summary>
        /// Swaps the caret from its current position to the other end of the selection.
        /// </summary>
        void SwapCaretAndAnchor();

        #endregion // Navigation Operations

        #region Edit Operations

        /// <summary>
        /// Deletes a character to the left of the current caret.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the edit succeeded, otherwise <c>false</c>.
        /// </returns>
        bool Backspace();

        /// <summary>
        /// Deletes the word to the right of the current caret position.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the edit succeeded, otherwise <c>false</c>.
        /// </returns>
        bool DeleteWordToRight();

        /// <summary>
        /// Deletes the word to the left of the current caret position.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the edit succeeded, otherwise <c>false</c>.
        /// </returns>
        bool DeleteWordToLeft();

        /// <summary>
        /// Deletes the line the caret is on, up to the line break character and the selection, if present.
        /// </summary>
        bool DeleteToEndOfLine();

        /// <summary>
        /// Deletes the line the caret is on, up to the previous line break character and the selection, if present.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the edit succeeded, otherwise <c>false</c>.
        /// </returns>
        bool DeleteToBeginningOfLine();

        /// <summary>
        /// Deletes all empty lines or lines that contain only white space in the selection.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the edit succeeded, otherwise <c>false</c>.
        /// </returns>
        bool DeleteBlankLines();

        /// <summary>
        /// Deletes all white space from the beginnings and ends of the selected lines, and trims internal white space.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the edit succeeded, otherwise <c>false</c>.
        /// </returns>
        /// <remarks>
        /// <para>
        /// The algorithm for this operation follows these rules:
        /// </para>
        /// <para>
        /// If there is no selection, the white space around the caret is trimmed so that only one space or tab remains.
        /// If there is only one space or tab, then this operation does nothing.
        /// </para>
        /// <para>
        /// If there is a selection, then the white space at the beginning or end of a line 
        /// contained within the selection is completely deleted.
        /// If there is at least one block of contiguous white space longer than one character 
        /// in the selection, then all white space between the first and last 
        /// non-white space characters is trimmed so that only one space or tab remains for each contiguous block.
        /// If there are only contiguous runs of a single space or tab contained within the selection,
        /// then all spaces and tabs in the selection are deleted.
        /// </para>
        /// </remarks>
        bool DeleteHorizontalWhiteSpace();

        /// <summary>
        /// Inserts a new line at the current caret position.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the edit succeeded, otherwise <c>false</c>.
        /// </returns>
        bool InsertNewLine();

        /// <summary>
        /// Inserts a new line at the start of the line the caret is on.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the edit succeeded, otherwise <c>false</c>.
        /// </returns>
        bool OpenLineAbove();

        /// <summary>
        /// Inserts a new line at the end of the line the caret is on.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the edit succeeded, otherwise <c>false</c>.
        /// </returns>
        bool OpenLineBelow();

        /// <summary>
        /// If there is a multi-line selection indents the selection, otherwise inserts a tab at the caret location.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the edit succeeded, otherwise <c>false</c>.
        /// </returns>
        bool Indent();

        /// <summary>
        /// If there is a multi-line selection, unindents the selection. If there is a single line selection,
        /// removes up to a tab's worth of white space from before the start of the selection. If there is no selection,
        /// removes up to a tab's worth of white space from before the caret position.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the edit succeeded, otherwise <c>false</c>.
        /// </returns>
        bool Unindent();

        /// <summary>
        /// If there is a multi-line selection, adds indentation to every line in the selection, 
        /// otherwise adds indentation to the line the caret is on.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the edit succeeded, otherwise <c>false</c>.
        /// </returns>
        bool IncreaseLineIndent();

        /// <summary>
        /// If there is a multi-line selection, removes indentation from every line in the selection, 
        /// otherwise removes indentation from the line the caret is on.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the edit succeeded, otherwise <c>false</c>.
        /// </returns>
        bool DecreaseLineIndent();

        /// <summary>
        /// Inserts the given text at the current caret position.
        /// </summary>
        /// <param name="text">
        /// The text to be inserted in the buffer.
        /// </param>
        /// <returns>
        /// <c>true</c> if the edit succeeded, otherwise <c>false</c>.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is null.</exception>
        bool InsertText(string text);

        /// <summary>
        /// Inserts the given text at the current caret position as a box.
        /// </summary>
        /// <param name="text">
        /// The text to be inserted in the buffer.  Each "line" from the text
        /// will be written out a line at a time.
        /// </param>
        /// <param name="boxStart">The start of the newly inserted box.</param>
        /// <param name="boxEnd">The end of the newly inserted box.</param>
        /// <returns>
        /// <c>true</c> if the edit succeeded, otherwise <c>false</c>.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is null.</exception>
        /// <remarks>
        /// This has the same behavior as copying and pasting a box selection.
        /// In order to insert the text as a box, the <paramref name="text" /> is
        /// split by newlines and inserted a line at a time, each one on a successive
        /// line below the line the caret is on (and starting at the caret's x coordinate
        /// on each line).
        /// </remarks>
        bool InsertTextAsBox(string text, out VirtualSnapshotPoint boxStart, out VirtualSnapshotPoint boxEnd);

        /// <summary>
        /// Inserts the given text at the current caret position as provisional text.
        /// </summary>
        /// <param name="text">
        /// The text to be inserted in the buffer.
        /// </param>
        /// <returns>
        /// <c>true</c> if the edit succeeded, otherwise <c>false</c>.
        /// </returns>
        /// <remarks>
        /// Provisional text is automatically replaced by subsequent InsertText() or InsertProvisionalText() calls.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is null.</exception>
        bool InsertProvisionalText(string text);

        /// <summary>
        /// Deletes the selection if there is one, or the next character in the buffer if one exists.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the edit succeeded, otherwise <c>false</c>.
        /// </returns>
        bool Delete();

        /// <summary>
        /// If there is a selection, deletes all the lines touched by the selection, including line break characters.
        /// Otherwise, deletes the line the caret is on, including the line break characters.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the edit succeeded, otherwise <c>false</c>.
        /// </returns>
        bool DeleteFullLine();

        /// <summary>
        /// Replaces the text selection with the new text.
        /// </summary>
        /// <param name="text">
        /// The new text that replaces the old selection.
        /// </param>
        /// <returns>
        /// <c>true</c> if the edit succeeded, otherwise <c>false</c>.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is null.</exception>
        bool ReplaceSelection(string text);

        /// <summary>
        /// Transposes the character at the cursor with the next character. 
        /// Transposes the first two characters when the cursor is at the start of the line. 
        /// Transposes the last two characters when the cursor is at the end of the line.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the edit succeeded, otherwise <c>false</c>.
        /// </returns>
        bool TransposeCharacter();

        /// <summary>
        /// Transposes the line containing the cursor with the next line. Transposes the last two lines when the cursor at the last line.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the edit succeeded, otherwise <c>false</c>.
        /// </returns>
        bool TransposeLine();

        /// <summary>
        /// Transposes the current word with the next one. White space and punctuation are not treated as words.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the edit succeeded, otherwise <c>false</c>.
        /// </returns>
        bool TransposeWord();

        /// <summary>
        /// Converts uppercase letters to lowercase in the selection. If the selection is empty, makes the next character lowercase.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the edit succeeded, otherwise <c>false</c>.
        /// </returns>
        bool MakeLowercase();

        /// <summary>
        /// Converts lowercase letters to uppercase in the selection. If the selection is empty, makes the next character uppercase.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the edit succeeded, otherwise <c>false</c>.
        /// </returns>
        bool MakeUppercase();

        /// <summary>
        /// Switches the case of each character in the selection. If the selection is empty, changes the case of the next character.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the edit succeeded, otherwise <c>false</c>.
        /// </returns>
        bool ToggleCase();

        /// <summary>
        /// Converts all the characters in the selection to lowercase, 
        /// then converts the first character in each word in the selection to uppercase.
        /// If the selection is empty, then it makes the next character uppercase.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the edit succeeded, otherwise <c>false</c>.
        /// </returns>
        bool Capitalize();

        /// <summary>
        /// Replaces text from the given span with the new text.
        /// </summary>
        /// <param name="replaceSpan">The span of text to be replaced.</param>
        /// <param name="text">
        /// The new text.
        /// </param>
        /// <returns>
        /// <c>true</c> if the edit succeeded, otherwise <c>false</c>.
        /// </returns>
        bool ReplaceText(Span replaceSpan, string text);

        /// <summary>
        /// Replaces all matching occurrences of the given string.
        /// </summary>
        /// <param name="searchText">
        /// The text to match.
        /// </param>
        /// <param name="replaceText">
        /// The replacement text.
        /// </param>
        /// <param name="matchCase">
        /// <c>true</c> if the search should match case, otherwise <c>false</c>.
        /// </param>
        /// <param name="matchWholeWord">
        /// <c>true</c> if the search should match whole words, otherwise <c>false</c>.
        /// </param>
        /// <param name="useRegularExpressions">
        /// <c>true</c> if the search should use regular expressions, otherwise <c>false</c>.
        /// </param>
        /// <returns>
        /// <c>true</c> if the edit succeeded, otherwise <c>false</c>.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="searchText"/> is null.</exception>
        /// <exception cref="ArgumentException"> if <paramref name="useRegularExpressions"/> is true and <paramref name="searchText"/> is an invalid regular expression.</exception>
        /// <returns>The number of matches found.</returns>
        /// <remarks>If any of the matches found is read only, none of the matches will be replaced.</remarks>
        int ReplaceAllMatches(string searchText, string replaceText, bool matchCase, bool matchWholeWord, bool useRegularExpressions);

        /// <summary>
        /// Inserts a file on disk into the text buffer.
        /// </summary>
        /// <param name="filePath">The path of the file on disk.</param>
        /// <returns>
        /// <c>true</c> if the edit succeeded, otherwise <c>false</c>.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="filePath"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="filePath"/> is a zero-length string, 
        /// contains only white space, or contains one or more invalid characters as defined by InvalidPathChars.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length. 
        /// For example, on Windows-based platforms, paths must be less than 248 characters, and file names must be less than 260 characters. </exception>
        /// <exception cref="DirectoryNotFoundException">The specified path is invalid (for example, it is on an unmapped drive). </exception>
        /// <exception cref="IOException">An I/O error occurred while opening the file. </exception>
        /// <exception cref="UnauthorizedAccessException"><paramref name="filePath"/> specified a file that is read-only, or
        /// this operation is not supported on the current platform, or
        /// <paramref name="filePath"/> specified a directory, or
        /// the caller does not have the required permission.</exception>
        /// <exception cref="FileNotFoundException">The file specified in <paramref name="filePath"/> was not found.</exception>
        /// <exception cref="NotSupportedException"><paramref name="filePath"/> is in an invalid format. </exception>
        /// <exception cref="System.Security.SecurityException">The caller does not have the required permission.</exception>
        bool InsertFile(string filePath);

        /// <summary>
        /// Converts the leading white space to tabs on all lines touched by the selection and caret.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the edit succeeded, otherwise <c>false</c>.
        /// </returns>
        /// <remarks>
        /// <para>
        /// If the column position of the first non-white space character is not evenly divisible by the tab size, there will be
        /// spaces left at the end of the line equal to the remainder of that division.
        /// </para>
        /// </remarks>
        bool Tabify();

        /// <summary>
        /// Converts the leading white space to spaces of all lines touched by the selection and caret.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the edit succeeded, otherwise <c>false</c>.
        /// </returns>
        bool Untabify();

        /// <summary>
        /// Converts spaces to tabs in the selection, or on the line the caret is on if the selection is empty.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the edit succeeded, otherwise <c>false</c>.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Only spaces immediately preceding a tab stop will be converted to tabs.
        /// </para>
        /// </remarks>
        bool ConvertSpacesToTabs();

        /// <summary>
        /// Converts tabs to spaces in the selection, or on the line the caret is on if the selection is empty.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the edit succeeded, otherwise <c>false</c>.
        /// </returns>
        /// <remarks>
        /// <para>
        /// A tab is replaced by the number of spaces equal to the distance between one tab and the next.
        /// </para>
        /// </remarks>
        bool ConvertTabsToSpaces();

        /// <summary>
        /// Replaces all line endings that do not match <paramref name="replacement"/> with <paramref name="replacement"/>.
        /// </summary>
        /// <param name="replacement">The character sequence that all line endings will match.</param>
        /// <returns>
        /// <c>true</c> if the edit succeeded, otherwise <c>false</c>.
        /// </returns>
        bool NormalizeLineEndings(string replacement);

        #endregion // Edit Operations

        #region Selection Operations

        /// <summary>
        /// Selects the current word.
        /// </summary>
        void SelectCurrentWord();

        /// <summary>
        /// Selects the enclosing parent.
        /// </summary>
        void SelectEnclosing();

        /// <summary>
        /// Selects the first child.
        /// </summary>
        void SelectFirstChild();

        /// <summary>
        /// Selects the next sibling.
        /// </summary>
        /// <param name="extendSelection">If <c>true</c>, the selection is extended when the caret is moved; if <c>false</c>, the selection is not extended.</param>
        void SelectNextSibling(bool extendSelection);

        /// <summary>
        /// Selects the previous sibling.
        /// </summary>
        /// <param name="extendSelection">If <c>true</c>, the selection is extended when the caret is moved; if <c>false</c>, the selection is not extended.</param>
        void SelectPreviousSibling(bool extendSelection);

        /// <summary>
        /// Selects the given line.
        /// </summary>
        /// <param name="viewLine">
        /// The line to select.
        /// </param>
        /// <param name="extendSelection">
        /// If <c>true</c>, the selection is extended when the caret is moved; if <c>false</c>, the selection is not extended.
        /// </param>
        /// <exception cref="ArgumentNullException"><paramref name="viewLine"/> is
        /// <c>null</c></exception>
        void SelectLine(ITextViewLine viewLine, bool extendSelection);

        /// <summary>
        /// Selects all text.
        /// </summary>
        void SelectAll();

        /// <summary>
        /// Extends the current selection span to the new selection end.
        /// </summary>
        /// <param name="newEnd">
        /// The new character position to which the selection is to be extended.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="newEnd"/> is less than 0.</exception>
        void ExtendSelection(int newEnd);

        /// <summary>
        /// Moves the caret to the given <paramref name="textLine"/> at the given <paramref name="horizontalOffset"/>.
        /// </summary>
        /// <param name="textLine">The <see cref="ITextViewLine"/> on which to place the caret.</param>
        /// <param name="horizontalOffset">The horizontal location in the given <paramref name="textLine"/> to which to move the caret.</param>
        /// <param name="extendSelection">
        /// If <c>true</c>, the selection is extended when the caret is moved; if <c>false</c>, the selection is not extended.
        /// </param>
        /// <exception cref="ArgumentNullException"><paramref name="textLine"/> is null.</exception>
        void MoveCaret(ITextViewLine textLine, double horizontalOffset, bool extendSelection);

        /// <summary>
        /// Resets any selection in the text.
        /// </summary>
        void ResetSelection();
        
        #endregion // Selection Operations

        #region Clipboard Operations

        /// <summary>
        /// Copies the selected text to the clipboard.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the clipboard operation succeeded, otherwise <c>false</c>.
        /// </returns>
        /// <exception cref="InsufficientMemoryException"> is thrown if there is not sufficient memory to complete the operation.</exception>
        bool CopySelection();

        /// <summary>
        /// Cuts the selected text.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the edit and the clipboard operation both succeeded, otherwise <c>false</c>.
        /// </returns>
        /// <exception cref="InsufficientMemoryException"> is thrown if there is not sufficient memory to complete the operation.</exception>
        bool CutSelection();

        /// <summary>
        /// Pastes text from the clipboard to the text buffer.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the edit and the clipboard operation both succeeded, otherwise <c>false</c>.
        /// </returns>
        bool Paste();

        /// <summary>
        /// If there is a selection present, deletes all lines touched by the selection,
        /// including line break characters, and copies the text to the clipboard.
        /// Otherwise, deletes the line the caret is on, including the line break characters, and copies the text to the clipboard.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the edit and the clipboard operation both succeeded, otherwise <c>false</c>.
        /// </returns>
        /// <exception cref="InsufficientMemoryException"> is thrown if there is not sufficient memory to complete the operation.</exception>
        bool CutFullLine();

        /// <summary>
        /// Determines whether a paste operation is possible.
        /// </summary>
        bool CanPaste
        {
            get;
        }

        /// <summary>
        /// Determines whether a delete operation is possible.
        /// </summary>
        bool CanDelete
        {
            get;
        }

        /// <summary>
        /// Determines whether  a cut operation is possible.
        /// </summary>
        bool CanCut
        {
            get;
        }

        #endregion // Clipboard Operations

        #region Scrolling Operations
        /// <summary>
        /// Scrolls the view up by one line and repositions the caret, 
        /// if it is scrolled off the page, to the last fully-visible
        /// line in the view.
        /// </summary>
        void ScrollUpAndMoveCaretIfNecessary();

        /// <summary>
        /// Scrolls the view down by one line and repositions the caret, 
        /// if it is scrolled off the page, to the first fully-visible
        /// line in the view.
        /// </summary>
        void ScrollDownAndMoveCaretIfNecessary();

        /// <summary>
        /// Scrolls the view up a page without moving the caret.
        /// </summary>
        void ScrollPageUp();

        /// <summary>
        /// Scrolls the view down a page without moving the caret.
        /// </summary>
        void ScrollPageDown();

        /// <summary>
        /// Scrolls the view one column to the left.
        /// </summary>
        /// <remarks>
        /// A column is the width of a space in the default font.
        /// </remarks>
        void ScrollColumnLeft();

        /// <summary>
        /// Scrolls the view one column to the right.
        /// </summary>
        /// <remarks>
        /// A column is the width of a space in the default font.
        /// </remarks>
        void ScrollColumnRight();

        /// <summary>
        /// Scrolls the line the caret is on, so that it is the last
        /// fully-visible line in the view.
        /// </summary>
        void ScrollLineBottom();

        /// <summary>
        /// Scroll sthe line the caret is on, so that it is the first
        /// fully-visible line in the view.
        /// </summary>
        void ScrollLineTop();

        /// <summary>
        /// Scrolls the line the caret is on, so that it is centered in the view.
        /// </summary>
        void ScrollLineCenter();

        /// <summary>
        /// Adds an <see cref="ITextUndoPrimitive"/> to the <see cref="ITextUndoHistory"/> for the buffer
        /// that will revert the selection to the current state when it is undone.
        /// </summary>
        /// <remarks>
        /// When performing edits that will change the selection, you can surround the edits with calls
        /// to <see cref="AddBeforeTextBufferChangePrimitive"/> and 
        /// <see cref="AddAfterTextBufferChangePrimitive"/> to ensure that the selection
        /// behaves correctly when the edits are undone and redone.
        /// </remarks>
        void AddBeforeTextBufferChangePrimitive();
        
        /// <summary>
        /// Adds an <see cref="ITextUndoPrimitive"/> to the <see cref="ITextUndoHistory"/> for the buffer
        /// that will revert the selection to the current state when it is redone.
        /// </summary>
        /// <remarks>
        /// When performing edits that will change the selection, you can surround the edits with calls
        /// to <see cref="AddBeforeTextBufferChangePrimitive"/> and 
        /// <see cref="AddAfterTextBufferChangePrimitive"/> to ensure that the selection
        /// behaves correctly when the edits are undone and redone.
        /// </remarks>
        void AddAfterTextBufferChangePrimitive();
        #endregion

        #region Zoom Operations
        /// <summary>
        /// Zooms in to the text view by a scaling factor of 10%
        /// </summary>
        /// <remarks>
        /// The maximum zooming scale is 400%
        /// </remarks>
        void ZoomIn();

        /// <summary>
        /// Zooms out of the text view by a scaling factor of 10%
        /// </summary>
        /// <remarks>
        /// The minimum zooming scale is 20%
        /// </remarks>
        void ZoomOut();

        /// <summary>
        /// Applies the given zoomLevel to the text view
        /// </summary>
        /// <param name="zoomLevel">The zoom level to apply between 20% to 400%</param>
        void ZoomTo(double zoomLevel);
        #endregion

        #region Miscellaneous

        /// <summary>
        /// Gets a string composed of whitespace characters that would be inserted to fill the gap between
        /// a given <see cref="VirtualSnapshotPoint"/> and the closest <see cref="SnapshotPoint"/> on the same line.
        /// </summary>
        /// <param name="point">The point in virtual space</param>
        /// <remarks>
        /// Returns an empty string if the provided <paramref name="point"/> is not in virtual space.
        /// </remarks>
        string GetWhitespaceForVirtualSpace(VirtualSnapshotPoint point);
        #endregion

        #region Properties

        /// <summary>
        /// Gets the text view on which these operations work.
        /// </summary>
        ITextView TextView
        {
            get;
        }

        /// <summary>
        /// Gets the options specific to this view.
        /// </summary>
        IEditorOptions Options
        {
            get;
        }

        /// <summary>
        /// Gets the span of the current provisional composition (null if there is no provisional composition).
        /// </summary>
        ITrackingSpan ProvisionalCompositionSpan { get; }

        /// <summary>
        /// Gets the selected text.
        /// </summary>
        /// <remarks>
        /// In box selection mode, this will have each span of text separated by a newline
        /// character, with an extra newline at the very end.
        /// </remarks>
        string SelectedText { get; }

        #endregion // Properties
    }
}
