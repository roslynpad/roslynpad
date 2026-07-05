namespace Microsoft.VisualStudio.Text.Editor.Commanding
{
    public static class CommandSelector
    {
        // Note: keep selectors in alphabetical order within their region groupings.
        // Provide the standard macOS default key bindings in a <summary> commment
        // for applicable selectors.
        //
        // ⎋ - Escape
        // ⌃ - Control
        // ⌥ - Option
        // ⌘ - Command
        // ⇧ - Shift

        #region NSStandardKeyBindingResponding

        /// <summary>⎋</summary>
        public const string CancelOperation = "cancelOperation:";

        /// <summary>⌃L</summary>
        public const string CenterSelectionInVisibleArea = "centerSelectionInVisibleArea:";

        /// <summary>⌥⎋</summary>
        public const string Complete = "complete:";

        public const string CycleToNextInputKeyboardLayout = "cycleToNextInputKeyboardLayout:";
        public const string CycleToNextInputScript = "cycleToNextInputScript:";

        public const string Delete = "delete:";

        /// <summary>⌫</summary>
        public const string DeleteBackward = "deleteBackward:";

        /// <summary>⌃⌫</summary>
        public const string DeleteBackwardByDecomposingPreviousCharacter = "deleteBackwardByDecomposingPreviousCharacter:";

        /// <summary>⌦</summary>
        public const string DeleteForward = "deleteForward:";

        /// <summary>⌘⌫</summary>
        public const string DeleteToBeginningOfLine = "deleteToBeginningOfLine:";

        /// <summary>⌃K</summary>
        public const string DeleteToEndOfParagraph = "deleteToEndOfParagraph:";

        /// <summary>⌥⌫</summary>
        public const string DeleteWordBackward = "deleteWordBackward:";

        /// <summary>⌥⌦</summary>
        public const string DeleteWordForward = "deleteWordForward:";

        /// <summary>⇤, ⇧↹</summary>
        public const string InsertBacktab = "insertBacktab:";

        /// <summary>⌃</summary>
        public const string InsertDoubleQuoteIgnoringSubstitution = "insertDoubleQuoteIgnoringSubstitution:";

        /// <summary>⌃↩</summary>
        public const string InsertLinereturn = "insertLinereturn:";

        /// <summary>↩</summary>
        public const string InsertNewline = "insertNewline:";

        /// <summary>⌃O</summary>
        public const string InsertNewlineIgnoringFieldEditor = "insertNewlineIgnoringFieldEditor:";

        public const string InsertRightToLeftSlash = "insertRightToLeftSlash:";

        /// <summary>⌃'</summary>
        public const string InsertSingleQuoteIgnoringSubstitution = "insertSingleQuoteIgnoringSubstitution:";

        /// <summary>⇥, ↹</summary>
        public const string InsertTab = "insertTab:";

        public const string InsertTabIgnoringFieldEditor = "insertTabIgnoringFieldEditor:";

        public const string MakeBaseWritingDirectionLeftToRight = "makeBaseWritingDirectionLeftToRight:";
        public const string MakeBaseWritingDirectionNatural = "makeBaseWritingDirectionNatural:";
        public const string MakeBaseWritingDirectionRightToLeft = "makeBaseWritingDirectionRightToLeft:";
        public const string MakeTextWritingDirectionLeftToRight = "makeTextWritingDirectionLeftToRight:";
        public const string MakeTextWritingDirectionNatural = "makeTextWritingDirectionNatural:";
        public const string MakeTextWritingDirectionRightToLeft = "makeTextWritingDirectionRightToLeft:";

        /// <summary>⌃B</summary>
        public const string MoveBackward = "moveBackward:";

        /// <summary>⇧⌃B</summary>
        public const string MoveBackwardAndModifySelection = "moveBackwardAndModifySelection:";

        /// <summary>⇣, ⌃N</summary>
        public const string MoveDown = "moveDown:";

        /// <summary>⇧⇣, ⇧⌃N</summary>
        public const string MoveDownAndModifySelection = "moveDownAndModifySelection:";

        /// <summary>⌃F</summary>
        public const string MoveForward = "moveForward:";

        /// <summary>⇧⌃F</summary>
        public const string MoveForwardAndModifySelection = "moveForwardAndModifySelection:";

        /// <summary>⇠</summary>
        public const string MoveLeft = "moveLeft:";

        /// <summary>⇧⇠</summary>
        public const string MoveLeftAndModifySelection = "moveLeftAndModifySelection:";

        /// <summary>⌥⇧⇡</summary>
        public const string MoveParagraphBackwardAndModifySelection = "moveParagraphBackwardAndModifySelection:";

        /// <summary>⌥⇧⇣</summary>
        public const string MoveParagraphForwardAndModifySelection = "moveParagraphForwardAndModifySelection:";

        /// <summary>⇢</summary>
        public const string MoveRight = "moveRight:";

        /// <summary>⇧⇢</summary>
        public const string MoveRightAndModifySelection = "moveRightAndModifySelection:";

        /// <summary>⌘⇡</summary>
        public const string MoveToBeginningOfDocument = "moveToBeginningOfDocument:";

        /// <summary>⇧⌘⇡</summary>
        public const string MoveToBeginningOfDocumentAndModifySelection = "moveToBeginningOfDocumentAndModifySelection:";

        /// <summary>⌃A</summary>
        public const string MoveToBeginningOfParagraph = "moveToBeginningOfParagraph:";

        /// <summary>⇧⌃A</summary>
        public const string MoveToBeginningOfParagraphAndModifySelection = "moveToBeginningOfParagraphAndModifySelection:";

        /// <summary>⌘⇣</summary>
        public const string MoveToEndOfDocument = "moveToEndOfDocument:";

        /// <summary>⇧⌘⇣</summary>
        public const string MoveToEndOfDocumentAndModifySelection = "moveToEndOfDocumentAndModifySelection:";

        /// <summary>⌃E</summary>
        public const string MoveToEndOfParagraph = "moveToEndOfParagraph:";

        /// <summary>⇧⌃E</summary>
        public const string MoveToEndOfParagraphAndModifySelection = "moveToEndOfParagraphAndModifySelection:";

        /// <summary>⌘⇠</summary>
        public const string MoveToLeftEndOfLine = "moveToLeftEndOfLine:";

        /// <summary>⇧⌘⇠</summary>
        public const string MoveToLeftEndOfLineAndModifySelection = "moveToLeftEndOfLineAndModifySelection:";

        /// <summary>⌘⇢</summary>
        public const string MoveToRightEndOfLine = "moveToRightEndOfLine:";

        /// <summary>⇧⌘⇢</summary>
        public const string MoveToRightEndOfLineAndModifySelection = "moveToRightEndOfLineAndModifySelection:";

        /// <summary>⇡, ⌃P</summary>
        public const string MoveUp = "moveUp:";

        /// <summary>⇧⇡, ⇧⌃P</summary>
        public const string MoveUpAndModifySelection = "moveUpAndModifySelection:";

        /// <summary>⌃⌥B</summary>
        public const string MoveWordBackward = "moveWordBackward:";

        /// <summary>⇧⌃⌥B</summary>
        public const string MoveWordBackwardAndModifySelection = "moveWordBackwardAndModifySelection:";

        /// <summary>⌃⌥F</summary>
        public const string MoveWordForward = "moveWordForward:";

        /// <summary>⇧⌃⌥F</summary>
        public const string MoveWordForwardAndModifySelection = "moveWordForwardAndModifySelection:";

        /// <summary>⌥⇠</summary>
        public const string MoveWordLeft = "moveWordLeft:";

        /// <summary>⇧⌥⇠</summary>
        public const string MoveWordLeftAndModifySelection = "moveWordLeftAndModifySelection:";

        /// <summary>⌥⇢</summary>
        public const string MoveWordRight = "moveWordRight:";

        /// <summary>⇧⌥⇢</summary>
        public const string MoveWordRightAndModifySelection = "moveWordRightAndModifySelection:";

        /// <summary>⌥fn⇣, ⌥⇟</summary>
        public const string PageDown = "pageDown:";

        /// <summary>⇧fn⇣, ⇧⇟</summary>
        public const string PageDownAndModifySelection = "pageDownAndModifySelection:";

        /// <summary>⌥fn⇡, ⌥⇞</summary>
        public const string PageUp = "pageUp:";

        /// <summary>⇧fn⇡, ⇧⇞</summary>
        public const string PageUpAndModifySelection = "pageUpAndModifySelection:";

        /// <summary>⌘F, ⌥⌘F, ⌘G, ⌥⌘G, ⌘E</summary>
        public const string PerformTextFinderAction = "performTextFinderAction:";

        /// <summary>fn⇡, ⇞</summary>
        public const string ScrollPageDown = "scrollPageDown:";

        /// <summary>fn⇣, ⇟</summary>
        public const string ScrollPageUp = "scrollPageUp:";

        /// <summary>fn⇠</summary>
        public const string ScrollToBeginningOfDocument = "scrollToBeginningOfDocument:";

        /// <summary>fn⇢</summary>
        public const string ScrollToEndOfDocument = "scrollToEndOfDocument:";

        public const string SelectNextKeyView = "selectNextKeyView:";

        public const string SelectPreviousKeyView = "selectPreviousKeyView:";

        public const string TogglePlatformInputSystem = "togglePlatformInputSystem:";

        /// <summary>⌃T</summary>
        public const string Transpose = "transpose:";

        public const string Yank = "yank:";

        #endregion

        #region Standard AppKit Selectors

        /// <summary>⌘C</summary>
        public const string Copy = "copy:";

        /// <summary>⌘X</summary>
        public const string Cut = "cut:";

        public const string Noop = "noop:";

        /// <summary>⌘V</summary>
        public const string Paste = "paste:";

        /// <summary>⇧⌘Z</summary>
        public const string Redo = "redo:";

        /// <summary>⌘A</summary>
        public const string SelectAll = "selectAll:";

        /// <summary>⌘Z</summary>
        public const string Undo = "undo:";

        #endregion

        #region Semi-standard AppKit Selectors

        /// <summary>⌘+</summary>
        public const string ZoomIn = "zoomIn:";

        /// <summary>⌘-</summary>
        public const string ZoomOut = "zoomOut:";

        /// <summary>⌘0</summary>
        public const string ZoomReset = "zoomReset:";

        #endregion

        #region VS Editor Specific Selectors

        public const string CommitUniqueCompletionListItem = "commitUniqueCompletionListItem:";

        public const string InvokeSignatureHelp = "invokeSignatureHelp:";

        public const string InvokeQuickInfo = "invokeQuickInfo:";

        public const string InvokeQuickFix = "invokeQuickFix:";

        public const string TransposeWord = "transposeWord:";

        public const string TransposeLine = "transposeLine:";

        /// <summary>⌘L</summary>
        public const string GoToLine = "goToLine:";

        public const string PerformFormatAction = "performFormatAction:";

        public const string ToggleOutliningEnabled = "toggleOutliningEnabled:";

        public const string ToggleOutliningExpansion = "toggleOutliningExpansion:";

        public const string ToggleAllOutlining = "toggleAllOutlining:";

        public const string ToggleOutliningDefinitions = "toggleOutliningDefinitions:";

        /// <summary>⇧⌥⇡</summary>
        public const string ExpandSelection = "expandSelection:";

        /// <summary>⇧⌥⇣</summary>
        public const string ContractSelection = "contractSelection:";

        /// <summary>⌥⇡</summary>
        public const string NavigateToPreviousIssueInDocument = "navigateToPreviousIssueInDocument:";

        /// <summary>⌥⇣</summary>
        public const string NavigateToNextIssueInDocument = "navigateToNextIssueInDocument:";

        /// <summary>⌘⌥⇡</summary>
        public const string NavigateToPreviousErrorInDocument = "navigateToPreviousErrorInDocument:";

        /// <summary>⌘⌥⇣</summary>
        public const string NavigateToNextErrorInDocument = "navigateToNextErrorInDocument:";

        #endregion
    }

    public enum TextFinderAction
    {
        ShowFindInterface = 1,
        NextMatch,
        PreviousMatch,
        ReplaceAll,
        Replace,
        ReplaceAndFind,
        SetSearchString,
        ReplaceAllInSelection,
        SelectAll,
        SelectAllInSelection,
        HideFindInterface,
        ShowReplaceInterface,
        HideReplaceInterface
    }

    public enum FormatAction
    {
        SortSelectedLines = 1,
        JoinSelectedLines
    }
}