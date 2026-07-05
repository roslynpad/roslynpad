//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
namespace Microsoft.VisualStudio.Text.Operations.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Composition;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Avalonia;

    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;
    using Microsoft.VisualStudio.Text.Formatting;
    using Microsoft.VisualStudio.Utilities;
    using Microsoft.VisualStudio.Text.Outlining;
    using Microsoft.VisualStudio.Text.Tagging;
    using Microsoft.VisualStudio.Language.Intellisense.Utilities;
    using Microsoft.VisualStudio.Text.Utilities;
    using Microsoft.VisualStudio.Text.Data.Utilities;

    /// <summary>
    /// Provides a default operations set on top of the text editor
    /// </summary>
    public class EditorOperations : IEditorOperations4
    {
        enum CaretMovementDirection
        {
            Previous = 0,
            Next = 1,
        }

        enum LetterCase
        {
            Uppercase = 0,
            Lowercase = 1,
        }

        enum SelectionUpdate
        {
            Preserve,
            Reset,
            ResetUnlessEmptyBox,
            Ignore,
            ClearVirtualSpace
        };

        #region Private Members

        readonly ITextView _textView;
        readonly EditorOperationsFactoryService _factory;
        readonly ITextDocument _textDocument;
        readonly ITextStructureNavigator _textStructureNavigator;
        readonly ITextUndoHistory _undoHistory;
        readonly IViewPrimitives _editorPrimitives;
        readonly IEditorOptions _editorOptions;
        readonly IMultiSelectionBroker _multiSelectionBroker;

        private ITrackingSpan _immProvisionalComposition;

        /// <summary>
        /// A data format used to tag the contents of the clipboard so that it's clear
        /// the data has been put in the clipboard by our editor
        /// </summary>
        private const string _clipboardLineBasedCutCopyTag = "VisualStudioEditorOperationsLineCutCopyClipboardTag";

        /// <summary>
        /// A data format used to tag the contents of the clipboard as a box selection.
        /// This is the same string that was used in VS9 and previous versions.
        /// </summary>
        private const string _boxSelectionCutCopyTag = "MSDEVColumnSelect";

        #endregion // Private Members

        /// <summary>
        /// Constructs an <see cref="EditorOperations"/> bound to a given <see cref="ITextView"/>.
        /// </summary>
        /// <param name="textView">
        /// The text editor to which this operations provider should bind to
        /// </param>
        internal EditorOperations(ITextView textView,
                                  EditorOperationsFactoryService factory)
        {
            // Validate
            if (textView == null)
                throw new ArgumentNullException(nameof(textView));
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            _textView = textView;
            _factory = factory;
            _multiSelectionBroker = _textView.GetMultiSelectionBroker();
            _editorPrimitives = factory.EditorPrimitivesProvider.GetViewPrimitives(textView);
            // Get the TextStructure Navigator
            _textStructureNavigator = factory.TextStructureNavigatorFactory.GetTextStructureNavigator(_textView.TextBuffer);
            Debug.Assert(_textStructureNavigator != null);

            _undoHistory = factory.UndoHistoryRegistry.RegisterHistory(_textView.TextBuffer);
            // Ensure that there is an ITextBufferUndoManager created for our TextBuffer
            ITextBufferUndoManager textBufferUndoManager = factory.TextBufferUndoManagerProvider.GetTextBufferUndoManager(_textView.TextBuffer);
            Debug.Assert(textBufferUndoManager != null);

            _editorOptions = factory.EditorOptionsProvider.GetOptions(textView);

            _factory.TextDocumentFactoryService.TryGetTextDocument(_textView.TextDataModel.DocumentBuffer, out _textDocument);

            _textView.Closed += delegate
            {
                _factory.UndoHistoryRegistry.RemoveHistory(_undoHistory);
                _factory.TextBufferUndoManagerProvider.RemoveTextBufferUndoManager(_textView.TextBuffer);
            };
        }


        #region IEditorOperations2 Members

        public bool MoveSelectedLinesUp()
        {
            Func<bool> action = () =>
            {
                bool success = false;

                // find line start
                ITextViewLine startViewLine = GetLineStart(_textView, _textView.Selection.Start.Position);
                SnapshotPoint start = startViewLine.Start;
                ITextSnapshotLine startLine = start.GetContainingLine();

                // find the last line view
                ITextViewLine endViewLine = GetLineEnd(_textView, _textView.Selection.End.Position);
                SnapshotPoint end = endViewLine.EndIncludingLineBreak;
                ITextSnapshotLine endLine = endViewLine.End.GetContainingLine();

                ITextSnapshot snapshot = endLine.Snapshot;

                // Handle the case where multiple lines are selected and the caret is sitting just after the line break on the next line.
                // Shortening the selection here handles the case where the last line is a collapsed region. Using endLine.End will give
                // a line within the collapsed region instead of skipping it all together. 
                if (GetLineEnd(_textView, startViewLine.Start) != endViewLine
                    && _textView.Selection.End.Position == GetLineStart(_textView, _textView.Selection.End.Position).Start
                    && !_textView.Selection.End.IsInVirtualSpace)
                {
                    endLine = snapshot.GetLineFromLineNumber(endLine.LineNumber - 1);
                    end = endLine.EndIncludingLineBreak;
                    endViewLine = _textView.GetTextViewLineContainingBufferPosition(_textView.Selection.End.Position - 1);
                }

                #region Initial Asserts

                Debug.Assert(_textView.Selection.Start.Position.Snapshot == _textView.TextSnapshot, "Selection is out of sync with view.");

                Debug.Assert(_textView.TextSnapshot == _textView.TextBuffer.CurrentSnapshot, "View is out of sync with text buffer.");

                Debug.Assert(_textView.TextSnapshot == snapshot, "Text view lines are out of sync with the view");

                #endregion

                // check if we are at the top of the file, or trying to move a blank line
                if (startLine.LineNumber < 1 || start == end)
                {
                    // noop 
                    success = true;
                }
                else
                {
                    // find the line we are going to move over
                    ITextSnapshotLine prevLine = snapshot.GetLineFromLineNumber(startLine.LineNumber - 1);

                    // prevLineExtent is different from prevLine.Extent and avoids issues around collapsed regions
                    SnapshotPoint prevLineStart = GetLineStart(_textView, prevLine.Start).Start;
                    SnapshotSpan prevLineExtent = new SnapshotSpan(prevLineStart, prevLine.End);
                    SnapshotSpan prevLineExtentIncludingLineBreak = new SnapshotSpan(prevLineStart, prevLine.EndIncludingLineBreak);

                    using (ITextEdit edit = _textView.TextBuffer.CreateEdit())
                    {
                        int offset;

                        SnapshotSpan curLineExtent = new SnapshotSpan(startViewLine.Start, endViewLine.End);
                        SnapshotSpan curLineExtentIncLineBreak = new SnapshotSpan(startViewLine.Start, endViewLine.EndIncludingLineBreak);
                        string curLineText = curLineExtentIncLineBreak.GetText();

                        List<Tuple<Span, IOutliningRegionTag>> collapsedSpansInCurLine = null;
                        bool hasCollapsedRegions = false;

                        IOutliningManager outliningManager = (_factory.OutliningManagerService != null)
                                                             ? _factory.OutliningManagerService.GetOutliningManager(_textView)
                                                             : null;

                        if (outliningManager != null)
                        {
                            collapsedSpansInCurLine = outliningManager.GetCollapsedRegions(new NormalizedSnapshotSpanCollection(curLineExtent))
                                .Select(collapsed => Tuple.Create(collapsed.Extent.GetSpan(curLineExtent.Snapshot).Span, collapsed.Tag)).ToList();

                            hasCollapsedRegions = collapsedSpansInCurLine.Count > 0;

                            // check if we have collapsed spans in the selection and add the undo primitive if so
                            if (hasCollapsedRegions)
                            {
                                using (ITextUndoTransaction undoTransaction = _undoHistory.CreateTransaction(Strings.MoveSelLinesUp))
                                {
                                    BeforeCollapsedMoveUndoPrimitive undoPrim = new BeforeCollapsedMoveUndoPrimitive(outliningManager, _textView, collapsedSpansInCurLine);
                                    undoTransaction.AddUndo(undoPrim);
                                    undoTransaction.Complete();
                                }
                            }
                        }

                        string nextLineText = prevLineExtentIncludingLineBreak.GetText();

                        offset = nextLineText.Length;

                        // make the change
                        edit.Delete(curLineExtentIncLineBreak);
                        edit.Insert(prevLineExtentIncludingLineBreak.Start, curLineText);

                        // swap the line break around if needed for the last line of the selection                    
                        if (endViewLine.LineBreakLength == 0 && endViewLine.EndIncludingLineBreak == snapshot.Length)
                        {
                            // put the line break on the line we just moved that didn't have one
                            edit.Insert(prevLine.ExtentIncludingLineBreak.Start, prevLine.GetLineBreakText());

                            // delete the break from the line now at the end of the file
                            edit.Delete(new SnapshotSpan(prevLine.End, prevLine.EndIncludingLineBreak));
                        }


                        if (!edit.HasFailedChanges)
                        {
                            // store the position before the edit is applied
                            int anchorPos = _textView.Selection.AnchorPoint.Position.Position;
                            int anchorVirtualSpace = _textView.Selection.AnchorPoint.VirtualSpaces;
                            int activePos = _textView.Selection.ActivePoint.Position.Position;
                            int activeVirtualSpace = _textView.Selection.ActivePoint.VirtualSpaces;
                            var selectionMode = _textView.Selection.Mode;

                            // apply the edit
                            ITextSnapshot newSnapshot = edit.Apply();

                            if (newSnapshot != snapshot)
                            {

                                // Update the selection and caret position after the move
                                ITextSnapshot currentSnapshot = snapshot.TextBuffer.CurrentSnapshot;
                                VirtualSnapshotPoint desiredAnchor = new VirtualSnapshotPoint(
                                    new SnapshotPoint(newSnapshot, Math.Min(anchorPos - offset, newSnapshot.Length)), anchorVirtualSpace)
                                    .TranslateTo(currentSnapshot, PointTrackingMode.Negative);
                                VirtualSnapshotPoint desiredActive = new VirtualSnapshotPoint(
                                    new SnapshotPoint(newSnapshot, Math.Min(activePos - offset, newSnapshot.Length)), activeVirtualSpace)
                                    .TranslateTo(currentSnapshot, PointTrackingMode.Negative);

                                // Keep the selection and caret position the same
                                SelectAndMoveCaret(desiredAnchor, desiredActive, selectionMode, EnsureSpanVisibleOptions.None);

                                // Recollapse the spans
                                if (outliningManager != null && hasCollapsedRegions)
                                {
                                    // This comes from adhocoutliner.cs in env\editor\pkg\impl\outlining and will not be available outside of VS
                                    SimpleTagger<IOutliningRegionTag> simpleTagger =
                                        _textView.TextBuffer.Properties.GetOrCreateSingletonProperty<SimpleTagger<IOutliningRegionTag>>(
                                        () => new SimpleTagger<IOutliningRegionTag>(_textView.TextBuffer));

                                    if (simpleTagger != null)
                                    {
                                        if (hasCollapsedRegions)
                                        {
                                            List<Tuple<ITrackingSpan, IOutliningRegionTag>> addedSpans = collapsedSpansInCurLine.Select(tuple => Tuple.Create(newSnapshot.CreateTrackingSpan(tuple.Item1.Start - offset, tuple.Item1.Length,
                                                SpanTrackingMode.EdgeExclusive), tuple.Item2)).ToList();

                                            if (addedSpans.Count > 0)
                                            {
                                                List<Tuple<Span, IOutliningRegionTag>> spansForUndo = new List<Tuple<Span, IOutliningRegionTag>>();

                                                foreach (var addedSpan in addedSpans)
                                                {
                                                    simpleTagger.CreateTagSpan(addedSpan.Item1, addedSpan.Item2);
                                                    spansForUndo.Add(new Tuple<Span, IOutliningRegionTag>(addedSpan.Item1.GetSpan(newSnapshot), addedSpan.Item2));
                                                }

                                                SnapshotSpan changedSpan = new SnapshotSpan(addedSpans.Select(tuple => tuple.Item1.GetSpan(newSnapshot).Start).Min(),
                                                    addedSpans.Select(tuple => tuple.Item1.GetSpan(newSnapshot).End).Max());

                                                List<SnapshotSpan> addedSnapshotSpans = addedSpans.Select(tuple => tuple.Item1.GetSpan(newSnapshot)).ToList();


                                                bool disableOutliningUndo = _editorOptions.IsOutliningUndoEnabled();

                                                // Recollapse the spans
                                                // We need to disable the OutliningUndoManager for this operation otherwise an undo will expand it
                                                try
                                                {
                                                    if (disableOutliningUndo)
                                                    {
                                                        _textView.Options.SetOptionValue(DefaultTextViewOptions.OutliningUndoOptionId, false);
                                                    }

                                                    outliningManager.CollapseAll(changedSpan, collapsible => addedSnapshotSpans.Contains(collapsible.Extent.GetSpan(newSnapshot)));
                                                }
                                                finally
                                                {
                                                    if (disableOutliningUndo)
                                                    {
                                                        _textView.Options.SetOptionValue(DefaultTextViewOptions.OutliningUndoOptionId, true);
                                                    }
                                                }

                                                // we need to recollapse after a redo
                                                using (ITextUndoTransaction undoTransaction = _undoHistory.CreateTransaction(Strings.MoveSelLinesUp))
                                                {
                                                    AfterCollapsedMoveUndoPrimitive undoPrim = new AfterCollapsedMoveUndoPrimitive(outliningManager, _textView, spansForUndo);
                                                    undoTransaction.AddUndo(undoPrim);
                                                    undoTransaction.Complete();
                                                }
                                            }
                                        }
                                    }
                                }

                                success = true;
                            }
                        }
                    }
                }

                return success;
            };

            return ExecuteAction(Strings.MoveSelLinesUp, action, SelectionUpdate.Ignore, true);
        }

        public bool MoveSelectedLinesDown()
        {
            Func<bool> action = () =>
            {
                bool success = false;

                // find line start
                ITextViewLine startViewLine = GetLineStart(_textView, _textView.Selection.Start.Position);
                SnapshotPoint start = startViewLine.Start;
                ITextSnapshotLine startLine = start.GetContainingLine();

                // find the last line view
                ITextViewLine endViewLine = GetLineEnd(_textView, _textView.Selection.End.Position);
                ITextSnapshotLine endLine = endViewLine.End.GetContainingLine();

                ITextSnapshot snapshot = endLine.Snapshot;

                // Handle the case where multiple lines are selected and the caret is sitting just after the line break on the next line.
                // Shortening the selection here handles the case where the last line is a collapsed region. Using endLine.End will give
                // a line within the collapsed region instead of skipping it all together. 
                if (GetLineEnd(_textView, startViewLine.Start) != endViewLine
                    && _textView.Selection.End.Position == GetLineStart(_textView, _textView.Selection.End.Position).Start
                    && !_textView.Selection.End.IsInVirtualSpace)
                {
                    endLine = snapshot.GetLineFromLineNumber(endLine.LineNumber - 1);
                    endViewLine = _textView.GetTextViewLineContainingBufferPosition(_textView.Selection.End.Position - 1);
                }

                #region Initial Asserts

                Debug.Assert(_textView.Selection.Start.Position.Snapshot == _textView.TextSnapshot, "Selection is out of sync with view.");

                Debug.Assert(_textView.TextSnapshot == _textView.TextBuffer.CurrentSnapshot, "View is out of sync with text buffer.");

                Debug.Assert(_textView.TextSnapshot == snapshot, "Text view lines are out of sync with the view");

                #endregion

                // check if we are at the end of the file
                if ((endLine.LineNumber + 1) >= snapshot.LineCount)
                {
                    // noop
                    success = true;
                }
                else
                {
                    // nextLineExtent is different from prevLine.Extent and avoids issues around collapsed regions
                    ITextViewLine lastNextLine = GetLineEnd(_textView, endViewLine.EndIncludingLineBreak);
                    SnapshotSpan nextLineExtent = new SnapshotSpan(endViewLine.EndIncludingLineBreak, lastNextLine.End);
                    SnapshotSpan nextLineExtentIncludingLineBreak = new SnapshotSpan(endViewLine.EndIncludingLineBreak, lastNextLine.EndIncludingLineBreak);

                    using (ITextEdit edit = _textView.TextBuffer.CreateEdit())
                    {
                        SnapshotSpan curLineExtent = new SnapshotSpan(startViewLine.Start, endViewLine.End);
                        SnapshotSpan curLineExtentIncLineBreak = new SnapshotSpan(startViewLine.Start, endViewLine.EndIncludingLineBreak);
                        string curLineText = curLineExtentIncLineBreak.GetText();

                        string nextLineText = nextLineExtentIncludingLineBreak.GetText();

                        if (nextLineText.Length == 0)
                        {
                            // end of file - noop
                            success = true;
                        }
                        else
                        {
                            List<Tuple<Span, IOutliningRegionTag>> collapsedSpansInCurLine = null;
                            bool hasCollapsedRegions = false;

                            IOutliningManager outliningManager = (_factory.OutliningManagerService != null)
                                                                    ? _factory.OutliningManagerService.GetOutliningManager(_textView)
                                                                    : null;

                            if (outliningManager != null)
                            {
                                collapsedSpansInCurLine = outliningManager.GetCollapsedRegions(new NormalizedSnapshotSpanCollection(curLineExtent))
                                    .Select(collapsed => Tuple.Create(collapsed.Extent.GetSpan(curLineExtent.Snapshot).Span, collapsed.Tag)).ToList();

                                hasCollapsedRegions = collapsedSpansInCurLine.Count > 0;

                                // check if we have collapsed spans in the selection and add the undo primitive if so
                                if (hasCollapsedRegions)
                                {
                                    using (ITextUndoTransaction undoTransaction = _undoHistory.CreateTransaction(Strings.MoveSelLinesDown))
                                    {
                                        BeforeCollapsedMoveUndoPrimitive undoPrim = new BeforeCollapsedMoveUndoPrimitive(outliningManager, _textView, collapsedSpansInCurLine);
                                        undoTransaction.AddUndo(undoPrim);
                                        undoTransaction.Complete();
                                    }
                                }
                            }


                            int offset = nextLineText.Length;

                            // a line without a line break
                            if (nextLineExtent == nextLineExtentIncludingLineBreak)
                            {
                                string lineBreakText = new SnapshotSpan(startLine.End, startLine.EndIncludingLineBreak).GetText();

                                offset += lineBreakText.Length;

                                curLineText = lineBreakText + curLineText.Substring(0, curLineText.Length - lineBreakText.Length);
                            }


                            edit.Delete(curLineExtentIncLineBreak);
                            edit.Insert(nextLineExtentIncludingLineBreak.End, curLineText);

                            if (edit.HasFailedChanges)
                            {
                                success = false;
                            }
                            else
                            {
                                int anchorPos = _textView.Selection.AnchorPoint.Position.Position;
                                int anchorVirtualSpace = _textView.Selection.AnchorPoint.VirtualSpaces;
                                int activePos = _textView.Selection.ActivePoint.Position.Position;
                                int activeVirtualSpace = _textView.Selection.ActivePoint.VirtualSpaces;
                                var selectionMode = _textView.Selection.Mode;

                                ITextSnapshot newSnapshot = edit.Apply();
                                if (newSnapshot == snapshot)
                                {
                                    success = false;
                                }
                                else
                                {
                                    // Update the selection and caret position after the move
                                    ITextSnapshot currentSnapshot = snapshot.TextBuffer.CurrentSnapshot;
                                    VirtualSnapshotPoint desiredAnchor = new VirtualSnapshotPoint(new SnapshotPoint(newSnapshot, Math.Min(anchorPos + offset, newSnapshot.Length)),
                                        anchorVirtualSpace).TranslateTo(currentSnapshot, PointTrackingMode.Negative);
                                    VirtualSnapshotPoint desiredActive = new VirtualSnapshotPoint(new SnapshotPoint(newSnapshot, Math.Min(activePos + offset, newSnapshot.Length)),
                                        activeVirtualSpace).TranslateTo(currentSnapshot, PointTrackingMode.Negative);

                                    // keep the caret position and selection after the move
                                    SelectAndMoveCaret(desiredAnchor, desiredActive, selectionMode, EnsureSpanVisibleOptions.None);

                                    // Recollapse the spans
                                    if (outliningManager != null && hasCollapsedRegions)
                                    {
                                        // This comes from adhocoutliner.cs in env\editor\pkg\impl\outlining and will not be available outside of VS
                                        SimpleTagger<IOutliningRegionTag> simpleTagger =
                                            _textView.TextBuffer.Properties.GetOrCreateSingletonProperty<SimpleTagger<IOutliningRegionTag>>(() => new SimpleTagger<IOutliningRegionTag>(_textView.TextBuffer));

                                        if (simpleTagger != null)
                                        {
                                            if (hasCollapsedRegions)
                                            {
                                                List<Tuple<ITrackingSpan, IOutliningRegionTag>> addedSpans = collapsedSpansInCurLine.Select(tuple => Tuple.Create(newSnapshot.CreateTrackingSpan(tuple.Item1.Start + offset,
                                                    tuple.Item1.Length, SpanTrackingMode.EdgeExclusive), tuple.Item2)).ToList();

                                                if (addedSpans.Count > 0)
                                                {
                                                    List<Tuple<Span, IOutliningRegionTag>> spansForUndo = new List<Tuple<Span, IOutliningRegionTag>>();

                                                    // add spans to tracking
                                                    foreach (var addedSpan in addedSpans)
                                                    {
                                                        simpleTagger.CreateTagSpan(addedSpan.Item1, addedSpan.Item2);
                                                        spansForUndo.Add(new Tuple<Span, IOutliningRegionTag>(addedSpan.Item1.GetSpan(newSnapshot), addedSpan.Item2));
                                                    }

                                                    SnapshotSpan changedSpan = new SnapshotSpan(addedSpans.Select(tuple => tuple.Item1.GetSpan(newSnapshot).Start).Min(),
                                                        addedSpans.Select(tuple => tuple.Item1.GetSpan(newSnapshot).End).Max());

                                                    List<SnapshotSpan> addedSnapshotSpans = addedSpans.Select(tuple => tuple.Item1.GetSpan(newSnapshot)).ToList();

                                                    bool disableOutliningUndo = _editorOptions.IsOutliningUndoEnabled();

                                                    // Recollapse the span
                                                    // We need to disable the OutliningUndoManager for this operation otherwise an undo will expand it
                                                    try
                                                    {
                                                        if (disableOutliningUndo)
                                                        {
                                                            _textView.Options.SetOptionValue(DefaultTextViewOptions.OutliningUndoOptionId, false);
                                                        }

                                                        outliningManager.CollapseAll(changedSpan, collapsible => addedSnapshotSpans.Contains(collapsible.Extent.GetSpan(newSnapshot)));
                                                    }
                                                    finally
                                                    {
                                                        if (disableOutliningUndo)
                                                        {
                                                            _textView.Options.SetOptionValue(DefaultTextViewOptions.OutliningUndoOptionId, true);
                                                        }
                                                    }

                                                    // we need to recollapse after a redo
                                                    using (ITextUndoTransaction undoTransaction = _undoHistory.CreateTransaction(Strings.MoveSelLinesDown))
                                                    {
                                                        AfterCollapsedMoveUndoPrimitive undoPrim = new AfterCollapsedMoveUndoPrimitive(outliningManager, _textView, spansForUndo);
                                                        undoTransaction.AddUndo(undoPrim);
                                                        undoTransaction.Complete();
                                                    }
                                                }
                                            }
                                        }
                                    }

                                    success = true;
                                }
                            }
                        }
                    }
                }
                return success;
            };

            return ExecuteAction(Strings.MoveSelLinesDown, action, SelectionUpdate.Ignore, true);
        }

        private static ITextViewLine GetLineStart(ITextView view, SnapshotPoint snapshotPoint)
        {
            ITextViewLine line = view.GetTextViewLineContainingBufferPosition(snapshotPoint);
            while (!line.IsFirstTextViewLineForSnapshotLine)
            {
                line = view.GetTextViewLineContainingBufferPosition(line.Start - 1);
            }
            return line;
        }

        private static ITextViewLine GetLineEnd(ITextView view, SnapshotPoint snapshotPoint)
        {
            ITextViewLine line = view.GetTextViewLineContainingBufferPosition(snapshotPoint);
            while (!line.IsLastTextViewLineForSnapshotLine)
            {
                line = view.GetTextViewLineContainingBufferPosition(line.EndIncludingLineBreak);
            }
            return line;
        }

        #endregion


        #region IEditorOperations Members

        public void SelectAndMoveCaret(VirtualSnapshotPoint anchorPoint, VirtualSnapshotPoint activePoint)
        {
            SelectAndMoveCaret(anchorPoint, activePoint, TextSelectionMode.Stream, EnsureSpanVisibleOptions.MinimumScroll);
        }

        public void SelectAndMoveCaret(VirtualSnapshotPoint anchorPoint, VirtualSnapshotPoint activePoint, TextSelectionMode selectionMode)
        {
            this.SelectAndMoveCaret(anchorPoint, activePoint, selectionMode, EnsureSpanVisibleOptions.MinimumScroll);
        }

        public void SelectAndMoveCaret(VirtualSnapshotPoint anchorPoint, VirtualSnapshotPoint activePoint, TextSelectionMode selectionMode, EnsureSpanVisibleOptions? scrollOptions)
        {
            bool empty = (anchorPoint == activePoint);

            var selection = new Selection(anchorPoint, activePoint);
            if (selectionMode == TextSelectionMode.Box)
            {
                _multiSelectionBroker.SetBoxSelection(selection);
            }
            else
            {
                _multiSelectionBroker.SetSelection(selection);
            }

            // 3) If scrollOptions were provided, we're going to try and make the span visible using the provided options.
            if (scrollOptions.HasValue)
            {
                //Make sure scrollOptions forces EnsureSpanVisible to bring the start or end of the selection into view appropriately.
                if (_textView.Selection.IsReversed)
                {
                    scrollOptions = scrollOptions.Value | EnsureSpanVisibleOptions.ShowStart;
                }
                else
                {
                    scrollOptions = scrollOptions.Value & (~EnsureSpanVisibleOptions.ShowStart);
                }

                // Try to make the span visible. Since we are setting the scrollOptions above, this will ensure the caret
                // is visible as well (we do not need to worry about the case where the caret is at the end of a word-wrapped
                // line since -- when the caret is moved to a VirtualSnapshotPoint -- it won't be).
                _textView.ViewScroller.EnsureSpanVisible(_textView.Selection.StreamSelectionSpan, scrollOptions.Value);
            }
        }

        /// <summary>
        /// Moves one character to the right.
        /// </summary>
        /// <param name="select">
        /// Specifies whether selection is made as the caret is moved.
        /// </param>
        public void MoveToNextCharacter(bool select)
        {
            _multiSelectionBroker.PerformActionOnAllSelections(select ? PredefinedSelectionTransformations.SelectToNextCaretPosition : PredefinedSelectionTransformations.MoveToNextCaretPosition);
            _textView.Caret.EnsureVisible();
        }

        /// <summary>
        /// Moves one character to the left.
        /// </summary>
        /// <param name="select">
        /// Specifies whether selection is made as the caret is moved.
        /// </param>
        public void MoveToPreviousCharacter(bool select)
        {
            _multiSelectionBroker.PerformActionOnAllSelections(select ? PredefinedSelectionTransformations.SelectToPreviousCaretPosition : PredefinedSelectionTransformations.MoveToPreviousCaretPosition);
            _textView.Caret.EnsureVisible();
        }

        /// <summary>
        /// Moves the caret to the next word.
        /// </summary>
        /// <param name="select">
        /// Specifies whether or not selection is extended as the caret is moved.
        /// </param>
        public void MoveToNextWord(bool select)
        {
            _multiSelectionBroker.PerformActionOnAllSelections(select ? PredefinedSelectionTransformations.SelectToNextWord : PredefinedSelectionTransformations.MoveToNextWord);
            _textView.Caret.EnsureVisible();
        }

        /// <summary>
        /// Moves the caret to the previous word.
        /// </summary>
        /// <param name="select">
        /// Specifies whether or not selection is extended as the caret is moved.
        /// </param>
        public void MoveToPreviousWord(bool select)
        {
            _multiSelectionBroker.PerformActionOnAllSelections(select ? PredefinedSelectionTransformations.SelectToPreviousWord : PredefinedSelectionTransformations.MoveToPreviousWord);
            _textView.Caret.EnsureVisible();
        }

        /// <summary>
        /// Sets the caret at the start of the document.
        /// </summary>
        /// <param name="select">
        /// Specifies whether selection is made as the caret is moved.
        /// </param>
        public void MoveToStartOfDocument(bool select)
        {
            _multiSelectionBroker.PerformActionOnAllSelections(select ? PredefinedSelectionTransformations.SelectToStartOfDocument : PredefinedSelectionTransformations.MoveToStartOfDocument);
            _textView.Caret.EnsureVisible();
        }

        /// <summary>
        /// Sets the caret at the end of the document.
        /// </summary>
        /// <param name="select">
        /// Specifies whether selection is made as the caret is moved.
        /// </param>
        public void MoveToEndOfDocument(bool select)
        {
            _multiSelectionBroker.PerformActionOnAllSelections(select ? PredefinedSelectionTransformations.SelectToEndOfDocument : PredefinedSelectionTransformations.MoveToEndOfDocument);
            _textView.Caret.EnsureVisible();
        }

        /// <summary>
        /// Moves the current line to the top of the view.
        /// </summary>
        public void MoveCurrentLineToTop()
        {
            _editorPrimitives.View.MoveLineToTop(_editorPrimitives.Caret.LineNumber);
        }

        /// <summary>
        /// Moves the current line to the bottom of the view.
        /// </summary>
        public void MoveCurrentLineToBottom()
        {
            _editorPrimitives.View.MoveLineToBottom(_editorPrimitives.Caret.LineNumber);
        }

        public void MoveToStartOfLineAfterWhiteSpace(bool select)
        {
            int firstTextColumn = _editorPrimitives.Caret.GetFirstNonWhiteSpaceCharacterOnViewLine().CurrentPosition;
            if (firstTextColumn == _editorPrimitives.Caret.EndOfViewLine)
                firstTextColumn = _editorPrimitives.Caret.StartOfViewLine;

            _editorPrimitives.Caret.MoveTo(firstTextColumn, select);
        }

        public void MoveToStartOfNextLineAfterWhiteSpace(bool select)
        {
            DisplayTextPoint caretPoint = _editorPrimitives.Caret.Clone();
            caretPoint.MoveToBeginningOfNextLine();

            int firstTextColumn = caretPoint.GetFirstNonWhiteSpaceCharacterOnViewLine().CurrentPosition;
            if (firstTextColumn == caretPoint.EndOfViewLine)
                firstTextColumn = caretPoint.StartOfViewLine;

            _editorPrimitives.Caret.MoveTo(firstTextColumn, select);
        }

        public void MoveToStartOfPreviousLineAfterWhiteSpace(bool select)
        {
            DisplayTextPoint caretPoint = _editorPrimitives.Caret.Clone();
            caretPoint.MoveToBeginningOfPreviousLine();

            int firstTextColumn = caretPoint.GetFirstNonWhiteSpaceCharacterOnViewLine().CurrentPosition;
            if (firstTextColumn == caretPoint.EndOfViewLine)
                firstTextColumn = caretPoint.StartOfViewLine;

            _editorPrimitives.Caret.MoveTo(firstTextColumn, select);
        }

        public void MoveToLastNonWhiteSpaceCharacter(bool select)
        {
            int lastNonWhiteSpaceCharacterInLine = _editorPrimitives.Caret.EndOfViewLine - 1;
            for (; lastNonWhiteSpaceCharacterInLine >= _editorPrimitives.Caret.StartOfViewLine; lastNonWhiteSpaceCharacterInLine--)
            {
                string nextCharacter = _editorPrimitives.View.GetTextPoint(lastNonWhiteSpaceCharacterInLine).GetNextCharacter();
                if (!char.IsWhiteSpace(nextCharacter[0]))
                {
                    break;
                }
            }

            lastNonWhiteSpaceCharacterInLine = Math.Max(lastNonWhiteSpaceCharacterInLine, _editorPrimitives.Caret.StartOfLine);

            if (lastNonWhiteSpaceCharacterInLine != _editorPrimitives.Caret.CurrentPosition)
            {
                _editorPrimitives.Caret.MoveTo(lastNonWhiteSpaceCharacterInLine, select);
            }
        }

        public void MoveToTopOfView(bool select)
        {
            // TextViewLines may have both a partially and a fully hidden line at the top, or either, or neither.
            ITextViewLineCollection lines = _editorPrimitives.View.AdvancedTextView.TextViewLines;
            ITextViewLine firstVisibleLine = lines.FirstVisibleLine;
            int iFirstVisibleLine = lines.GetIndexOfTextLine(firstVisibleLine);
            ITextViewLine fullyVisibleLine = FindFullyVisibleLine(firstVisibleLine, iFirstVisibleLine + 1);

            MoveCaretToTextLine(fullyVisibleLine, select);
        }

        public void MoveToBottomOfView(bool select)
        {
            // TextViewLines may have both a partially and a fully hidden line at the end, or either, or neither.
            ITextViewLineCollection lines = _editorPrimitives.View.AdvancedTextView.TextViewLines;
            ITextViewLine lastVisibleLine = lines.LastVisibleLine;
            int iLastVisibleLine = lines.GetIndexOfTextLine(lastVisibleLine);
            ITextViewLine fullyVisibleLine = FindFullyVisibleLine(lastVisibleLine, iLastVisibleLine - 1);

            MoveCaretToTextLine(fullyVisibleLine, select);
        }

        /// <summary>
        /// Deletes the word to the right of the current caret position.
        /// </summary>
        public bool DeleteWordToRight()
        {
            Func<bool> action = () =>
            {
                TextPoint startPointOfDelete = _editorPrimitives.Caret.Clone();

                TextRange nextWord = startPointOfDelete.GetNextWord();
                TextPoint endPointOfDelete = nextWord.GetStartPoint();

                // If this delete did not start at the end of the line
                // then only delete to the end of the line.
                int endOfLine = startPointOfDelete.EndOfLine;
                if (startPointOfDelete.CurrentPosition != endOfLine)
                {
                    // It is possible that the text structure navigator has returned
                    // a word that spans multiple lines.  In that case, just delete to
                    // the end of the line to match VS9 behavior.
                    if (endPointOfDelete.CurrentPosition > endOfLine)
                    {
                        endPointOfDelete.MoveTo(endOfLine);
                    }
                    else if (startPointOfDelete.CurrentPosition >= endPointOfDelete.CurrentPosition)
                    {
                        //If the startPointOfDelete was on the last word of the line then endPointOfDelete might be the
                        //start of the word so we should, instead, delete to the end of the line (or, at least, that is
                        //what we think is happening).
                        endPointOfDelete.MoveTo(endOfLine);
                    }
                }

                return ExpandRangeToIncludeSelection(startPointOfDelete.GetTextRange(endPointOfDelete)).Delete();
            };

            return ExecuteAction(Strings.DeleteWordToRight, action);
        }

        /// <summary>
        /// Deletes the word to the left of the current caret position.
        /// </summary>
        public bool DeleteWordToLeft()
        {
            Func<bool> action = () =>
            {
                TextRange currentWord = _editorPrimitives.Caret.GetCurrentWord();
                TextRange rangeToDelete = currentWord;
                if (_editorPrimitives.Caret.CurrentPosition > currentWord.GetStartPoint().CurrentPosition)
                {
                    rangeToDelete = currentWord.GetStartPoint().GetTextRange(_editorPrimitives.Caret);
                }
                else
                {
                    TextRange previousWord = _editorPrimitives.Caret.GetPreviousWord();
                    rangeToDelete = previousWord.GetStartPoint().GetTextRange(_editorPrimitives.Caret);
                }

                return ExpandRangeToIncludeSelection(rangeToDelete).Delete();
            };

            return ExecuteAction(Strings.DeleteWordToLeft, action);
        }

        public bool DeleteToBeginningOfLine()
        {
            Func<bool> action = () =>
            {
                TextRange selectionRange = _editorPrimitives.Selection.Clone();

                if (_editorPrimitives.Selection.IsReversed || _editorPrimitives.Selection.IsEmpty)
                {
                    selectionRange.SetStart(_editorPrimitives.View.GetTextPoint(_editorPrimitives.Caret.StartOfLine));
                }

                return selectionRange.Delete();
            };

            return ExecuteAction(Strings.DeleteToBOL, action);
        }

        public bool DeleteToEndOfLine()
        {
            Func<bool> action = () =>
            {
                TextRange selectionRange = _editorPrimitives.Selection.Clone();

                if (!_editorPrimitives.Selection.IsReversed || _editorPrimitives.Selection.IsEmpty)
                {
                    selectionRange.SetEnd(_editorPrimitives.View.GetTextPoint(_editorPrimitives.Caret.EndOfViewLine));
                }

                return selectionRange.Delete();
            };

            return ExecuteAction(Strings.DeleteToEOL, action);
        }

        /// <summary>
        /// Deletes a character to the left of the current caret.
        /// </summary>
        public bool Backspace()
        {
            bool success = true;

            if (WillBackspaceCreateEdit())
            {
                var selections = _multiSelectionBroker.AllSelections;
                var boxSelection = _multiSelectionBroker.BoxSelection;
                var primarySelection = _multiSelectionBroker.PrimarySelection;

                Func<bool> action = () =>
                {
                    using (_multiSelectionBroker.BeginBatchOperation())
                    {
                        if (TryBackspaceEdit(selections))
                        {
                            return TryPostBackspaceSelectionUpdate(selections, primarySelection, boxSelection);
                        }
                    }
                    return false;
                };

                success = ExecuteAction(Strings.DeleteCharToLeft, action, SelectionUpdate.Ignore, ensureVisible: false);
            }
            else
            {
                success = TryBackspaceSelections();
            }

            if (success)
            {
                _multiSelectionBroker.TryEnsureVisible(_multiSelectionBroker.PrimarySelection, EnsureSpanVisibleOptions.MinimumScroll);
            }

            return success;
        }

        private bool TryPostBackspaceSelectionUpdate(IReadOnlyList<Selection> selections, Selection primarySelection, Selection boxSelection)
        {
            // Throughout this method, the parameters passed in are the OLD values, and the parameters on _multiSelectionBroker are the NEW ones

            if (boxSelection != Selection.Invalid)
            {
                // If this is an empty box, we may need to capture the new active/anchor points, as points in virtual space
                // won't track as we want them to through the edit.
                VirtualSnapshotPoint anchorPoint = _multiSelectionBroker.BoxSelection.AnchorPoint;
                VirtualSnapshotPoint activePoint = _multiSelectionBroker.BoxSelection.ActivePoint;

                if (primarySelection.IsEmpty)
                {
                    if (boxSelection.AnchorPoint.IsInVirtualSpace)
                    {
                        anchorPoint = new VirtualSnapshotPoint(_multiSelectionBroker.BoxSelection.AnchorPoint.Position, boxSelection.AnchorPoint.VirtualSpaces - 1);
                    }
                    if (boxSelection.ActivePoint.IsInVirtualSpace)
                    {
                        activePoint = new VirtualSnapshotPoint(_multiSelectionBroker.BoxSelection.ActivePoint.Position, boxSelection.ActivePoint.VirtualSpaces - 1);
                    }
                }
                else
                {
                    // Just take the starting points in the first and last selections
                    activePoint = selections[boxSelection.IsReversed ? 0 : selections.Count - 1].Start;
                    anchorPoint = selections[boxSelection.IsReversed ? selections.Count - 1 : 0].Start;
                }

                VirtualSnapshotPoint newAnchor = anchorPoint.TranslateTo(_textView.TextSnapshot);
                VirtualSnapshotPoint newActive = activePoint.TranslateTo(_textView.TextSnapshot);

                var newSelection = new Selection(insertionPoint: newActive, anchorPoint: newAnchor, activePoint: newActive, boxSelection.InsertionPointAffinity);
                if (_multiSelectionBroker.BoxSelection != newSelection)
                {
                    _multiSelectionBroker.SetBoxSelection(newSelection);
                }
            }
            else
            {
                // Perf: This is actually an n^2 algorithm here, since TryPerform... also loops through all the selections. Try to avoid copying this code
                // elsewhere. We need it here because we're actually modifying each one based on its context AND because merges can happen with backspace so we
                // can't do anything funny like caching the transformers.
                for (int i = 0; i < selections.Count; i++)
                {
                    //Some could have merged away, ignore return values here intentionally.
                    _multiSelectionBroker.TryPerformActionOnSelection(selections[i], transformer =>
                    {
                        // We can't use the virtual snapshot point TranslateTo since it will remove the virtual space (because the line's line break was deleted).
                        // VirtualSnapshotPoint.TranslateTo doesn't know what to do with virtual whitespace, so we have to do this ourselves.
                        if (selections[i].IsEmpty && selections[i].InsertionPoint.IsInVirtualSpace)
                        {
                            // Move the caret back one if we have an empty selection
                            transformer.MoveTo(new VirtualSnapshotPoint(transformer.Selection.InsertionPoint.Position, selections[i].InsertionPoint.VirtualSpaces - 1),
                                select: false,
                                insertionPointAffinity: PositionAffinity.Successor);
                        }
                        else
                        {
                            //Move the caret to the start of the selection.
                            transformer.MoveTo(new VirtualSnapshotPoint(transformer.Selection.InsertionPoint.Position, selections[i].Start.VirtualSpaces),
                            select: false,
                            PositionAffinity.Successor);
                        }
                    }, out _);
                }
            }

            return true;
        }

        private bool WillBackspaceCreateEdit()
        {
            if (_multiSelectionBroker.IsBoxSelection)
            {
                // Edits can not happen if we're a box selection at the beginning of a line
                var primary = _multiSelectionBroker.PrimarySelection;
                if (primary.IsEmpty && primary.Start.Position == primary.Start.Position.GetContainingLine().Start)
                {
                    return false;
                }
            }

            var selections = _multiSelectionBroker.AllSelections;
            for (int i = 0; i < selections.Count; i++)
            {
                if ((!selections[i].Extent.SnapshotSpan.IsEmpty) ||
                    (selections[i].IsEmpty
                      && !selections[i].InsertionPoint.IsInVirtualSpace
                      && selections[i].InsertionPoint.Position.Position != 0))
                {
                    return true;
                }
            }

            return false;
        }

        private bool TryBackspaceEdit(IReadOnlyList<Selection> selections)
        {
            using (var edit = _textView.TextBuffer.CreateEdit())
            {
                for (int i = (selections.Count - 1); i >= 0; i--)
                {
                    var selection = selections[i];

                    if (selection.IsEmpty)
                    {
                        if (selection.Extent.IsInVirtualSpace)
                        {
                            continue;
                        }

                        if (!TryBackspaceEmptySelection(selection, edit))
                        {
                            return false;
                        }
                    }
                    else if (!edit.Delete(selection.Extent.SnapshotSpan))
                    {
                        return false;
                    }
                }

                edit.Apply();
                return !edit.Canceled;
            }
        }

        private bool TryBackspaceEmptySelection(Selection selection, ITextEdit edit)
        {
            // Assumptions:
            // We should have already validated this before calling.
            Debug.Assert(selection.IsEmpty);

            // This method is only written to perform edits on text. Virtual space operations should be performed separately and not passed here.
            Debug.Assert(!selection.InsertionPoint.IsInVirtualSpace);

            // Performing deletion:
            // Identify what should be deleted
            if (selection.InsertionPoint.Position.Position == 0)
            {
                // We're at the beginning of the document, we're done.
                return true;
            }

            // Get the span of the previous element
            SnapshotSpan previousElementSpan = TextView.GetTextElementSpan(selection.InsertionPoint.Position - 1);

            // Here we have some interesting decisions to make. If this is a collapsed region, we want to delete the whole thing.
            // If this is a multi-byte character, we typically want to delete just one byte to allow for easier typing in chinese and other languages.
            // However, if that multi-byte character is a surrogate pair or newline, we want to delete the whole thing.

            // We start by looking to see if this is a collapsed region or something like one.
            if ((previousElementSpan.Length > 0) &&
                (_textView.TextViewModel.IsPointInVisualBuffer(selection.InsertionPoint.Position, PositionAffinity.Successor)) &&
                (!_textView.TextViewModel.IsPointInVisualBuffer(previousElementSpan.End - 1, PositionAffinity.Successor)))
            {
                // Since the previous character is not visible but the current one is, delete 
                // the entire previous text element span.
                return edit.Delete(previousElementSpan);
            }
            else
            {
                int previousPosition = selection.InsertionPoint.Position.Position - 1;
                //If span length == 1 it's no brainer to delete just 1 char
                if (previousElementSpan.Length == 1)
                    return edit.Delete(new Span(previousPosition, 1));

                ITextSnapshot snapshot = edit.Snapshot;

                //Handle deleting both characters of Windows new line
                if (snapshot[previousPosition] == '\n' &&
                    snapshot[previousPosition - 1] == '\r')
                {
                    return edit.Delete(new Span(previousPosition - 1, 2));
                }

                // Check if it's emoji or other kind of multichar unicode that should be deleted at once
                if (char.GetUnicodeCategory(snapshot[previousPosition]) == UnicodeCategory.Surrogate)
                {
                    return edit.Delete(new Span(selection.InsertionPoint.Position.Position - previousElementSpan.Length, previousElementSpan.Length));
                }

                // This is also multichar unicode, but we don't want to delete it at once but char by char
                // because thats what user expects when doing chinese or arabic input
                return edit.Delete(new Span(previousPosition, 1));
            }
        }

        private bool TryBackspaceSelections()
        {
            if (_multiSelectionBroker.IsBoxSelection && _multiSelectionBroker.PrimarySelection.InsertionPoint.IsInVirtualSpace)
            {
                _multiSelectionBroker.SetSelection(new Selection(_multiSelectionBroker.PrimarySelection.Start));
            }
            else if (!_multiSelectionBroker.IsBoxSelection)
            {
                _multiSelectionBroker.PerformActionOnAllSelections(transformer =>
                {
                    if (transformer.Selection.IsEmpty)
                    {
                        if (transformer.Selection.InsertionPoint.IsInVirtualSpace)
                        {
                            MoveToPreviousTabStop(transformer);
                        }
                        else
                        {
                            transformer.PerformAction(PredefinedSelectionTransformations.MoveToPreviousCaretPosition);
                        }
                    }
                    else
                    {
                        transformer.MoveTo(transformer.Selection.Start, select: false, PositionAffinity.Successor);
                    }
                });
            }

            return true;
        }

        private void MoveToPreviousTabStop(ISelectionTransformer transformer)
        {
            var previousStop = GetPreviousIndentStopInVirtualSpace(transformer.Selection.InsertionPoint);
            transformer.MoveTo(previousStop, select: false, PositionAffinity.Successor);
        }

        private void ResetVirtualSelection()
        {
            //Move the caret to the same line as the active point, but at the left edge of the current selection.
            VirtualSnapshotPoint start = _textView.Selection.Start;
            ITextViewLine startLine = _textView.GetTextViewLineContainingBufferPosition(start.Position);

            VirtualSnapshotPoint end = _textView.Selection.End;
            ITextViewLine endLine = _textView.GetTextViewLineContainingBufferPosition(end.Position);

            double leftEdge = Math.Min(startLine.GetExtendedCharacterBounds(start).Left, endLine.GetExtendedCharacterBounds(end).Left);

            ITextViewLine activeLine = (_textView.Selection.IsReversed) ? startLine : endLine;
            VirtualSnapshotPoint newCaret = activeLine.GetInsertionBufferPositionFromXCoordinate(leftEdge);

            _multiSelectionBroker.ClearSecondarySelections();
            Selection unused;
            _multiSelectionBroker.TryPerformActionOnSelection(_multiSelectionBroker.PrimarySelection, transformer =>
            {
                transformer.MoveTo(newCaret, select: false, PositionAffinity.Successor);
            }, out unused);
        }

        public bool DeleteFullLine()
        {
            return ExecuteAction(Strings.DeleteLine, () => GetFullLines().Delete());
        }

        public bool Tabify()
        {
            return ConvertLeadingWhitespace(Strings.Tabify, convertTabsToSpaces: false);
        }

        public bool Untabify()
        {
            return ConvertLeadingWhitespace(Strings.Untabify, convertTabsToSpaces: true);
        }

        public bool ConvertSpacesToTabs()
        {
            return ExecuteAction(Strings.ConvertSpacesToTabs, delegate { return ConvertSpacesAndTabsHelper(true); });
        }

        public bool ConvertTabsToSpaces()
        {
            return ExecuteAction(Strings.ConvertTabsToSpaces, delegate { return ConvertSpacesAndTabsHelper(false); });
        }

        public bool NormalizeLineEndings(string replacement)
        {
            return ExecuteAction(Strings.NormalizeLineEndings, () => NormalizeLineEndingsHelper(replacement));
        }

        /// <summary>
        /// Selects the current word.
        /// </summary>
        public void SelectCurrentWord()
        {
            TextRange currentWord = _editorPrimitives.Caret.GetCurrentWord();
            TextRange previousWord = _editorPrimitives.Caret.GetPreviousWord();
            TextRange selection = _editorPrimitives.Selection.Clone();

            if (!_editorPrimitives.Selection.IsEmpty)
            {
                if ((
                    (selection.GetStartPoint().CurrentPosition == currentWord.GetStartPoint().CurrentPosition) &&
                    (selection.GetEndPoint().CurrentPosition == currentWord.GetEndPoint().CurrentPosition)) ||
                    (selection.GetStartPoint().CurrentPosition == previousWord.GetStartPoint().CurrentPosition) &&
                    (selection.GetEndPoint().CurrentPosition == previousWord.GetEndPoint().CurrentPosition))
                {
                    // If the selection is already correct, don't select again, but *do* ensure
                    // the existing span is visible, even though we aren't moving the selection.
                    _textView.ViewScroller.EnsureSpanVisible(_textView.Selection.StreamSelectionSpan.SnapshotSpan, EnsureSpanVisibleOptions.MinimumScroll);
                    return;
                }
            }

            // If the current word is blank, use the previous word if it is on the same line.
            if (currentWord.IsEmpty)
            {
                TextRange previous = currentWord.GetStartPoint().GetPreviousWord();
                if (previous.GetStartPoint().LineNumber == currentWord.GetStartPoint().LineNumber)
                    currentWord = previous;
            }
            _editorPrimitives.Selection.SelectRange(currentWord);

        }

        /// <summary>
        /// Selects the enclosing.
        /// </summary>
        public void SelectEnclosing()
        {
            SnapshotSpan selectionSpan;
            if (_textView.Selection.IsEmpty || _textView.Selection.Mode == TextSelectionMode.Box)
            {
                selectionSpan = new SnapshotSpan(_textView.Caret.Position.BufferPosition, 0);
            }
            else
            {
                // This ignores virtual space
                selectionSpan = _textView.Selection.StreamSelectionSpan.SnapshotSpan;
            }

            Span span = _textStructureNavigator.GetSpanOfEnclosing(selectionSpan);

            TextRange enclosingRange = _editorPrimitives.Buffer.GetTextRange(span.Start, span.End);
            _editorPrimitives.Selection.SelectRange(enclosingRange);
        }

        /// <summary>
        /// Selects the first child.
        /// </summary>
        public void SelectFirstChild()
        {
            SnapshotSpan selectionSpan;
            if (_textView.Selection.IsEmpty || _textView.Selection.Mode == TextSelectionMode.Box)
            {
                selectionSpan = new SnapshotSpan(_textView.Caret.Position.BufferPosition, 0);
            }
            else
            {
                // This ignores virtual space
                selectionSpan = _textView.Selection.StreamSelectionSpan.SnapshotSpan;
            }

            Span span = _textStructureNavigator.GetSpanOfFirstChild(selectionSpan);

            TextRange firstChildRange = _editorPrimitives.Buffer.GetTextRange(span.Start, span.End);
            _editorPrimitives.Selection.SelectRange(firstChildRange);
        }

        /// <summary>
        /// Selects the next sibling.
        /// </summary>
        /// <param name="extendSelection">Specifies whether the selection is to be extended or a new selection is to be made.</param>
        public void SelectNextSibling(bool extendSelection)
        {
            SnapshotSpan selectionSpan;
            if (_textView.Selection.IsEmpty || _textView.Selection.Mode == TextSelectionMode.Box)
            {
                selectionSpan = new SnapshotSpan(_textView.Caret.Position.BufferPosition, 0);
            }
            else
            {
                // This ignores virtual space
                selectionSpan = _textView.Selection.StreamSelectionSpan.SnapshotSpan;
            }

            Span span = _textStructureNavigator.GetSpanOfNextSibling(selectionSpan);

            _textView.Selection.Clear();
            if (!span.IsEmpty && extendSelection)
            {
                // extend the selection to the end of the next sibling
                int start = (span.Start <= selectionSpan.Start) ? span.Start : selectionSpan.Start;
                int end = (span.End <= selectionSpan.End) ? selectionSpan.End : span.End;
                span = Span.FromBounds(start, end);
            }

            TextRange nextSiblingRange = _editorPrimitives.Buffer.GetTextRange(span.Start, span.End);
            _editorPrimitives.Selection.SelectRange(nextSiblingRange);
        }

        /// <summary>
        /// Selects the previous sibling.
        /// </summary>
        /// <param name="extendSelection">Specifies whether the selection is to be extended or a new selection is to be made.</param>
        public void SelectPreviousSibling(bool extendSelection)
        {
            SnapshotSpan selectionSpan;
            if (_textView.Selection.IsEmpty || _textView.Selection.Mode == TextSelectionMode.Box)
            {
                selectionSpan = new SnapshotSpan(_textView.Caret.Position.BufferPosition, 0);
            }
            else
            {
                // This ignores virtual space
                selectionSpan = _textView.Selection.StreamSelectionSpan.SnapshotSpan;
            }

            Span span = _textStructureNavigator.GetSpanOfPreviousSibling(selectionSpan);

            _textView.Selection.Clear();
            if (!span.IsEmpty && extendSelection)
            {
                // extend the selection to the start of the previous sibling
                int start = (span.Start <= selectionSpan.Start) ? span.Start : selectionSpan.Start;
                int end = (span.End <= selectionSpan.End) ? selectionSpan.End : span.End;
                span = new Span(start, end - start);
            }

            TextRange previousSiblingRange = _editorPrimitives.Buffer.GetTextRange(span.Start, span.End);
            _editorPrimitives.Selection.SelectRange(previousSiblingRange);
        }

        /// <summary>
        /// Selects all text.
        /// </summary>
        public void SelectAll()
        {
            _editorPrimitives.Selection.SelectAll(_editorOptions.ShouldMoveCaretOnSelectAll());
        }

        /// <summary>
        /// Extends the current selection span to the new selection end.
        /// </summary>
        /// <param name="newEnd">
        /// The new character position to extend the selection to.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="newEnd"/> is less than 0.</exception>
        public void ExtendSelection(int newEnd)
        {
            _editorPrimitives.Selection.ExtendSelection(_editorPrimitives.Buffer.GetTextPoint(newEnd));
        }

        /// <summary>
        /// Moves the caret to the given <paramref name="textLine"/> at the given horizontal offset <paramref name="horizontalOffset"/>.
        /// </summary>
        /// <param name="textLine">The <see cref="ITextViewLine"/> on which to place the caret.</param>
        /// <param name="horizontalOffset">The horizontal location in the given <paramref name="textLine"/> at which to move the caret.</param>
        /// <exception cref="ArgumentNullException"><paramref name="textLine"/> is null.</exception>
        public void MoveCaret(ITextViewLine textLine, double horizontalOffset, bool extendSelection)
        {
            if (textLine == null)
            {
                throw new ArgumentNullException(nameof(textLine));
            }

            if (extendSelection)
            {
                VirtualSnapshotPoint anchor = _textView.Selection.AnchorPoint;
                _textView.Caret.MoveTo(textLine, horizontalOffset);

                // It is possible that the text was modified as part of this caret move so translate the anchor
                _textView.Selection.Select(anchor.TranslateTo(_textView.TextSnapshot), _textView.Caret.Position.VirtualBufferPosition);
            }
            else
            {
                // Retain the selection mode, even though we are clearing it
                bool inBox = _textView.Selection.Mode == TextSelectionMode.Box;

                _textView.Selection.Clear();

                if (inBox)
                    _textView.Selection.Mode = TextSelectionMode.Box;

                _textView.Caret.MoveTo(textLine, horizontalOffset);
            }
        }

        /// <summary>
        /// Puts the caret one line up.
        /// </summary>
        /// <param name="select">
        /// Specifies whether selection is made as the caret is moved.
        /// </param>
        public void MoveLineUp(bool select)
        {
            _multiSelectionBroker.PerformActionOnAllSelections(select ? PredefinedSelectionTransformations.SelectToPreviousLine : PredefinedSelectionTransformations.MoveToPreviousLine);
            _textView.Caret.EnsureVisible();
        }

        /// <summary>
        /// Puts the caret one line down.
        /// </summary>
        /// <param name="select">
        /// Specifies whether selection is made as the caret is moved.
        /// </param>
        public void MoveLineDown(bool select)
        {
            _multiSelectionBroker.PerformActionOnAllSelections(select ? PredefinedSelectionTransformations.SelectToNextLine : PredefinedSelectionTransformations.MoveToNextLine);
            _textView.Caret.EnsureVisible();
        }

        /// <summary>
        /// Moves the caret one page up.
        /// </summary>
        /// <param name="select">
        /// Specifies whether selection is made as the caret is moved.
        /// </param>
        public void PageUp(bool select)
        {
            _multiSelectionBroker.PerformActionOnAllSelections(select ? PredefinedSelectionTransformations.SelectPageUp : PredefinedSelectionTransformations.MovePageUp);

        }

        /// <summary>
        /// Moves the caret one page down.
        /// </summary>
        /// <param name="select">
        /// Specifies whether selection is made as the caret is moved.
        /// </param>
        public void PageDown(bool select)
        {
            _multiSelectionBroker.PerformActionOnAllSelections(select ? PredefinedSelectionTransformations.SelectPageDown : PredefinedSelectionTransformations.MovePageDown);
        }

        /// <summary>
        /// Moves the caret to the end of the view line.
        /// </summary>
        /// <param name="select">
        /// Specifies whether selection is made as the caret is moved.
        /// </param>
        public void MoveToEndOfLine(bool select)
        {
            _multiSelectionBroker.PerformActionOnAllSelections(select ? PredefinedSelectionTransformations.SelectToEndOfLine : PredefinedSelectionTransformations.MoveToEndOfLine);
            _textView.Caret.EnsureVisible();
        }

        public void MoveToHome(bool select)
        {
            _multiSelectionBroker.PerformActionOnAllSelections(select ? PredefinedSelectionTransformations.SelectToHome : PredefinedSelectionTransformations.MoveToHome);
            _textView.Caret.EnsureVisible();
        }

        public void MoveToStartOfLine(bool select)
        {
            _editorPrimitives.Caret.MoveToStartOfViewLine(select);
        }

        /// <summary>
        /// Inserts a new line at the current caret position.
        /// </summary>
        public bool InsertNewLine()
        {
            Func<bool> action = () =>
            {
                bool editSucceeded = true;
                ITextSnapshot snapshot = _textView.TextViewModel.EditBuffer.CurrentSnapshot;

                using (var batchOp = _multiSelectionBroker.BeginBatchOperation())
                {
                    var toIndent = new HashSet<object>();

                    using (var edit = _textView.TextBuffer.CreateEdit())
                    {
                        if (_multiSelectionBroker.IsBoxSelection)
                        {
                            _multiSelectionBroker.BreakBoxSelection();
                        }

                        _multiSelectionBroker.PerformActionOnAllSelections(transformer =>
                        {
                            bool doIndent = false;

                            VirtualSnapshotPoint caret = transformer.Selection.InsertionPoint;
                            ITextSnapshotLine line = caret.Position.GetContainingLine();
                            ITextViewLine viewLine = _textView.GetTextViewLineContainingBufferPosition(caret.Position);

                            // todo: the following logic is duplicated in DefaultTextPointPrimitive.InsertNewLine()
                            // didn't call that method here because it would result in two text transactions
                            // ultimately everything here should probably move into primitives.
                            string textToInsert = TextBufferOperationHelpers.GetNewLineCharacterToInsert(line, _editorOptions);

                            // Indent unless the caret is at column 0 or the current line is empty. 
                            // This appears to be added as a fix for Venus; which combined with our implementation of 
                            // PositionCaretWithSmartIndent does not indent correctly on NewLine when Caret is at column 0.
                            doIndent = caret.IsInVirtualSpace || (caret.Position != viewLine.Extent.Start)
                                                    || (viewLine.Extent.Length == 0);

                            int searchIndexforPreviousWhitespaces = -1;
                            var lineContainingTrimTrailingWhitespacesSearchindex = line; // usually is the line containing  caret.

                            // This ignores virtual space
                            Span selection = transformer.Selection.Extent.SnapshotSpan;

                            editSucceeded = editSucceeded && edit.Replace(selection, textToInsert);
                            // For stream selection you should always look for trimming whitespaces previous to selection.start instead of caret position
                            lineContainingTrimTrailingWhitespacesSearchindex = snapshot.GetLineFromPosition(selection.Start);
                            searchIndexforPreviousWhitespaces = selection.Start - lineContainingTrimTrailingWhitespacesSearchindex.Start.Position;
                            // Trim traling whitespaces as we insert the new line as well if the editor option is set
                            if (_editorOptions.GetOptionValue<bool>(DefaultOptions.TrimTrailingWhiteSpaceOptionId))
                            {
                                var previousNonWhitespaceCharacterIndex = lineContainingTrimTrailingWhitespacesSearchindex.IndexOfPreviousNonWhiteSpaceCharacter(searchIndexforPreviousWhitespaces);

                                // Note: If previousNonWhiteSpaceCharacter index is -1 this will automatically default to line.start.position
                                var startIndexForTrailingWhitespaceSpan = lineContainingTrimTrailingWhitespacesSearchindex.Start.Position + previousNonWhitespaceCharacterIndex + 1;
                                var lengthOfTrailingWhitespaceSpan = searchIndexforPreviousWhitespaces - previousNonWhitespaceCharacterIndex - 1;

                                if (lengthOfTrailingWhitespaceSpan != 0) // If there are any whitespaces before the caret delete them
                                    edit.Delete(new Span(startIndexForTrailingWhitespaceSpan, lengthOfTrailingWhitespaceSpan));
                            }

                            if (doIndent)
                            {
                                // WARNING: We're caching the transformers here because we are both inside a batch operation
                                // and we're inserting text, so we know that there will be no merging of selections going on.
                                // We're using them as a perf optimization so we can avoid searching through the list of selections
                                // later, since we already know what we need.
                                //
                                // When writing multiple selection-aware code, do everything you can to avoid saving transformers.
                                toIndent.Add(transformer);
                            }
                        });

                        // Apply all changes
                        editSucceeded = editSucceeded && (edit.Apply() != snapshot);
                    }

                    if (editSucceeded && toIndent.Count > 0)
                    {
                        // Need to move carets to indented location after the edit has completed, so we put them at the correct indentation in the new snapshot.
                        _multiSelectionBroker.PerformActionOnAllSelections(transformer =>
                                {
                                    if (toIndent.Contains(transformer))
                                    {
                                        var caretMoved = PositionCaretWithSmartIndent(transformer, useOnlyVirtualSpace: false, extendSelection: false);
                                        if (!caretMoved && transformer.Selection.InsertionPoint.IsInVirtualSpace)
                                        {
                                            //No smart indent logic so make sure the caret is not in virtual space.
                                            transformer.MoveTo(new VirtualSnapshotPoint(transformer.Selection.InsertionPoint.Position), select: false, PositionAffinity.Successor);
                                        }
                                        transformer.PerformAction(PredefinedSelectionTransformations.ClearSelection);
                                        transformer.CapturePreferredReferencePoint();
                                    }
                                });
                    }
                }

                return editSucceeded;
            };
            return ExecuteAction(Strings.InsertNewLine, action, SelectionUpdate.Ignore, true);
        }

        public bool OpenLineAbove()
        {
            Func<bool> action = () =>
            {
                bool result;
                DisplayTextPoint textPoint = _editorPrimitives.Caret.Clone();
                if (textPoint.LineNumber == 0)
                {
                    _editorPrimitives.Caret.MoveTo(0);
                    result = this.InsertNewLine();
                    if (result)
                    {
                        // leave caret on the new line
                        _editorPrimitives.Caret.MoveTo(0);
                    }
                }
                else
                {
                    textPoint.MoveToBeginningOfPreviousViewLine();
                    textPoint.MoveToEndOfViewLine();
                    _editorPrimitives.Caret.MoveTo(textPoint.CurrentPosition);
                    result = this.InsertNewLine();
                }
                return result;
            };
            return ExecuteAction(Strings.OpenLineAbove, action);
        }

        public bool OpenLineBelow()
        {
            Func<bool> action = () =>
            {
                _editorPrimitives.Caret.MoveToEndOfViewLine();
                return this.InsertNewLine();
            };
            return ExecuteAction(Strings.OpenLineBelow, action);
        }

        /// <summary>
        /// If there is a multi-line selection, indents the selection, otherwise inserts a tab at the caret location.
        /// </summary>
        public bool Indent()
        {
            if (!IndentWillCreateEdit())
            {
                // Indent box selection in virtual whitespace, if any.
                if (_multiSelectionBroker.IsBoxSelection)
                {
                    int indentSize = _editorOptions.GetIndentSize();
                    VirtualSnapshotPoint? anchorPoint = CalculateBoxIndentForSelectionPoint(_multiSelectionBroker.BoxSelection.AnchorPoint, indentSize);
                    VirtualSnapshotPoint? activePoint = CalculateBoxIndentForSelectionPoint(_multiSelectionBroker.BoxSelection.ActivePoint, indentSize);
                    FixUpSelectionAfterBoxOperation(anchorPoint, activePoint);
                }

                return true;
            }

            Func<bool> action = () =>
            {
                using (_multiSelectionBroker.BeginBatchOperation())
                {
                    if (_multiSelectionBroker.IsBoxSelection)
                    {
                        if (!TryIndentBoxSelection())
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if (!TryIndentAndFixupStreamSelections())
                        {
                            return false;
                        }
                    }

                    _multiSelectionBroker.TryEnsureVisible(_multiSelectionBroker.PrimarySelection, EnsureSpanVisibleOptions.MinimumScroll);

                    return true;
                }
            };

            return ExecuteAction(Strings.InsertTab, action, SelectionUpdate.Ignore, ensureVisible: false);
        }

        private bool IndentWillCreateEdit()
        {
            var allSelections = _multiSelectionBroker.AllSelections;
            if (_multiSelectionBroker.IsBoxSelection)
            {
                // Box selection can only create an edit if at least one non-virtual subspan is of non-zero length.
                for (int i = 0; i < allSelections.Count; i++)
                {
                    var selection = allSelections[i];
                    if (!selection.Start.IsInVirtualSpace)
                    {
                        return true;
                    }
                }
                return false;
            }

            for (int i = 0; i < allSelections.Count; i++)
            {
                var selection = allSelections[i];

                // Carets (zero-width selections) always produce an edit.
                if (selection.IsEmpty)
                {
                    return true;
                }
                else
                {
                    var startLine = selection.Start.Position.GetContainingLine();
                    var endLine = selection.End.Position.GetContainingLine();

                    // Selections that start and end on the same line create an edit if and only if the line is non-zero in length.
                    if (startLine.LineNumber == endLine.LineNumber)
                    {
                        return (startLine.Length > 0);
                    }
                    else
                    {
                        // Only edits for multiline selections if one of the contained lines is
                        // non-zero in length because empty lines have nothing to indent.
                        for (int j = startLine.LineNumber; j < endLine.LineNumber; j++)
                        {
                            if (startLine.Snapshot.GetLineFromLineNumber(j).Length > 0)
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        private bool TryIndentAndFixupStreamSelections()
        {
            var newSelections = new FrugalList<(Selection old, Selection newSel)>();
            if (!EditHelper((edit) => IndentStreamSelections(edit, newSelections)))
            {
                return false;
            }

            _multiSelectionBroker.PerformActionOnAllSelections(transformer =>
            {
                var selection = transformer.Selection;
                if (selection.IsEmpty && selection.Extent.IsInVirtualSpace)
                {
                    // Remove all virtual space from selection.
                    transformer.MoveTo(
                        new VirtualSnapshotPoint(selection.AnchorPoint.Position),
                        new VirtualSnapshotPoint(selection.ActivePoint.Position),
                        new VirtualSnapshotPoint(selection.InsertionPoint.Position),
                        PositionAffinity.Successor);
                }
            });

            // Apply selection changes after the edit so we can apply the desired point tracking mode.
            // Preserves pre-multicaret behavior by keeping the left edge of selections unmodified
            // when the user presses 'tab' using negative tracking.
            TransformSelections(newSelections);

            return true;
        }

        private void TransformSelections(FrugalList<(Selection old, Selection newSel)> newSelections)
        {
            foreach (var (oldSelection, newSelection) in newSelections)
            {
                var start = newSelection.Start.TranslateTo(_multiSelectionBroker.CurrentSnapshot, PointTrackingMode.Negative);
                var end = newSelection.End.TranslateTo(_multiSelectionBroker.CurrentSnapshot, PointTrackingMode.Positive);
                var insertion = newSelection.InsertionPoint.TranslateTo(
                    _multiSelectionBroker.CurrentSnapshot,
                    newSelection.IsReversed ? PointTrackingMode.Negative : PointTrackingMode.Positive);

                _multiSelectionBroker.TryPerformActionOnSelection(
                    oldSelection,
                    transformer => transformer.MoveTo(
                        newSelection.IsReversed ? end : start,
                        newSelection.IsReversed ? start : end,
                        insertion,
                        newSelection.InsertionPointAffinity),
                    out _);
            }
        }

        private bool IndentStreamSelections(ITextEdit edit, IList<(Selection old, Selection newSel)> newSelections)
        {
            // Tracks whether or not an edit succeeded. Edits can fail if the caret is in a readonly region.
            bool succeeded = true;

            // Compound edits perform multiple actions with reference to the original snapshot. This tracks
            // the column adjustment of the previous caret so that we can align the subsequent caret with the
            // correct tabstop when there are multiple carets on the same line.
            var columnOffset = default(int?);

            // There is a particular edge case where there can be multiple multi-line selections on the same line.
            // Naively indenting one tabstop per selection would cause this line to be indented twice so we must
            // keep track of which line the previous multi-line selection ended on.
            int? previousSelectionEndLineNumber = 0;
            bool previousSelectionWasMultiline = false;

            _multiSelectionBroker.PerformActionOnAllSelections((transformer) =>
            {
                int? newSelectionStartLineNumber = transformer.Selection.Start.Position.GetContainingLine().LineNumber;
                int? newSelectionEndLineNumber = transformer.Selection.End.Position.GetContainingLine().LineNumber;

                // Reset the column offset if we've transitioned to the next line.
                if (newSelectionStartLineNumber != previousSelectionEndLineNumber)
                {
                    columnOffset = null;
                }

                previousSelectionWasMultiline = IndentOperationShouldBeMultiLine(transformer.Selection);

                if (IndentOperationShouldBeMultiLine(transformer.Selection))
                {
                    // Don't indent this line if we are the second multi-line selection intersecting this line
                    // or else we'll cause a double-indent.
                    bool skipFirstLine = (columnOffset != null) && previousSelectionEndLineNumber == newSelectionStartLineNumber && previousSelectionWasMultiline;

                    // In case there are multiple carets on a single line, we have to remember the column
                    // to ensure that the subsequent carets on the same line receive adequate indentation.
                    columnOffset = PerformIndentActionOnEachBufferLine(
                        edit,
                        newSelections,
                        skipFirstLine ? MultiLineSelectionWithoutFirstLine(transformer.Selection) : transformer.Selection,
                        InsertSingleIndentAtPoint);
                    if (columnOffset == null)
                    {
                        succeeded = false;
                        return;
                    }
                }
                else
                {
                    // In case there are multiple carets on a single line, we have to remember the column
                    // to ensure that the subsequent carets on the same line receive adequate indentation.
                    columnOffset = InsertIndentForSpan(transformer.Selection.Extent, edit, exactlyOneIndentLevel: false, false, columnOffset: columnOffset ?? 0);
                    if (columnOffset == null)
                    {
                        succeeded = false;
                        return;
                    }
                }

                previousSelectionEndLineNumber = newSelectionEndLineNumber;
            });

            return succeeded;
        }

        private static Selection MultiLineSelectionWithoutFirstLine(Selection selection)
        {
            var snapshot = selection.Start.Position.Snapshot;
            var firstLineNumber = selection.Start.Position.GetContainingLine().LineNumber;

            // Requires the selection to be multiple lines.
            Debug.Assert(firstLineNumber <= snapshot.LineCount);

            var start = new VirtualSnapshotPoint(snapshot.GetLineFromLineNumber(firstLineNumber + 1).Start);

            return new Selection(
                new VirtualSnapshotSpan(start, selection.End),
                isReversed: selection.IsReversed);
        }

        private bool TryIndentBoxSelection()
        {
            return this.EditHelper((edit) =>
            {
                int tabSize = _editorOptions.GetTabSize();
                int indentSize = _editorOptions.GetIndentSize();
                bool convertTabsToSpaces = _editorOptions.IsConvertTabsToSpacesEnabled();

                // We'll need to update the start/end points if they are in virtual space, since they won't be tracking
                // through a text change.
                VirtualSnapshotPoint? anchorPoint = CalculateBoxIndentForSelectionPoint(_multiSelectionBroker.BoxSelection.AnchorPoint, indentSize);
                VirtualSnapshotPoint? activePoint = CalculateBoxIndentForSelectionPoint(_multiSelectionBroker.BoxSelection.ActivePoint, indentSize);

                // Insert an indent for each portion of the selection (with an empty selection, there will only be a single
                // span).
                foreach (VirtualSnapshotSpan span in _textView.Selection.VirtualSelectedSpans)
                {
                    if (InsertIndentForSpan(span, edit, exactlyOneIndentLevel: false) == null)
                    {
                        return false;
                    }
                }

                FixUpSelectionAfterBoxOperation(anchorPoint, activePoint);

                return true;
            });
        }

        /// <summary>
        /// If there is a multi-line selection, unindents the selection.  If there is a single line selection,
        /// removes up to a indent's worth of whitespace from before the start of the selection.  If there is no selection,
        /// removes up to a intent's worth of whitespace from before the caret position.
        /// </summary>
        public bool Unindent()
        {
            if (UnindentWillCreateEdit())
            {
                Func<bool> action = () =>
                {
                    using (_multiSelectionBroker.BeginBatchOperation())
                    {
                        var succeeded = this.EditHelper(edit =>
                        {
                            if (_multiSelectionBroker.IsBoxSelection)
                            {
                                return UnindentBoxSelection(edit);
                            }
                            else
                            {
                                return UnindentStreamSelections(edit);
                            }
                        });

                        _multiSelectionBroker.TryEnsureVisible(_multiSelectionBroker.PrimarySelection, EnsureSpanVisibleOptions.MinimumScroll);

                        return succeeded;
                    }
                };

                return ExecuteAction(Strings.RemovePreviousTab, action, SelectionUpdate.Ignore, ensureVisible: true);
            }
            else
            {
                using (_multiSelectionBroker.BeginBatchOperation())
                {
                    if (_multiSelectionBroker.IsBoxSelection)
                    {
                        int columnsToRemove = DetermineMaxBoxUnindent();

                        // We'll need to update the start/end points if they are in virtual space, since they won't be tracking
                        // through a text change.
                        VirtualSnapshotPoint? anchorPoint = CalculateBoxUnindentForSelectionPoint(_textView.Selection.AnchorPoint, columnsToRemove);
                        VirtualSnapshotPoint? activePoint = CalculateBoxUnindentForSelectionPoint(_textView.Selection.ActivePoint, columnsToRemove);

                        FixUpSelectionAfterBoxOperation(anchorPoint, activePoint);
                    }
                    else
                    {
                        _multiSelectionBroker.PerformActionOnAllSelections(transformer =>
                        {
                            VirtualSnapshotPoint point = GetPreviousIndentStopInVirtualSpace(transformer.Selection.Start);
                            transformer.MoveTo(point, select: false, PositionAffinity.Successor);
                        });
                    }
                }

                _multiSelectionBroker.TryEnsureVisible(_multiSelectionBroker.PrimarySelection, EnsureSpanVisibleOptions.MinimumScroll);

                return true;
            }
        }

        private bool UnindentWillCreateEdit()
        {
            var allSelections = _multiSelectionBroker.AllSelections;
            if (_multiSelectionBroker.IsBoxSelection)
            {
                // Box selection can only create an edit if at least one non-virtual subspan is of non-zero length.
                for (int i = 0; i < allSelections.Count; i++)
                {
                    var selection = allSelections[i];
                    if (!selection.Start.IsInVirtualSpace)
                    {
                        return true;
                    }
                }
            }
            else
            {
                // Stream selection can only create an edit if at least one selection has an end-point in non-virtual space.
                for (int i = 0; i < allSelections.Count; i++)
                {
                    var selection = allSelections[i];
                    if (!selection.Start.IsInVirtualSpace || !selection.End.IsInVirtualSpace)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool UnindentStreamSelections(ITextEdit edit)
        {
            bool succeeded = true;
            _multiSelectionBroker.PerformActionOnAllSelections(transformer =>
            {
                Selection selection = transformer.Selection;

                if (selection.IsEmpty && selection.InsertionPoint.IsInVirtualSpace)
                {
                    VirtualSnapshotPoint point = GetPreviousIndentStopInVirtualSpace(selection.InsertionPoint);
                    transformer.MoveTo(point, select: false, PositionAffinity.Successor);
                }
                else if (IndentOperationShouldBeMultiLine(selection))
                {
                    var selections = new List<(Selection oldSelection, Selection newSelection)>();
                    if (PerformIndentActionOnEachBufferLine(edit, selections, selection, RemoveIndentAtPoint) == null)
                    {
                        succeeded = false;
                        return;
                    }
                }
                else
                {
                    if (!RemoveIndentAtPoint(selection.Start.Position, edit, failOnNonWhitespaceCharacter: false))
                    {
                        succeeded = false;
                        return;
                    }
                }
            });

            return succeeded;
        }

        private bool UnindentBoxSelection(ITextEdit edit)
        {
            int columnsToRemove = DetermineMaxBoxUnindent();

            // We'll need to update the start/end points if they are in virtual space, since they won't be tracking
            // through a text change.
            VirtualSnapshotPoint? anchorPoint = CalculateBoxUnindentForSelectionPoint(_textView.Selection.AnchorPoint, columnsToRemove);
            VirtualSnapshotPoint? activePoint = CalculateBoxUnindentForSelectionPoint(_textView.Selection.ActivePoint, columnsToRemove);

            // Remove an indent for each portion of the selection (with an empty selection, there will only be a single
            // span).
            foreach (VirtualSnapshotSpan span in _textView.Selection.VirtualSelectedSpans)
            {
                if (!RemoveIndentAtPoint(span.Start.Position, edit, failOnNonWhitespaceCharacter: false, columnsToRemove: columnsToRemove))
                    return false;
            }

            FixUpSelectionAfterBoxOperation(anchorPoint, activePoint);

            return true;
        }

        public bool IncreaseLineIndent()
        {
            Func<bool> action = () =>
            {
                // NOTE: This method doesn't account for multiple multi-line selections indenting the same
                // line, so using this method will double indent the line intersected by both selections.
                // Not worth fixing at the moment because this isn't a mapped command. See Indent for an
                // example of how this should work.
                return PerformIndentActionOnEachBufferLine(InsertSingleIndentAtPoint);
            };

            return ExecuteAction(Strings.IncreaseLineIndent, action, SelectionUpdate.Ignore, ensureVisible: true);
        }

        public bool DecreaseLineIndent()
        {
            Func<bool> action = () =>
            {
                return PerformIndentActionOnEachBufferLine(RemoveIndentAtPoint);
            };

            return ExecuteAction(Strings.DecreaseLineIndent, action, SelectionUpdate.Ignore, ensureVisible: true);
        }

        public bool DeleteBlankLines()
        {
            Func<bool> action = () =>
            {
                double oldLeft = _textView.Caret.Left;
                using (ITextEdit textEdit = _editorPrimitives.Buffer.AdvancedTextBuffer.CreateEdit())
                {
                    int startLine = _editorPrimitives.Selection.GetStartPoint().LineNumber;
                    int endLine = _editorPrimitives.Selection.GetEndPoint().LineNumber;

                    // If the selection is empty, we follow this algorithm:
                    // If the current line the caret is on is blank or the caret is at the end of the line
                    // delete all blank lines that occur later in the file.
                    // If the caret is not on a blank line and there are no blank lines below the caret
                    // then delete all blank lines between the caret line and the next non-blank line.
                    // This matches VS9 behavior.
                    if (_editorPrimitives.Selection.IsEmpty)
                    {
                        // First search downwards to see if there are blank lines to delete if we are at the end of the
                        // line or if we are on a blank line.
                        TextPoint startOfLine = _editorPrimitives.Buffer.GetTextPoint(startLine, 0);

                        if (IsPointOnBlankLine(startOfLine) ||
                            (_editorPrimitives.Caret.CurrentPosition == _editorPrimitives.Caret.EndOfLine))                  //Caret is at the physical end of a line
                        {
                            while (endLine < _editorPrimitives.Selection.AdvancedSelection.TextView.TextSnapshot.LineCount - 1)
                            {
                                TextPoint startOfNextLine = _editorPrimitives.Buffer.GetTextPoint(endLine + 1, 0);
                                if (!IsPointOnBlankLine(startOfNextLine))
                                {
                                    break;
                                }
                                endLine++;
                            }
                        }

                        // If there are no blank lines below the current line and
                        // this is not a blank line, look up to see if there are any
                        // blank lines directly above the current one
                        if (startLine == endLine)
                        {
                            if (!IsPointOnBlankLine(startOfLine))
                            {
                                while (startLine > 0)
                                {
                                    startOfLine = _editorPrimitives.Buffer.GetTextPoint(startLine - 1, 0);
                                    if (!IsPointOnBlankLine(startOfLine))
                                    {
                                        break;
                                    }
                                    startLine--;
                                }
                            }
                        }
                    }

                    for (int i = startLine; i <= endLine; i++)
                    {
                        TextPoint startPoint = _editorPrimitives.Buffer.GetTextPoint(i, 0);

                        if (IsPointOnBlankLine(startPoint))
                        {
                            TextPoint startOfNextLine = startPoint.Clone();
                            startOfNextLine.MoveToBeginningOfNextLine();
                            if (!textEdit.Delete(Span.FromBounds(startPoint.CurrentPosition, startOfNextLine.CurrentPosition)))
                                return false;
                        }
                    }
                    textEdit.Apply();

                    if (textEdit.Canceled)
                        return false;

                    _textView.Caret.EnsureVisible();
                    ITextViewLine newLine = _textView.Caret.ContainingTextViewLine;
                    _textView.Caret.MoveTo(newLine, oldLeft);

                    return true;
                }
            };

            return ExecuteAction(Strings.DeleteBlankLines, action);
        }

        public bool DeleteHorizontalWhiteSpace()
        {
            return ExecuteAction(Strings.DeleteHorizontalWhiteSpace, DeleteHorizontalWhitespace);
        }

        public Boolean InsertFinalNewLine()
        {
            Func<bool> action = () =>
            {
                var buffer = _textView.TextBuffer;
                return TextBufferOperationHelpers.TryInsertFinalNewLine(buffer, _editorOptions);
            };

            return ExecuteAction(Strings.InsertFinalNewLine, action, SelectionUpdate.Ignore, true);
        }

        public bool TrimTrailingWhiteSpace()
        {
            Func<bool> action = () =>
            {
                ITextSnapshot snapshot = _textView.TextSnapshot;
                var startLine = snapshot.GetLineFromPosition(_textView.Selection.Start.Position);
                var endLine = snapshot.GetLineFromPosition(_textView.Selection.End.Position);

                return TrimTrailingWhiteSpace(startLine, endLine);
            };

            return ExecuteAction(Strings.TrimTrailingWhitespace, action, SelectionUpdate.Ignore, true);
        }

        private bool TrimTrailingWhiteSpace(ITextSnapshotLine line)
        {
            return TrimTrailingWhiteSpace(line, line);
        }

        private bool TrimTrailingWhiteSpace(ITextSnapshotLine startLine, ITextSnapshotLine endLine)
        {
            Debug.Assert(startLine.LineNumber <= endLine.LineNumber);

            bool isEditMade = false;
            bool success = true;

            using (ITextEdit edit = _textView.TextBuffer.CreateEdit())
            {
                var currentSnapshot = _textView.TextBuffer.CurrentSnapshot;
                for (int i = startLine.LineNumber; i <= endLine.LineNumber; i++)
                {
                    var currentTrailingSpan = GetTrailingWhitespaceSpanToDelete(currentSnapshot.GetLineFromLineNumber(i));
                    if (currentTrailingSpan != null && currentTrailingSpan.HasValue && !currentTrailingSpan.Value.IsEmpty)
                    {
                        isEditMade = true;
                        edit.Delete(currentTrailingSpan.Value);
                    }
                }

                if (isEditMade)
                {
                    success = (edit.Apply() != currentSnapshot);
                }
            }

            return success;
        }

        private static Span? GetTrailingWhitespaceSpanToDelete(ITextSnapshotLine line)
        {
            int indexOfLastNonWhitespaceCharacter = -1;

            for (int i = line.End.Position - 1; i >= line.Start.Position; i--)
            {
                if (!char.IsWhiteSpace(line.Snapshot[i]))
                {
                    break;
                }
                indexOfLastNonWhitespaceCharacter = i;
            }

            if (indexOfLastNonWhitespaceCharacter != -1)
                return new Span(indexOfLastNonWhitespaceCharacter, line.End.Position - indexOfLastNonWhitespaceCharacter);
            return null;
        }

        /// <summary>
        /// Inserts the given text at the current caret position.
        /// </summary>
        /// <param name="text">
        /// The text to be inserted in the buffer.
        /// </param>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is null.</exception>
        public bool InsertText(string text)
        {
            return this.InsertText(text, true);
        }

        public bool InsertProvisionalText(string text)
        {
            return this.InsertText(text, false);
        }

        public ITrackingSpan ProvisionalCompositionSpan { get { return _immProvisionalComposition; } }

        public IEditorOptions Options { get { return _editorOptions; } }

        public string SelectedText
        {
            get
            {
                string text;
                if (_textView.Selection.SelectedSpans.Count > 1)
                {
                    text = string.Join(_editorOptions.GetNewLineCharacter(), _textView.Selection.SelectedSpans
                                                                                                .Select((span) => span.GetText())
                                                                                                .ToArray());

                    // Append one last newline character
                    text += _editorOptions.GetNewLineCharacter();
                }
                else
                {
                    text = _textView.Selection.StreamSelectionSpan.GetText();
                }

                return text;
            }
        }

        /// <summary>
        /// Selects the given line.
        /// </summary>
        /// <param name="extendSelection">
        /// Specifies whether the selection is to be extended or a new selection is to be made.
        /// </param>
        public void SelectLine(ITextViewLine viewLine, bool extendSelection)
        {
            if (viewLine == null)
                throw new ArgumentNullException(nameof(viewLine));

            SnapshotPoint anchor;
            SnapshotPoint active;
            if (!extendSelection || _textView.Selection.IsEmpty)
            {
                // This is always going to be a forward span
                anchor = viewLine.Start;
                active = viewLine.EndIncludingLineBreak;
            }
            else
            {
                ITextViewLine anchorLine = _textView.GetTextViewLineContainingBufferPosition(_textView.Selection.AnchorPoint.Position);
                if (_textView.Selection.IsReversed && (!_textView.Selection.AnchorPoint.IsInVirtualSpace) &&
                    (_textView.Selection.AnchorPoint.Position == anchorLine.Start) &&
                    anchorLine.Start.Position > 0)
                {
                    //In a reversed selection, an anchor that starts at the begining of a line really corresponds
                    //to the end of the previous line.
                    anchorLine = _textView.GetTextViewLineContainingBufferPosition(anchorLine.Start - 1);
                }

                if (viewLine.Start < anchorLine.Start)
                {
                    //Grow the selection (creating a reversed selection)
                    anchor = anchorLine.EndIncludingLineBreak;
                    active = viewLine.Start;
                }
                else
                {
                    anchor = anchorLine.Start;
                    active = viewLine.EndIncludingLineBreak;
                }
            }

            //Only try and show the caret, not the entire selection.
            this.SelectAndMoveCaret(new VirtualSnapshotPoint(anchor), new VirtualSnapshotPoint(active), TextSelectionMode.Stream, null);
            _textView.Caret.EnsureVisible();
        }

        /// <summary>
        /// Resets any selection in the text.
        /// </summary>
        public void ResetSelection()
        {
            _textView.Selection.Clear();
        }

        /// <summary>
        /// Deletes the selection, if present, or the next character in the text buffer.
        /// </summary>
        public bool Delete()
        {
            bool success = true;

            if (WillDeleteCreateEdit())
            {
                var selections = _multiSelectionBroker.AllSelections;
                var boxSelection = _multiSelectionBroker.BoxSelection;
                var primarySelection = _multiSelectionBroker.PrimarySelection;

                Func<bool> action = () =>
                {
                    using (_multiSelectionBroker.BeginBatchOperation())
                    {
                        if (TryDeleteEdit(selections))
                        {
                            return TryPostDeleteSelectionUpdate(selections, primarySelection, boxSelection);
                        }
                    }
                    return false;
                };

                success = ExecuteAction(Strings.DeleteCharToRight, action, SelectionUpdate.Ignore, ensureVisible: false);
            }
            else
            {
                success = TryDeleteSelections();
            }

            if (success)
            {
                _multiSelectionBroker.TryEnsureVisible(_multiSelectionBroker.PrimarySelection, EnsureSpanVisibleOptions.MinimumScroll);
            }

            return success;
        }

        private bool TryDeleteSelections()
        {
            if (_multiSelectionBroker.IsBoxSelection && _multiSelectionBroker.PrimarySelection.InsertionPoint.IsInVirtualSpace)
            {
                _multiSelectionBroker.SetSelection(new Selection(_multiSelectionBroker.PrimarySelection.Start));
            }
            else if (!_multiSelectionBroker.IsBoxSelection)
            {
                _multiSelectionBroker.PerformActionOnAllSelections(transformer =>
                {
                    if (!transformer.Selection.IsEmpty)
                    {
                        transformer.MoveTo(transformer.Selection.Start, select: false, PositionAffinity.Successor);
                    }
                });
            }

            return true;
        }

        private bool TryPostDeleteSelectionUpdate(IReadOnlyList<Selection> selections, Selection primarySelection, Selection boxSelection)
        {
            // Throughout this method, the parameters passed in are the OLD values, and the parameters on _multiSelectionBroker are the NEW ones
            if (boxSelection != Selection.Invalid)
            {
                // If this is an empty box, we may need to capture the new active/anchor points, as points in virtual space
                // won't track as we want them to through the edit.
                VirtualSnapshotPoint anchorPoint = _multiSelectionBroker.BoxSelection.AnchorPoint;
                VirtualSnapshotPoint activePoint = _multiSelectionBroker.BoxSelection.ActivePoint;

                if (primarySelection.IsEmpty)
                {
                    if (boxSelection.AnchorPoint.IsInVirtualSpace)
                    {
                        anchorPoint = new VirtualSnapshotPoint(_multiSelectionBroker.BoxSelection.AnchorPoint.Position, boxSelection.AnchorPoint.VirtualSpaces);
                    }
                    if (boxSelection.ActivePoint.IsInVirtualSpace)
                    {
                        activePoint = new VirtualSnapshotPoint(_multiSelectionBroker.BoxSelection.ActivePoint.Position, boxSelection.ActivePoint.VirtualSpaces);
                    }
                }
                else
                {
                    // Just take the starting points in the first and last selections
                    activePoint = selections[boxSelection.IsReversed ? 0 : selections.Count - 1].Start;
                    anchorPoint = selections[boxSelection.IsReversed ? selections.Count - 1 : 0].Start;
                }

                VirtualSnapshotPoint newAnchor = anchorPoint.TranslateTo(_textView.TextSnapshot);
                VirtualSnapshotPoint newActive = activePoint.TranslateTo(_textView.TextSnapshot);

                var newSelection = new Selection(insertionPoint: newActive, anchorPoint: newAnchor, activePoint: newActive, boxSelection.InsertionPointAffinity);
                if (_multiSelectionBroker.BoxSelection != newSelection)
                {
                    _multiSelectionBroker.SetBoxSelection(newSelection);
                }
            }
            else
            {
                // Perf: This is actually an n^2 algorithm here, since TryPerform... also loops through all the selections. Try to avoid copying this code
                // elsewhere. We need it here because we're actually modifying each one based on its context AND because merges can happen with backspace so we
                // can't do anything funny like caching the transformers.
                for (int i = 0; i < selections.Count; i++)
                {
                    //Some could have merged away, ignore return values here intentionally.
                    _multiSelectionBroker.TryPerformActionOnSelection(selections[i], transformer =>
                    {
                        // We can't use the virtual snapshot point TranslateTo since it will remove the virtual space (because the line's line break was deleted).
                        // VirtualSnapshotPoint.TranslateTo doesn't know what to do with virtual whitespace, so we have to do this ourselves.
                        if (selections[i].IsEmpty && selections[i].InsertionPoint.IsInVirtualSpace)
                        {
                            // Move the caret back one if we have an empty selection
                            transformer.MoveTo(new VirtualSnapshotPoint(transformer.Selection.InsertionPoint.Position, selections[i].InsertionPoint.VirtualSpaces - 1),
                                select: false,
                                insertionPointAffinity: PositionAffinity.Successor);
                        }
                        else
                        {
                            //Move the caret to the start of the selection.
                            transformer.MoveTo(new VirtualSnapshotPoint(transformer.Selection.InsertionPoint.Position, selections[i].Start.VirtualSpaces),
                            select: false,
                            PositionAffinity.Successor);
                        }
                    }, out _);
                }
            }

            return true;
        }

        private bool TryDeleteEdit(IReadOnlyList<Selection> selections)
        {
            using (var edit = _textView.TextBuffer.CreateEdit())
            {
                for (int i = (selections.Count - 1); i >= 0; i--)
                {
                    var selection = selections[i];

                    if (selection.IsEmpty)
                    {
                        if (_multiSelectionBroker.IsBoxSelection)
                        {
                            var endOfLine = selection.InsertionPoint.Position.GetContainingLine().End;

                            if (selection.InsertionPoint.Position == endOfLine)
                            {
                                continue;
                            }
                        }

                        if (selection.InsertionPoint.IsInVirtualSpace)
                        {
                            var whitespace = GetWhitespaceForVirtualSpace(selection.InsertionPoint);
                            var span = _textView.GetTextElementSpan(selection.InsertionPoint.Position);

                            if (!edit.Replace(span, whitespace))
                            {
                                return false;
                            }
                        }
                        else if (!edit.Delete(_textView.GetTextElementSpan(selection.InsertionPoint.Position)))
                        {
                            return false;
                        }
                    }
                    else if (!edit.Delete(selection.Extent.SnapshotSpan))
                    {
                        return false;
                    }
                }

                edit.Apply();
                return !edit.Canceled;
            }
        }

        private bool WillDeleteCreateEdit()
        {
            var selections = _multiSelectionBroker.AllSelections;

            if (_multiSelectionBroker.IsBoxSelection)
            {
                // Edits can not happen if we're a box selection at the end of every line
                for (int i = 0; i < selections.Count; i++)
                {
                    if (selections[i].Start.Position < selections[i].Start.Position.GetContainingLine().End)
                    {
                        return true;
                    }
                }
            }
            else
            {
                for (int i = 0; i < selections.Count; i++)
                {
                    if ((!selections[i].Extent.SnapshotSpan.IsEmpty) ||
                        (selections[i].IsEmpty && selections[i].InsertionPoint.Position.Position != _multiSelectionBroker.CurrentSnapshot.Length))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Replace text selection with the new text.
        /// </summary>
        /// <param name="text">
        /// The new text that will replace the old selection.
        /// </param>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is null.</exception>
        public bool ReplaceSelection(string text)
        {
            // Validate
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            Func<bool> action = () =>
            {
                return ReplaceHelper(_textView.Selection.VirtualSelectedSpans, text);
            };

            return ExecuteAction(Strings.ReplaceSelectionWith + text, action);
        }

        /// <summary>
        /// Replace text from the given span with the new text.
        /// </summary>
        /// <param name="span">
        /// The span of text to replace.
        /// </param>
        /// <param name="text">
        /// The new text that will replace the old selection.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="span"/> is greater than the length in the TextBuffer.</exception>
        public bool ReplaceText(Span span, string text)
        {
            // Validate
            if (span.End > _textView.TextSnapshot.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(span));
            }

            Func<bool> action = () =>
            {
                return ReplaceHelper(span, text);
            };

            return ExecuteAction(Strings.ReplaceText, action, SelectionUpdate.Reset, true);
        }

        /// <summary>
        /// Replaces all matching occurrences of the given string.
        /// </summary>
        /// <param name="searchText">
        /// Text to match.
        /// </param>
        /// <param name="replaceText">
        /// Text used in replace.
        /// </param>
        /// <param name="matchCase">
        /// True if search should match case, false otherwise.
        /// </param>
        /// <param name="matchWholeWord">
        /// True if search should match whole word, false otherwise.
        /// </param>
        /// <param name="useRegularExpressions">
        /// True if search should use regular expression, false otherwise.
        /// </param>
        /// <exception cref="ArgumentNullException"><paramref name="searchText"/> is null.</exception>
        /// <remarks>If one of the matches found is read only, none of the matches will be replaced.</remarks>
        public int ReplaceAllMatches(string searchText, string replaceText, bool matchCase, bool matchWholeWord, bool useRegularExpressions)
        {
            if (searchText == null)
            {
                throw new ArgumentNullException(nameof(searchText));
            }

            FindData findData = new FindData(searchText, _textView.TextSnapshot);
            findData.TextStructureNavigator = _textStructureNavigator;
            if (matchCase)
            {
                findData.FindOptions = findData.FindOptions | FindOptions.MatchCase;
            }
            if (matchWholeWord)
            {
                findData.FindOptions = findData.FindOptions | FindOptions.WholeWord;
            }
            if (useRegularExpressions)
            {
                findData.FindOptions = findData.FindOptions | FindOptions.UseRegularExpressions;
            }

            int numberOfReplacementsMade = 0;

            // Now, use FindLogic to find all matching occurrences
            Collection<SnapshotSpan> textSpanMatches = _factory.TextSearchService.FindAll(findData);

            if (textSpanMatches != null && textSpanMatches.Count > 0)
            {
                using (ITextUndoTransaction undoTransaction = _undoHistory.CreateTransaction(Strings.ReplaceAll))
                {
                    AddBeforeTextBufferChangePrimitive();

                    bool replaceFailed = false;
                    using (ITextEdit textEdit = _textView.TextBuffer.CreateEdit())
                    {
                        // If we are not using regular expressions, simply replace all matches with replaceText
                        if (!useRegularExpressions)
                        {
                            // Perform each replace, and create an undo unit to track its changes
                            foreach (SnapshotSpan snapSpan in textSpanMatches)
                            {
                                if (!textEdit.Replace(snapSpan, replaceText))
                                {
                                    replaceFailed = true;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            // Since we are using regular expressions, each replace text may be dependent on the matching text.
                            // Therefore, bring up a regular expression engine, and for each match, we will take that text and use Regex.Replace() on the string.
                            Regex regex = new Regex(searchText, (!matchCase ? RegexOptions.IgnoreCase : 0));

                            foreach (SnapshotSpan textSpan in textSpanMatches)
                            {
                                string newText = regex.Replace(textSpan.GetText(), replaceText);
                                if (!textEdit.Replace(textSpan, newText))
                                {
                                    replaceFailed = true;
                                    break;
                                }
                            }
                        }

                        AddAfterTextBufferChangePrimitive();

                        if (!replaceFailed)
                        {
                            textEdit.Apply();

                            if (!textEdit.Canceled)
                            {
                                numberOfReplacementsMade = textSpanMatches.Count;
                                undoTransaction.Complete();
                            }
                        }
                    }
                }
            }

            return numberOfReplacementsMade;
        }

        /// <summary>
        /// Copies the selected text to clip board.
        /// </summary>
        /// <exception cref="InsufficientMemoryException"> is thrown if there is not sufficient memory to complete the operation.</exception>
        public bool CopySelection()
        {
            if (!_textView.Selection.IsEmpty)
            {
                return PrepareClipboardSelectionCopy().Invoke();
            }
            else if (!IsPointOnBlankViewLine(_editorPrimitives.Caret) ||
                    (_editorOptions.GetOptionValue<bool>(DefaultTextViewOptions.CutOrCopyBlankLineIfNoSelectionId)))
            {
                return PrepareClipboardFullLineCopy(GetFullLines()).Invoke();
            }
            else
            {
                return true;
            }
        }

        public bool CutFullLine()
        {
            DisplayTextRange fullSelectedLines = GetFullLines();

            if (!fullSelectedLines.TextBuffer.AdvancedTextBuffer.IsReadOnly(fullSelectedLines.AdvancedTextRange))
            {
                Func<bool> putCopiedLineToClipboard = PrepareClipboardFullLineCopy(fullSelectedLines);

                return ExecuteAction(Strings.CutLine, () => fullSelectedLines.Delete() && putCopiedLineToClipboard.Invoke());
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Cuts the selected text.
        /// </summary>
        /// <exception cref="InsufficientMemoryException"> is thrown if there is not sufficient memory to complete the operation.</exception>
        public bool CutSelection()
        {
            // no-op if can't do any cut operation
            if (_textView.Selection.IsEmpty && !CanCut)
                return true;

            using (ITextUndoTransaction undoTransaction = _undoHistory.CreateTransaction(Strings.CutSelection))
            {
                this.AddBeforeTextBufferChangePrimitive();

                bool performedCut = false;

                if (!_textView.Selection.IsEmpty)
                {
                    Func<bool> putCopiedSelectionToClipboard = PrepareClipboardSelectionCopy();

                    performedCut = this.Delete() && putCopiedSelectionToClipboard.Invoke();
                }
                else
                {
                    DisplayTextRange lineRange = GetFullLines();
                    double caretX = _textView.Caret.Left;
                    Func<bool> putCopiedLineToClipboard = PrepareClipboardFullLineCopy(lineRange);

                    if (lineRange.Delete() && putCopiedLineToClipboard.Invoke())
                    {
                        performedCut = true;

                        // Move the caret back to the starting x coordinate on it's current line
                        _textView.Caret.MoveTo(_textView.Caret.ContainingTextViewLine, caretX);
                    }
                }

                if (performedCut)
                {
                    _textView.Caret.EnsureVisible();
                    this.AddAfterTextBufferChangePrimitive();
                    undoTransaction.Complete();
                }
                else
                {
                    undoTransaction.Cancel();
                }

                return performedCut;
            }
        }

        /// <summary>
        /// Pastes text from the clipboard to the text buffer.
        /// </summary>
        public bool Paste()
        {
            string text = null;
            bool dataHasLineCutCopyTag = false;
            bool dataHasBoxCutCopyTag = false;

            // Clipboard may throw exceptions, so enclose Clipboard calls in a try-catch block
            try
            {
                IDataObject dataObj = Clipboard.GetDataObject();

                if (dataObj == null || !dataObj.GetDataPresent(typeof(string)))
                {
                    return true;
                }

                text = (string) dataObj.GetData(DataFormats.UnicodeText);
                if (text == null)
                {
                    text = (string) dataObj.GetData(DataFormats.Text);
                }

                dataHasLineCutCopyTag = dataObj.GetDataPresent(_clipboardLineBasedCutCopyTag);
                dataHasBoxCutCopyTag = dataObj.GetDataPresent(_boxSelectionCutCopyTag);
            }
            catch (System.Runtime.InteropServices.ExternalException)
            {
                // TODO: Log error
                return false;
            }
            catch (OutOfMemoryException)
            {
                // silently fail on out of memory exceptions.
                // the clipboard also throws out of memory exceptions when the data in the clipboard is corrupt
                // see bug 780687
                return false;
            }

            if (text != null)
            {
                if (dataHasLineCutCopyTag && _textView.Selection.IsEmpty)
                {
                    //this only applies if the data was copied from the editor itself in the special cut/copy 
                    //mode when the caret is placed on a line and it's copied/cut without any selection
                    using (ITextUndoTransaction undoTransaction = _undoHistory.CreateTransaction(Strings.Paste))
                    {
                        this.AddBeforeTextBufferChangePrimitive();

                        SnapshotPoint insertionPoint = _textView.Caret.Position.BufferPosition.GetContainingLine().Start;

                        using (ITextEdit edit = _textView.TextBuffer.CreateEdit())
                        {
                            if (!edit.Insert(insertionPoint.Position, text))
                                return false;

                            edit.Apply();
                        }

                        this.AddAfterTextBufferChangePrimitive();

                        undoTransaction.Complete();

                        _textView.Caret.EnsureVisible();

                        return true;
                    }
                }
                else if (dataHasBoxCutCopyTag)
                {
                    // If the caret is on a blank line, treat this almost like a normal stream
                    // insertion, but with extra whitespace on the beginning of each line to
                    // correctly maintain column position.
                    if (_textView.Selection.IsEmpty && IsPointOnBlankViewLine(_editorPrimitives.Caret))
                    {
                        // We want each line to be at the column position of the caret, which is
                        // the editor primitives caret column + virtual spaces.
                        string whitespace = GetWhitespaceForDisplayColumn(_editorPrimitives.Caret.Column + _textView.Caret.Position.VirtualBufferPosition.VirtualSpaces);

                        // Collect the lines from the clipboard
                        List<string> lines = new List<string>();
                        using (StringReader reader = new StringReader(text))
                        {
                            for (string lineText = reader.ReadLine(); lineText != null; lineText = reader.ReadLine())
                            {
                                lines.Add(lineText);
                            }
                        }

                        string streamText = string.Join(_editorOptions.GetNewLineCharacter() + whitespace, lines);
                        return this.InsertText(streamText.ToString(CultureInfo.CurrentCulture), true, Strings.Paste, isOverwriteModeEnabled: false);
                    }
                    else
                    {
                        VirtualSnapshotPoint unusedStart, unusedEnd;

                        return this.InsertTextAsBox(text, out unusedStart, out unusedEnd, Strings.Paste);
                    }
                }
                else
                {
                    return this.InsertText(text, true, Strings.Paste, isOverwriteModeEnabled: false);
                }
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Gets whether or not a paste operation can happen.
        /// </summary>
        public bool CanPaste
        {
            get
            {
                // Clipboard may throw exceptions, so enclose Clipboard calls in a try-catch block
                try
                {
                    return Clipboard.ContainsText() && !_textView.TextSnapshot.TextBuffer.IsReadOnly(_editorPrimitives.Caret.CurrentPosition);
                }
                catch (System.Runtime.InteropServices.ExternalException)
                {
                    // TODO: Log error
                    return false;
                }
            }
        }

        public bool CanDelete
        {
            get
            {
                if (_editorPrimitives.Selection.IsEmpty)
                {
                    if (_editorPrimitives.Caret.CurrentPosition < _textView.TextSnapshot.Length)
                    {
                        return !_textView.TextSnapshot.TextBuffer.IsReadOnly(_editorPrimitives.Caret.CurrentPosition);
                    }

                    return false;
                }
                else
                {
                    return !_editorPrimitives.Selection.AdvancedTextRange.Snapshot.TextBuffer.IsReadOnly(_editorPrimitives.Selection.AdvancedTextRange);
                }
            }
        }

        public bool CanCut
        {
            get
            {
                if (_editorPrimitives.Selection.IsEmpty)
                {
                    if (!IsPointOnBlankViewLine(_editorPrimitives.Caret) ||
                        _editorOptions.GetOptionValue<bool>(DefaultTextViewOptions.CutOrCopyBlankLineIfNoSelectionId))
                    {
                        ITextViewLine caretLine = _editorPrimitives.Caret.AdvancedCaret.ContainingTextViewLine;
                        return !caretLine.Snapshot.TextBuffer.IsReadOnly(caretLine.ExtentIncludingLineBreak);
                    }
                }
                else
                {
                    return !_editorPrimitives.Selection.AdvancedTextRange.Snapshot.TextBuffer.IsReadOnly(_editorPrimitives.Selection.AdvancedTextRange);
                }

                return false;
            }
        }

        /// <summary>
        /// Sets the caret at the start of the specified line.
        /// </summary>
        /// <param name="lineNumber">
        /// The line number to set the caret at.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="lineNumber"/>is less than 0 or greater than the line number of the last line in the TextBuffer.</exception>
        public void GotoLine(int lineNumber)
        {
            // Validate
            if (lineNumber < 0 || lineNumber > _textView.TextSnapshot.LineCount - 1)
                throw new ArgumentOutOfRangeException(nameof(lineNumber));

            ITextSnapshotLine line = _textView.TextSnapshot.GetLineFromLineNumber(lineNumber);

            _textView.Caret.MoveTo(line.Start);
            _textView.Selection.Clear();
            _textView.ViewScroller.EnsureSpanVisible(new SnapshotSpan(_textView.Caret.Position.BufferPosition, 0));
        }

        /// <summary>
        /// The text view on which these operations work.
        /// </summary>
        public ITextView TextView
        {
            get
            {
                return _textView;
            }
        }

        /// <summary>
        /// Scrolls the view either up by one line,
        /// reposition caret only if it is scrolled off the page. 
        /// It will reposition the cursor to the newly scrolled line
        /// at the bottom of the view.
        /// </summary>
        public void ScrollUpAndMoveCaretIfNecessary()
        {
            this.ScrollByLineAndMoveCaretIfNecessary(ScrollDirection.Up);
        }

        public void SwapCaretAndAnchor()
        {
            _textView.Selection.Select(_textView.Selection.ActivePoint,
                                       _textView.Selection.AnchorPoint);
            _textView.Caret.MoveTo(_textView.Selection.ActivePoint);
            _textView.Caret.EnsureVisible();
        }

        /// <summary>
        /// Scrolls the view either up by one line,
        /// reposition caret only if it is scrolled off the page. 
        /// It will reposition the cursor to the newly scrolled line
        /// at the top of the view.
        /// </summary>
        public void ScrollDownAndMoveCaretIfNecessary()
        {
            this.ScrollByLineAndMoveCaretIfNecessary(ScrollDirection.Down);
        }

        /// <summary>
        /// Transposes character at the cursor with next character.
        /// </summary>
        public bool TransposeCharacter()
        {
            return ExecuteAction(Strings.TransposeCharacter, () => _editorPrimitives.Caret.TransposeCharacter());
        }

        /// <summary>
        /// Transposes line containing the cursor with the next line.
        /// </summary>
        public bool TransposeLine()
        {
            // Only transpose lines if there are more than 2 lines
            if (_textView.TextSnapshot.LineCount < 2)
            {
                return true;
            }

            return ExecuteAction(Strings.TransposeLine, () => _editorPrimitives.Caret.TransposeLine());
        }

        public bool TransposeWord()
        {
            TextRange currentWord = _editorPrimitives.Caret.GetCurrentWord();
            if (currentWord.IsEmpty)
            {
                currentWord = currentWord.GetStartPoint().GetPreviousWord();
            }

            if (currentWord.GetEndPoint().CurrentPosition == _editorPrimitives.Buffer.GetEndPoint().CurrentPosition)
            {
                return true;
            }

            Func<bool> action = () =>
            {
                TextRange nextWord = currentWord.GetEndPoint().GetNextWord();

                Func<TextRange, bool> keepSearching = (tr) =>
                {
                    if (tr.GetEndPoint().CurrentPosition == _editorPrimitives.Buffer.GetEndPoint().CurrentPosition)
                    {
                        return false;
                    }
                    if (!tr.IsEmpty && char.IsLetterOrDigit(tr.GetText()[0]))
                    {
                        return false;
                    }
                    return true;
                };

                while (keepSearching(nextWord))
                {
                    nextWord = nextWord.GetEndPoint().GetNextWord();
                }

                int newCaretPosition = nextWord.GetEndPoint().CurrentPosition;

                using (ITextEdit textEdit = _editorPrimitives.Buffer.AdvancedTextBuffer.CreateEdit())
                {
                    if (!textEdit.Replace(currentWord.AdvancedTextRange.Span, nextWord.GetText()))
                        return false;
                    if (!textEdit.Replace(nextWord.AdvancedTextRange.Span, currentWord.GetText()))
                        return false;

                    textEdit.Apply();

                    if (textEdit.Canceled)
                        return false;
                }

                _editorPrimitives.Caret.MoveTo(newCaretPosition);

                return true;
            };

            return ExecuteAction(Strings.TransposeWord, action);
        }

        /// <summary>
        /// Converts letters to the specified case in select text or next character to the cursor if select is empty.
        /// </summary>
        /// <param name="letterCase">
        /// The letter case to convert to.
        /// </param>
        private bool ChangeCase(LetterCase letterCase)
        {
            NormalizedSnapshotSpanCollection spans;
            SelectionUpdate selectionUpdate;
            if (_textView.Selection.IsEmpty)
            {
                // Do nothing if caret is at the end of buffer and there is no selection
                if (_textView.Caret.Position.BufferPosition == _textView.TextSnapshot.Length)
                    return true;

                // If the caret is at the physical end of a line, just move to the next line.
                VirtualSnapshotPoint caret = _textView.Caret.Position.VirtualBufferPosition;
                ITextSnapshotLine line = _textView.TextSnapshot.GetLineFromPosition(caret.Position);

                if (line.End == caret.Position)
                {
                    _textView.Caret.MoveTo(line.EndIncludingLineBreak);
                    return true;
                }

                spans = new NormalizedSnapshotSpanCollection(_textView.GetTextElementSpan(caret.Position));
                selectionUpdate = SelectionUpdate.Ignore;
            }
            else
            {
                spans = _textView.Selection.SelectedSpans;
                selectionUpdate = SelectionUpdate.Preserve;
            }

            Func<bool> action = () =>
            {
                return EditHelper((edit) =>
                {
                    foreach (var span in spans)
                    {
                        string textToConvert = span.GetText();
                        string convertedCaseText = (letterCase == LetterCase.Uppercase)
                                   ? textToConvert.ToUpper(System.Globalization.CultureInfo.CurrentCulture)
                                   : textToConvert.ToLower(System.Globalization.CultureInfo.CurrentCulture);

                        if (!edit.Replace(span, convertedCaseText))
                            return false;
                    }

                    return true;
                });

            };
            return this.ExecuteAction((letterCase == LetterCase.Uppercase ? Strings.MakeUppercase : Strings.MakeLowercase), action, selectionUpdate, true);
        }

        /// <summary>
        /// Converts lowercase letters to uppercase in select text or next character to the cursor if select is empty.
        /// </summary>
        public bool MakeUppercase()
        {
            return ChangeCase(LetterCase.Uppercase);
        }

        /// <summary>
        /// Converts uppercase letters to lowercase in select text or next character to the cursor if select is empty.
        /// </summary>
        public bool MakeLowercase()
        {
            return ChangeCase(LetterCase.Lowercase);
        }

        public bool Capitalize()
        {
            return ExecuteAction(Strings.Capitalize, () => _editorPrimitives.Selection.Capitalize());
        }

        public bool ToggleCase()
        {
            return ExecuteAction(Strings.ToggleCase, () => _editorPrimitives.Selection.ToggleCase());
        }

        public bool InsertFile(string filePath)
        {
            Func<bool> action =
                () =>
                {
                    IContentType contentTypeInert = _factory.ContentTypeRegistryService.GetContentType("inert");
                    bool unused;
                    ITextDocument document = _factory.TextDocumentFactoryService.CreateAndLoadTextDocument(filePath, contentTypeInert, attemptUtf8Detection: true, characterSubstitutionsOccurred: out unused);
                    ITrackingPoint oldCaretLocation = _textView.TextSnapshot.CreateTrackingPoint(_editorPrimitives.Caret.CurrentPosition, PointTrackingMode.Negative);
                    if (!_editorPrimitives.Caret.InsertText(document.TextBuffer.CurrentSnapshot.GetText()))
                        return false;
                    _editorPrimitives.Caret.MoveTo(oldCaretLocation.GetPosition(_textView.TextSnapshot));
                    return true;
                };

            return ExecuteAction(Strings.InsertFile, action);
        }

        public void ScrollPageUp()
        {
            _editorPrimitives.View.ScrollPageUp();
        }

        public void ScrollPageDown()
        {
            _editorPrimitives.View.ScrollPageDown();
        }

        public void ScrollColumnLeft()
        {
            if (_textView.ViewScroller is IViewScroller2 viewScroller)
            {
                // A column is defined as the width of a space in the default font
                viewScroller.ScrollColumnLeft();
            }
        }

        public void ScrollColumnRight()
        {
            if (_textView.ViewScroller is IViewScroller2 viewScroller)
            {
                // A column is defined as the width of a space in the default font
                viewScroller.ScrollColumnRight();
            }
        }

        public void ScrollLineBottom()
        {
            _editorPrimitives.View.MoveLineToBottom(_editorPrimitives.Caret.LineNumber);
        }

        public void ScrollLineTop()
        {
            _editorPrimitives.View.MoveLineToTop(_editorPrimitives.Caret.LineNumber);
        }

        public void ScrollLineCenter()
        {
            _editorPrimitives.View.Show(_editorPrimitives.Caret, HowToShow.Centered);
        }

        public void AddAfterTextBufferChangePrimitive()
        {
            // Create an AfterTextBufferChangeUndoPrimitive undo primitive using the current caret position and selection.
            AfterTextBufferChangeUndoPrimitive afterTextBufferChangeUndoPrimitive =
                AfterTextBufferChangeUndoPrimitive.Create(_textView, _undoHistory);
            _undoHistory.CurrentTransaction.AddUndo(afterTextBufferChangeUndoPrimitive);
        }

        public void AddBeforeTextBufferChangePrimitive()
        {
            // Create a BeforeTextBufferChangeUndoPrimitive undo primitive using the current caret position and selection.
            BeforeTextBufferChangeUndoPrimitive beforeTextBufferChangeUndoPrimitive =
                BeforeTextBufferChangeUndoPrimitive.Create(_textView, _undoHistory);
            _undoHistory.CurrentTransaction.AddUndo(beforeTextBufferChangeUndoPrimitive);
        }

        public bool CanZoomIn => CanZoomTo && _factory.ZoomManager.ZoomLevel(_textView) < _textView.Options.GlobalOptions.MaxZoom();

        public bool CanZoomOut => CanZoomTo && _factory.ZoomManager.ZoomLevel(_textView) > _textView.Options.GlobalOptions.MinZoom();

        public bool CanZoomTo => _textView.Roles.Contains(PredefinedTextViewRoles.Zoomable);

        public bool CanZoomReset => CanZoomTo && _factory.ZoomManager.ZoomLevel(_textView) != ZoomConstants.DefaultZoom;

        public void ZoomReset()
        {
            if (CanZoomReset)
            {
                ZoomTo(ZoomConstants.DefaultZoom);
            }
        }

        public void ZoomIn()
        {
            _factory.ZoomManager.ZoomIn(_textView);
        }

        public void ZoomOut()
        {
            _factory.ZoomManager.ZoomOut(_textView);
        }

        public void ZoomTo(double zoomLevel)
        {
            _factory.ZoomManager.ZoomTo(_textView, zoomLevel);
        }

        #endregion // IEditorOperations Members

        #region Virtual Space to Whitespace helpers

        public string GetWhitespaceForVirtualSpace(VirtualSnapshotPoint point)
        {
            if (!point.IsInVirtualSpace)
                return string.Empty;

            return GetWhiteSpaceForPositionAndVirtualSpace(point.Position, point.VirtualSpaces);
        }

        internal string GetWhiteSpaceForPositionAndVirtualSpace(SnapshotPoint position, int virtualSpaces)
        {
            return GetWhiteSpaceForPositionAndVirtualSpace(position, virtualSpaces, useBufferPrimitives: false);
        }

        internal string GetWhiteSpaceForPositionAndVirtualSpace(SnapshotPoint position, int virtualSpaces, bool useBufferPrimitives)
        {
            return GetWhiteSpaceForPositionAndVirtualSpace(position, virtualSpaces, useBufferPrimitives, convertTabsToSpaces: _textView.Options.IsConvertTabsToSpacesEnabled());
        }

        /// <summary>
        /// Given a buffer point and a count of virtual spaces, determine the correct whitespace, as a sequence of tabs/spaces, to get
        /// from the buffer point to the virtual point.  Note that the buffer position doesn't have to be at the end of the line, so this
        /// method can be used to calculate whitespace offset in arbitrary contexts.
        /// </summary>
        /// <param name="position">The buffer position to start from.</param>
        /// <param name="virtualSpaces">The count of virtual spaces to generate whitespace for.</param>
        /// <param name="useBufferPrimitives">Whether to use buffer or view primitives.  Buffer primitives must be used if the given
        /// <paramref name="position"/> could be hidden in the view.</param>
        /// <param name="convertTabsToSpaces">Whether tabs or spaces should be use.</param>
        /// <returns>A non-null string of whitespace.</returns>
        internal string GetWhiteSpaceForPositionAndVirtualSpace(SnapshotPoint position, int virtualSpaces, bool useBufferPrimitives, bool convertTabsToSpaces)
        {
            string textToInsert;
            if (!convertTabsToSpaces)
            {
                int tabSize = _textView.Options.GetTabSize();

                int columnOfEndOfLine;
                if (useBufferPrimitives)
                    columnOfEndOfLine = _editorPrimitives.Buffer.GetTextPoint(position).Column;
                else
                    columnOfEndOfLine = _editorPrimitives.View.GetTextPoint(position).DisplayColumn;

                int caretColumn = columnOfEndOfLine + virtualSpaces;

                int spacesAfterPreviousTabStop = caretColumn % tabSize;
                int columnOfPreviousTabStop = caretColumn - spacesAfterPreviousTabStop;

                int requiredTabs = ((columnOfPreviousTabStop - columnOfEndOfLine) + tabSize - 1) / tabSize;

                if (requiredTabs > 0)
                    textToInsert = new string('\t', requiredTabs) + new string(' ', spacesAfterPreviousTabStop);
                else
                    textToInsert = new string(' ', virtualSpaces);
            }
            else
            {
                textToInsert = new string(' ', virtualSpaces);
            }

            return textToInsert;
        }

        internal string GetWhitespaceForDisplayColumn(int caretColumn)
        {
            string textToInsert;
            if (!_textView.Options.IsConvertTabsToSpacesEnabled())
            {
                int tabSize = _textView.Options.GetTabSize();

                int spacesToInsert = caretColumn % tabSize;
                textToInsert = new string(' ', spacesToInsert);

                int columnOfPreviousTabStop = caretColumn - spacesToInsert;

                int requiredTabs = ((columnOfPreviousTabStop) + tabSize - 1) / tabSize;

                if (requiredTabs > 0)
                    textToInsert = new string('\t', requiredTabs) + textToInsert;
            }
            else
            {
                textToInsert = new string(' ', caretColumn);
            }

            return textToInsert;
        }

        #endregion

        #region Text insertion helpers

        private bool InsertText(string text, bool final)
        {
            return this.InsertText(text, final, Strings.InsertText, _textView.Caret.OverwriteMode);
        }

        private bool InsertText(string text, bool final, string undoText, bool isOverwriteModeEnabled)
        {
            // Validate
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            if ((text.Length == 0) && !final)
            {
                throw new ArgumentException("Provisional TextInput cannot be zero-length");
            }

            using (ITextUndoTransaction undoTransaction = _undoHistory.CreateTransaction(undoText))
            {
                // Determine undo merge direction(s).  By default we allow merging, but in two cases we don't want to merge
                // with previous transactions:
                // 1. If the document is clean (else you won't be able to undo back to a clean state (Dev10 672382)).
                // 2. If we are replacing selected text (replacing selected text should be an atomic undo operation).
                var mergeDirections = TextTransactionMergeDirections.Forward | TextTransactionMergeDirections.Backward;
                if ((!_textView.Selection.IsEmpty && !IsEmptyBoxSelection()) // replacing selected text.
                    || (_textDocument != null && !_textDocument.IsDirty))    // document is clean.
                {
                    mergeDirections = TextTransactionMergeDirections.Forward;
                }
                undoTransaction.MergePolicy = new TextTransactionMergePolicy(mergeDirections);

                this.AddBeforeTextBufferChangePrimitive();

                IEnumerable<VirtualSnapshotSpan> replaceSpans;

                TextEditAction action = TextEditAction.Type;

                // If this is a non-empty stream selection and *not* an empty box
                if (!_textView.Selection.IsEmpty && _immProvisionalComposition == null)
                {
                    // If the caret is in overwrite mode and this is an empty box, treat
                    // it like an overwrite on each line.
                    if (_textView.Options.IsOverwriteModeEnabled() && IsEmptyBoxSelection())
                    {
                        List<VirtualSnapshotSpan> spans = new List<VirtualSnapshotSpan>();

                        foreach (var span in _textView.Selection.VirtualSelectedSpans)
                        {
                            SnapshotPoint startPoint = span.Start.Position;

                            if (span.Start.IsInVirtualSpace ||
                                startPoint.GetContainingLine().End == startPoint)
                            {
                                spans.Add(span);
                            }
                            else
                            {
                                spans.Add(new VirtualSnapshotSpan(
                                       new SnapshotSpan(startPoint, _textView.GetTextElementSpan(startPoint).End)));
                            }
                        }

                        replaceSpans = spans;
                    }
                    else
                    {
                        replaceSpans = _multiSelectionBroker.VirtualSelectedSpans;
                    }

                    // The provisional composition span should be null here (the IME should
                    // finalize before doing anything that could cause a selection) but we treat
                    // this as a soft error since something time sliced on the UI thread could
                    // affect the selection. So we simply ignore the old provisional span.
                }
                else if (_immProvisionalComposition != null)
                {
                    SnapshotSpan provisionalSpan = _immProvisionalComposition.GetSpan(_textView.TextSnapshot);

                    if (IsEmptyBoxSelection() && final)
                    {
                        // For an empty box, replace the equivalent to the provisional span
                        // on each line
                        IList<VirtualSnapshotSpan> spans = new List<VirtualSnapshotSpan>();
                        foreach (SnapshotPoint start in _textView.Selection.VirtualSelectedSpans.Select(s => s.Start.Position))
                        {
                            if (start.Position - provisionalSpan.Length >= 0)
                            {
                                spans.Add(new VirtualSnapshotSpan(
                                        new SnapshotSpan(start - provisionalSpan.Length, start)));
                            }
                            else
                            {
                                Debug.Fail("Asked to replace a provisional span that would go past the beginning of the buffer; ignoring.");
                            }
                        }

                        replaceSpans = spans;
                    }
                    else
                    {
                        replaceSpans = new VirtualSnapshotSpan[] {
                            new VirtualSnapshotSpan(provisionalSpan) };
                    }

                    // The length of the replacement should always be > 0 but there are scenarios
                    // (such as a replace by some asynchronous command time-sliced to the UI thread)
                    // that could zero out the span.

                    action = TextEditAction.ProvisionalOverwrite;
                }
                else
                {
                    var spans = new List<VirtualSnapshotSpan>();
                    foreach (var caret in _multiSelectionBroker.GetSelectionsIntersectingSpan(new SnapshotSpan(_multiSelectionBroker.CurrentSnapshot, 0, _multiSelectionBroker.CurrentSnapshot.Length)))
                    {
                        var insertionPoint = caret.InsertionPoint;
                        if (isOverwriteModeEnabled && !insertionPoint.IsInVirtualSpace)
                        {
                            SnapshotPoint point = insertionPoint.Position;
                            spans.Add(new VirtualSnapshotSpan(new SnapshotSpan(point, _textView.GetTextElementSpan(point).End)));
                        }
                        else
                        {
                            spans.Add(new VirtualSnapshotSpan(insertionPoint, insertionPoint));
                        }
                    }
                    replaceSpans = spans;
                }

                ITextVersion currentVersion = _textView.TextSnapshot.Version;
                ITrackingSpan newProvisionalSpan = null;

                bool editSuccessful = true;

                int? firstLineInsertedVirtualSpaces = null;
                int lastLineInsertedVirtualSpaces = 0;

                ITrackingPoint endPoint = null;

                using (ITextEdit textEdit = _textView.TextBuffer.CreateEdit(EditOptions.None, null, action))
                {
                    bool firstSpan = true;
                    foreach (var span in replaceSpans)
                    {
                        string lineText = text;

                        // Remember the endPoint for a possible provisional span.
                        // Use negative tracking, so it doesn't move with the newly inserted text.
                        endPoint = _textView.TextSnapshot.CreateTrackingPoint(span.Start.Position, PointTrackingMode.Negative);

                        if (span.Start.IsInVirtualSpace)
                        {
                            string whitespace = GetWhitespaceForVirtualSpace(span.Start);
                            if (firstSpan)
                            {
                                //Keep track of the number of whitespace characters -- which might include tabs -- so that the caller can figure out where the inserted text
                                //actually lands.
                                _textView.TextBuffer.Properties["WhitespaceInserted"] = whitespace.Length;
                            }
                            lineText = whitespace + text;
                        }

                        if (!firstLineInsertedVirtualSpaces.HasValue)
                            firstLineInsertedVirtualSpaces = lineText.Length - text.Length;

                        lastLineInsertedVirtualSpaces = lineText.Length - text.Length;

                        if (!textEdit.Replace(span.SnapshotSpan, lineText) || textEdit.Canceled)
                        {
                            editSuccessful = false;
                            break;
                        }

                        firstSpan = false;
                    }

                    if (editSuccessful)
                    {
                        textEdit.Apply();
                        editSuccessful = !textEdit.Canceled;
                    }
                }

                if (editSuccessful)
                {
                    if (_multiSelectionBroker.IsBoxSelection)
                    {
                        _textView.Caret.MoveTo(_textView.Caret.Position.BufferPosition);
                        _textView.Selection.Select(
                                new VirtualSnapshotPoint(_textView.Selection.AnchorPoint.Position),
                                new VirtualSnapshotPoint(_textView.Selection.ActivePoint.Position));

                        // If the selection ends up being non-empty (meaning not an empty
                        // single selection *or* an empty box), then clear it.
                        if (_textView.Selection.VirtualSelectedSpans.Any(s => !s.IsEmpty))
                            _textView.Selection.Clear();

                    }
                    else
                    {
                        _multiSelectionBroker.PerformActionOnAllSelections(transformer =>
                        {
                            // We've done the edit now. We need to both remove virtual space and clear selections.
                            var newInsertion = new VirtualSnapshotPoint(transformer.Selection.InsertionPoint.Position, 0);
                            transformer.MoveTo(newInsertion, select: false, PositionAffinity.Successor);
                        });
                    }

                    _textView.Caret.EnsureVisible();

                    this.AddAfterTextBufferChangePrimitive();

                    undoTransaction.Complete();

                    if (final)
                    {
                        newProvisionalSpan = null;
                    }
                    else if (_textView.Selection.IsReversed)
                    {
                        // The active point is at the start.
                        int virtualSpaces = firstLineInsertedVirtualSpaces.HasValue ? firstLineInsertedVirtualSpaces.Value : 0;

                        newProvisionalSpan = currentVersion.Next.CreateTrackingSpan(
                            new Span(replaceSpans.First().Start.Position + virtualSpaces, text.Length),
                            SpanTrackingMode.EdgeExclusive);
                    }
                    else
                    {
                        // The active point is at the end.
                        // Since text may have been inserted before this point, we need to accommodate the
                        // amount of inserted text, using the endPoint we constructed earlier.

                        int lastSpanStart = endPoint.GetPoint(_textView.TextSnapshot).Position;

                        newProvisionalSpan = currentVersion.Next.CreateTrackingSpan(
                            new Span(lastSpanStart + lastLineInsertedVirtualSpaces, text.Length),
                            SpanTrackingMode.EdgeExclusive);
                    }
                }

                // This test is for null to non-null or vice versa (two different ITrackingSpans -- even if they cover the same span -- are not
                // considered equal).
                if (_immProvisionalComposition != newProvisionalSpan)
                {
                    _immProvisionalComposition = newProvisionalSpan;
                    _textView.ProvisionalTextHighlight = _immProvisionalComposition;
                }

                return editSuccessful;
            }
        }

        public bool InsertTextAsBox(string text, out VirtualSnapshotPoint boxStart, out VirtualSnapshotPoint boxEnd)
        {
            return InsertTextAsBox(text, out boxStart, out boxEnd, Strings.InsertText);
        }

        public bool InsertTextAsBox(string text, out VirtualSnapshotPoint boxStart, out VirtualSnapshotPoint boxEnd, string undoText)
        {
            if (text == null)
                throw new ArgumentNullException(nameof(text));

            boxStart = boxEnd = _textView.Caret.Position.VirtualBufferPosition;

            // You can't use out parameters in a lambda or delegate, so just make a copy of
            // boxStart/boxEnd
            VirtualSnapshotPoint newStart, newEnd;
            newStart = newEnd = boxStart;

            if (text.Length == 0)
                return this.InsertText(text);

            Func<bool> action = () =>
            {
                // Separate edit transaction for the delete
                if (!DeleteHelper(_textView.Selection.SelectedSpans))
                    return false;

                // Put the selection in box mode for this operation
                _textView.Selection.Mode = TextSelectionMode.Box;

                _textView.Caret.MoveTo(_textView.Selection.Start);

                // Remember the starting position
                VirtualSnapshotPoint oldCaretPos = _textView.Caret.Position.VirtualBufferPosition;
                double startX = _textView.Caret.Left;

                // Estimate the number of spaces to the caret, to be used
                // for any amount of text that goes past the end of the buffer
                int spacesToCaret = _editorPrimitives.View.GetTextPoint(oldCaretPos.Position).DisplayColumn + oldCaretPos.VirtualSpaces;

                ITrackingPoint endPoint = null;
                int? firstLineInsertedVirtualSpaces = null;

                using (ITextEdit textEdit = _editorPrimitives.Buffer.AdvancedTextBuffer.CreateEdit())
                {
                    VirtualSnapshotPoint currentCaret = oldCaretPos;
                    ITextViewLine currentLine = _textView.GetTextViewLineContainingBufferPosition(currentCaret.Position);

                    bool pastEndOfBuffer = false;

                    // Read a line at a time from the given text, inserting each line of text
                    // into the buffer at a successive line below the line the caret is on.
                    // For any text left over at the end of the buffer, insert endlines as
                    // well (to create new lines at the end of the file), with virtual space
                    // for padding.
                    using (StringReader reader = new StringReader(text))
                    {
                        for (string lineText = reader.ReadLine(); lineText != null;)
                        {
                            if (lineText.Length > 0)
                            {
                                // Remember the endPoint, for determining the boxEnd argument
                                endPoint = _textView.TextSnapshot.CreateTrackingPoint(currentCaret.Position, PointTrackingMode.Positive);

                                int whitespaceLength = 0;

                                // Add on any virtual space needed
                                if (currentCaret.IsInVirtualSpace)
                                {
                                    string whitespace = GetWhitespaceForVirtualSpace(currentCaret);
                                    lineText = whitespace + lineText;
                                    whitespaceLength = whitespace.Length;
                                }

                                // Update information about the first inserted line
                                if (!firstLineInsertedVirtualSpaces.HasValue)
                                {
                                    firstLineInsertedVirtualSpaces = whitespaceLength;
                                    oldCaretPos = currentCaret;
                                }

                                // Insert the text as part of this edit transaction
                                if (!textEdit.Insert(currentCaret.Position, lineText))
                                    return false;
                            }

                            // We've already read past the end of the buffer, so we're done
                            if (pastEndOfBuffer)
                                break;

                            // If this is the last line in the file, collect the rest
                            // of the text to insert.
                            if (currentLine.LineBreakLength == 0)
                            {
                                string whitespace = GetWhitespaceForDisplayColumn(spacesToCaret);
                                string newline = _editorOptions.GetNewLineCharacter();

                                string extraLine = null;
                                lineText = string.Empty;
                                string blankLines = string.Empty;
                                while ((extraLine = reader.ReadLine()) != null)
                                {
                                    // Either add the line (if there is any text), or
                                    // just add the newline
                                    if (extraLine.Length > 0)
                                    {
                                        lineText += blankLines + // Any blank lines
                                                    newline + // a line break, to get a new line
                                                    whitespace + // realized virtual space
                                                    extraLine; // the line text itself

                                        blankLines = string.Empty;
                                    }
                                    else
                                    {
                                        blankLines += newline;
                                    }
                                }

                                pastEndOfBuffer = true;

                                currentCaret = new VirtualSnapshotPoint(currentLine.EndIncludingLineBreak);
                                // Current line hasn't changed, so we don't need to set it again
                            }
                            else // Otherwise, try and read the next line
                            {
                                lineText = reader.ReadLine();

                                if (lineText != null)
                                {
                                    currentLine = _textView.GetTextViewLineContainingBufferPosition(currentLine.EndIncludingLineBreak);
                                    currentCaret = currentLine.GetInsertionBufferPositionFromXCoordinate(startX);
                                }
                            }
                        }
                    }

                    // If we didn't actually insert any text, then cancel the edit and return false
                    if (endPoint == null)
                    {
                        textEdit.Cancel();
                        return false;
                    }

                    textEdit.Apply();

                    if (textEdit.Canceled)
                        return false;
                }

                // Now, figure out the start and end positions
                // and update the caret and selection.
                _textView.Selection.Clear();

                // Move the caret back to the starting point, which is now
                // no longer in virtual space.
                int virtualSpaces = firstLineInsertedVirtualSpaces.HasValue ? firstLineInsertedVirtualSpaces.Value : 0;

                _textView.Caret.MoveTo(new SnapshotPoint(_textView.TextSnapshot,
                                                         oldCaretPos.Position.Position + virtualSpaces));

                newStart = _textView.Caret.Position.VirtualBufferPosition;

                // endPoint was a forward-tracking point at the beginning of the last insertion,
                // so it should be positioned at the very end of the inserted text in the new
                // snapshot.
                newEnd = new VirtualSnapshotPoint(endPoint.GetPoint(_textView.TextSnapshot));

                return true;
            };

            bool succeeded = ExecuteAction(undoText, action);

            if (succeeded)
            {
                boxStart = newStart;
                boxEnd = newEnd;
            }

            return succeeded;
        }
        #endregion

        #region Clipboard and RTF helpers

        private Func<bool> PrepareClipboardSelectionCopy()
        {
            NormalizedSnapshotSpanCollection selectedSpans = _textView.Selection.SelectedSpans;

            string text = this.SelectedText;
            string rtfText = null;
            try
            {
                rtfText = GenerateRtf(selectedSpans);
            }
            catch (OperationCanceledException)
            {
                // Ignore cancellation when doing a copy. The user may not even want RTF text so preventing the normal text from being copied would be overkill.
            }

            bool isBox = _textView.Selection.Mode == TextSelectionMode.Box;

            return () => CopyToClipboard(text, rtfText, lineCutCopyTag: false, boxCutCopyTag: isBox);
        }

        private Func<bool> PrepareClipboardFullLineCopy(DisplayTextRange textRange)
        {
            string text = textRange.GetText();

            string rtfText = null;
            try
            {
                rtfText = GenerateRtf(textRange.AdvancedTextRange);
            }
            catch (OperationCanceledException)
            {
                // Ignore cancellation when doing a copy. The user may not even want RTF text so preventing the normal text from being copied would be overkill.
            }


            return () => CopyToClipboard(text, rtfText, lineCutCopyTag: true, boxCutCopyTag: false);
        }

        /// <summary>
        /// Copies data into the clipboard.
        /// </summary>
        /// <param name="textData">The textual content to put in the clipboard</param>
        /// <param name="rtfData">The RTF formatted data to put in the clipboard</param>
        /// <returns>true if operation succeeded.</returns>
        private static bool CopyToClipboard(string textData, string rtfData, bool lineCutCopyTag, bool boxCutCopyTag)
        {
            // note: we should change clipboard data even if textData or rtfData are empty

            // Clipboard may throw exceptions, so enclose Clipboard calls in a try-catch block
            try
            {
                DataObject dataObject = new DataObject();

                //set plain text format
                dataObject.SetText(textData);

                //set any additional data
                if (rtfData != null)
                {
                    dataObject.SetData(DataFormats.Rtf, rtfData);
                }

                //tag the data in the clipboard if requested
                if (lineCutCopyTag)
                {
                    dataObject.SetData(_clipboardLineBasedCutCopyTag, true);
                }

                if (boxCutCopyTag)
                {
                    dataObject.SetData(_boxSelectionCutCopyTag, true);
                }

                // When adding an item to the clipboard, use delay rendering. We expect the host to flush
                // the data down during shutdown if it so desires. Putting the data on the clipboard with delay 
                // rendering helps with clipboard contention issues with remote desktop and multiple clipboard 
                // chain listeners.
                // WPF, when doing no delay rendering, calls OleSetClipboard and OleFlushClipboard under the covers 
                // which causes the OS to send out two almost simultaneous clipboard open/close notification pairs 
                // which confuse applications that try to synchronize clipboard data between multiple machines such 
                // as MagicMouse or remote desktop.
                Clipboard.SetDataObject(dataObject, false);

                return true;
            }
            catch (System.Runtime.InteropServices.ExternalException)
            {
                // TODO: Log error
                return false;
            }
        }

        private string GenerateRtf(NormalizedSnapshotSpanCollection spans)
        {
            //TODO:MAC
            //if (_textView.Options.GetOptionValue(EnableRtfCopy.OptionKey))
            //{
            //    //Don't generate RTF for large spans (since it is expensive and probably not wanted).
            //    int length = spans.Sum((span) => span.Length);
            //    if (length < _textView.Options.GetOptionValue(MaxRtfCopyLength.OptionKey))
            //    {
            //        if (_textView.Options.GetOptionValue(UseAccurateClassificationForRtfCopy.OptionKey))
            //        {
            //            using (var dialog = WaitHelper.Wait(_factory.WaitIndicator, Strings.WaitTitle, Strings.WaitMessage))
            //            {
            //                return ((IRtfBuilderService2)(_factory.RtfBuilderService)).GenerateRtf(spans, dialog.CancellationToken);
            //            }
            //        }
            //        else
            //        {
            //            return _factory.RtfBuilderService.GenerateRtf(spans);
            //        }
            //    }
            //}

            return null;
        }

        private string GenerateRtf(SnapshotSpan span)
        {
            return GenerateRtf(new NormalizedSnapshotSpanCollection(span));
        }

        #endregion

        #region Horizontal whitespace helpers

        private bool DeleteHorizontalWhitespace()
        {
            List<Span> singleSpansToDelete = new List<Span>();
            List<Span> largeSpansToDelete = new List<Span>();
            List<Span> singleTabsToReplace = new List<Span>();
            ITextSnapshot snapshot = _editorPrimitives.View.AdvancedTextView.TextSnapshot;
            int whitespaceLocation = -1;
            ITrackingPoint lastDeleteLocation = snapshot.CreateTrackingPoint(_editorPrimitives.Caret.CurrentPosition, PointTrackingMode.Positive);

            using (ITextEdit textEdit = snapshot.TextBuffer.CreateEdit())
            {
                int startPoint = GetStartPointOfSpanForDeleteHorizontalWhitespace(snapshot);
                int endPoint = GetEndPointOfSpanForDeleteHorizontalWhitespace(snapshot);

                for (int i = startPoint; i <= endPoint && i < snapshot.Length; i++)
                {
                    if (IsSpaceCharacter(snapshot[i]))
                    {
                        if (whitespaceLocation == -1)
                        {
                            whitespaceLocation = i;
                        }
                    }
                    else
                    {
                        if (whitespaceLocation != -1)
                        {
                            if ((i - whitespaceLocation == 1) && !_editorPrimitives.Selection.IsEmpty)
                            {
                                // This is a span of just a single space or tab so track it as such
                                singleSpansToDelete.Add(Span.FromBounds(whitespaceLocation, i));

                                // If this is a tab character we need to replace it with a space
                                if (snapshot[i - 1] == '\t')
                                {
                                    lastDeleteLocation = snapshot.CreateTrackingPoint(i, PointTrackingMode.Positive);
                                    singleTabsToReplace.Add(Span.FromBounds(whitespaceLocation, i));
                                }
                            }
                            else
                            {
                                // This is a span of more than one space or tab so track it as such
                                lastDeleteLocation = snapshot.CreateTrackingPoint(i, PointTrackingMode.Positive);
                                largeSpansToDelete.Add(Span.FromBounds(whitespaceLocation, i));
                            }
                        }
                        whitespaceLocation = -1;
                    }
                }

                // If the last span is at end of the selection
                if (whitespaceLocation != -1)
                {
                    lastDeleteLocation = snapshot.CreateTrackingPoint(endPoint, PointTrackingMode.Positive);
                    if ((endPoint - whitespaceLocation == 1) && !_editorPrimitives.Selection.IsEmpty)
                        singleSpansToDelete.Add(Span.FromBounds(whitespaceLocation, endPoint));
                    else
                        largeSpansToDelete.Add(Span.FromBounds(whitespaceLocation, endPoint));
                }

                if (!DeleteHorizontalWhitespace(textEdit, largeSpansToDelete, singleSpansToDelete, singleTabsToReplace))
                    return false;

                textEdit.Apply();

                if (textEdit.Canceled)
                    return false;

                if (_editorPrimitives.Selection.IsEmpty)
                {
                    // Restore the caret to the x-coordinate it was at previously on the new line
                    _editorPrimitives.Caret.MoveTo(lastDeleteLocation.GetPosition(_editorPrimitives.View.AdvancedTextView.TextSnapshot));
                }
            }

            return true;
        }

        private static bool DeleteHorizontalWhitespace(ITextEdit textEdit, ICollection<Span> largeSpansToDelete, ICollection<Span> singleSpansToDelete, ICollection<Span> singleTabsToReplace)
        {
            ITextSnapshot snapshot = textEdit.Snapshot;
            Func<int, bool> isLeadingWhitespace = (p) => snapshot.GetLineFromPosition(p).Start == p;
            Func<int, bool> isTrailingWhitespace = (p) => snapshot.GetLineFromPosition(p).End == p;

            if (largeSpansToDelete.Count == 0)
            {
                // If there were no spans of more than one space or tab,
                // then delete all spans of just a single space or tab
                foreach (var span in singleSpansToDelete)
                {
                    if (!textEdit.Delete(span))
                        return false;
                }
            }
            else
            {
                // If there was at least one span of more than one space or
                // tab then replace all of those spans with spaces
                foreach (var span in largeSpansToDelete)
                {
                    // if it is leading or trailing whitespace, just delete it
                    if (isLeadingWhitespace(span.Start) || isTrailingWhitespace(span.End))
                    {
                        if (!textEdit.Delete(span))
                            return false;
                    }
                    // if there's space adjacent to the beginning/end of the selection
                    else if (IsSpaceCharacter(snapshot[span.Start - 1]) || IsSpaceCharacter(snapshot[span.End]))
                    {
                        if (!textEdit.Delete(span))
                            return false;
                    }
                    else if (!textEdit.Replace(span, " "))
                        return false;
                }

                // Replace single tabs with a single space, but
                // skip leading / trailing tabs since they will be deleted below by the singleSpansToDelete loop.
                foreach (var span in singleTabsToReplace)
                {
                    if (!isLeadingWhitespace(span.Start) && !isTrailingWhitespace(span.End))
                    {
                        if (!textEdit.Replace(span, " "))
                            return false;
                    }
                }

                // Delete leading or trailing whitespace in singleSpansToDelete
                foreach (var span in singleSpansToDelete)
                {
                    if (isLeadingWhitespace(span.Start) || isTrailingWhitespace(span.End))
                    {
                        if (!textEdit.Delete(span))
                            return false;
                    }
                }
            }

            return true;
        }

        private int GetEndPointOfSpanForDeleteHorizontalWhitespace(ITextSnapshot snapshot)
        {
            int endPoint = _editorPrimitives.Selection.GetEndPoint().CurrentPosition;
            if (_editorPrimitives.Selection.IsEmpty)
            {
                int endOfLine = _editorPrimitives.Caret.EndOfViewLine;
                while (endPoint < endOfLine)
                {
                    if (!IsSpaceCharacter(snapshot[endPoint]))
                    {
                        break;
                    }
                    endPoint++;
                }
            }
            return endPoint;
        }

        private int GetStartPointOfSpanForDeleteHorizontalWhitespace(ITextSnapshot snapshot)
        {
            int startPoint = _editorPrimitives.Selection.GetStartPoint().CurrentPosition;
            if (_editorPrimitives.Selection.IsEmpty)
            {
                int startOfViewLine = _editorPrimitives.Caret.StartOfViewLine;
                while (startPoint > startOfViewLine)
                {
                    // If the caracter *before* the current start point is
                    // not whitespace, then quit.
                    if (!IsSpaceCharacter(snapshot[startPoint - 1]))
                    {
                        break;
                    }

                    startPoint--;
                }
            }
            return startPoint;
        }

        #endregion

        #region Indent/unindent helpers

        // Perform the given indent action (indent/unindent) on each line at the first non-whitespace
        // character, skipping lines that are either empty or just whitespace.
        // This method is used by Indent, Unindent, IncreaseLineIndent, and DecreaseLineIndent, and
        // is essentially a replacement for the editor primitive's Indent and Unindent functions.
        private bool PerformIndentActionOnEachBufferLine(Func<SnapshotPoint, ITextEdit, int?> action)
        {
            Func<bool> indentAction = () =>
            {
                var newSelections = new FrugalList<(Selection old, Selection newSel)>();
                if (!this.EditHelper(edit =>
                {
                    bool succeeded = true;

                    using (_multiSelectionBroker.BeginBatchOperation())
                    {
                        _multiSelectionBroker.PerformActionOnAllSelections(transformer =>
                        {
                            if (this.PerformIndentActionOnEachBufferLine(edit, newSelections, transformer.Selection, action) == null)
                            {
                                succeeded = false;
                            }
                        });
                    }

                    return succeeded;
                }))
                {
                    return false;
                }

                // Apply selection changes after the edit so we can apply the desired point tracking mode.
                TransformSelections(newSelections);

                _multiSelectionBroker.TryEnsureVisible(_multiSelectionBroker.PrimarySelection, EnsureSpanVisibleOptions.MinimumScroll);

                return true;
            };

            return this.ExecuteAction(Strings.IncreaseLineIndent, indentAction, SelectionUpdate.Ignore);
        }

        private int? PerformIndentActionOnEachBufferLine(ITextEdit textEdit, IList<(Selection old, Selection newSel)> newSelections, Selection selection, Func<SnapshotPoint, ITextEdit, int?> action)
        {
            Func<ITextEdit, int?> editAction = edit =>
            {
                int? actionResult = null;
                ITextSnapshot snapshot = _textView.TextSnapshot;

                int startLineNumber = snapshot.GetLineNumberFromPosition(selection.Start.Position);
                int endLineNumber = snapshot.GetLineNumberFromPosition(selection.End.Position);

                for (int i = startLineNumber; i <= endLineNumber; i++)
                {
                    ITextSnapshotLine line = snapshot.GetLineFromLineNumber(i);

                    // If the line is blank or the (non-empty) selection ends at the start of this line, exclude
                    // the line from processing.
                    if (line.Length == 0 ||
                        (!selection.IsEmpty && line.Start == selection.End.Position))
                        continue;

                    TextPoint textPoint = _editorPrimitives.Buffer.GetTextPoint(line.Start).GetFirstNonWhiteSpaceCharacterOnLine();

                    if (textPoint.CurrentPosition == textPoint.EndOfLine)
                        continue;

                    SnapshotPoint point = new SnapshotPoint(snapshot, textPoint.CurrentPosition);

                    actionResult = action(point, edit);
                    if (actionResult == null)
                    {
                        return null;
                    }
                }

                // Return the indent of the final line.
                return actionResult;
            };

            // Track the start of the selection with PointTrackingMode.Negative through the text
            // change.  If the result has the same absolute snapshot offset as the point before the change, we'll move
            // the selection start point back to there instead of letting it track automatically.
            ITrackingPoint startPoint = _textView.TextSnapshot.CreateTrackingPoint(selection.Start.Position, PointTrackingMode.Negative);
            int startPositionBufferChange = selection.Start.Position;

            var result = editAction.Invoke(textEdit);
            if (result == null)
            {
                return null;
            }

            VirtualSnapshotPoint newStart = new VirtualSnapshotPoint(startPoint.GetPoint(_textView.TextSnapshot));
            if (newStart.Position == startPositionBufferChange)
            {
                bool isReversed = selection.IsReversed;
                VirtualSnapshotPoint anchor = isReversed ? selection.End : newStart;
                VirtualSnapshotPoint active = isReversed ? newStart : selection.End;

                newSelections.Add(
                    (selection,
                        new Selection(
                            active.TranslateTo(_textView.TextSnapshot, PointTrackingMode.Positive),
                            anchor.TranslateTo(_textView.TextSnapshot, PointTrackingMode.Negative),
                            active.TranslateTo(_textView.TextSnapshot, PointTrackingMode.Positive),
                            selection.InsertionPointAffinity)));
            }

            return result;
        }

        private bool IndentOperationShouldBeMultiLine(Selection selection)
        {
            if (selection.IsEmpty)
                return false;

            var startLine = selection.Start.Position.GetContainingLine();

            bool pointsOnSameLine = selection.End.Position <= startLine.End;

            bool lastLineOfFile = startLine.End == startLine.EndIncludingLineBreak;
            bool entireLastLineSelected = lastLineOfFile &&
                                            _textView.Selection.Start.Position == startLine.Start &&
                                            _textView.Selection.End.Position == startLine.End;

            return !pointsOnSameLine || entireLastLineSelected;
        }

        private int? InsertSingleIndentAtPoint(SnapshotPoint point, ITextEdit edit)
        {
            VirtualSnapshotPoint virtualPoint = new VirtualSnapshotPoint(point);
            VirtualSnapshotSpan span = new VirtualSnapshotSpan(virtualPoint, virtualPoint);
            return InsertIndentForSpan(span, edit, exactlyOneIndentLevel: true, useBufferPrimitives: true) != null ? 0 : default(int?);
        }

        private int? InsertIndentForSpan(VirtualSnapshotSpan span, ITextEdit edit, bool exactlyOneIndentLevel, bool useBufferPrimitives = false, int columnOffset = 0)
        {
            int indentSize = _textView.Options.GetIndentSize();
            bool convertTabsToSpaces = _textView.Options.IsConvertTabsToSpacesEnabled();
            bool boxSelection = _textView.Selection.Mode == TextSelectionMode.Box &&
                                _textView.Selection.Start != _textView.Selection.End;
            ITextSnapshot snapshot = edit.Snapshot;

            VirtualSnapshotPoint point = span.Start;

            // In a box selection, we don't insert anything for lines in virtual space
            if (boxSelection && point.IsInVirtualSpace)
            {
                return 0;
            }

            string textToInsert;

            int startPointForReplace = point.Position;
            int endPointForReplace = boxSelection ? startPointForReplace : span.End.Position;

            int currentColumn;
            int distanceToNextIndentStop;

            if (!convertTabsToSpaces)
            {
                // First, move to the left to find the first non-space character
                for (; startPointForReplace > 0; startPointForReplace--)
                {
                    if (snapshot[startPointForReplace - 1] != ' ')
                        break;
                }

                // Find the column of the position before the spaces
                TextPoint textPoint;
                if (useBufferPrimitives)
                    textPoint = _editorPrimitives.Buffer.GetTextPoint(startPointForReplace);
                else
                    textPoint = _editorPrimitives.View.GetTextPoint(startPointForReplace);

                int startColumn = textPoint.Column;
                currentColumn = startColumn + (point.Position - startPointForReplace) + point.VirtualSpaces;

                if (exactlyOneIndentLevel)
                    distanceToNextIndentStop = indentSize;
                else
                    distanceToNextIndentStop = indentSize - (currentColumn % indentSize);

                int columnToInsertTo = currentColumn + distanceToNextIndentStop;
                int totalDistance = columnToInsertTo - startColumn;

                textToInsert = GetWhiteSpaceForPositionAndVirtualSpace(new SnapshotPoint(snapshot, startPointForReplace), totalDistance, useBufferPrimitives);
            }
            else
            {
                // Here, we know we don't need to eat spaces
                TextPoint textPoint;
                if (useBufferPrimitives)
                    textPoint = _editorPrimitives.Buffer.GetTextPoint(point.Position.Position);
                else
                    textPoint = _editorPrimitives.View.GetTextPoint(point.Position.Position);

                currentColumn = textPoint.Column + point.VirtualSpaces + columnOffset;

                if (exactlyOneIndentLevel)
                    distanceToNextIndentStop = indentSize;
                else
                    distanceToNextIndentStop = indentSize - (currentColumn % indentSize);

                textToInsert = GetWhiteSpaceForPositionAndVirtualSpace(point.Position, point.VirtualSpaces + distanceToNextIndentStop, useBufferPrimitives);
            }

            if (_textView.Caret.OverwriteMode && span.IsEmpty)
            {
                // Walk forward, towards the end of the line, until we get to the column we will be
                // overwriting.
                int columnToInsertTo = currentColumn + distanceToNextIndentStop;

                int lineEnd = point.Position.GetContainingLine().End;

                for (; endPointForReplace < lineEnd; endPointForReplace++)
                {
                    if (_editorPrimitives.View.GetTextPoint(endPointForReplace).Column >= columnToInsertTo)
                        break;
                }
            }

            var spanToReplace = Span.FromBounds(startPointForReplace, endPointForReplace);
            int newColumnOffset = (textToInsert.Length - spanToReplace.Length) + columnOffset;

            return edit.Replace(spanToReplace, textToInsert) ? newColumnOffset : default(int?);
        }

        private int? RemoveIndentAtPoint(SnapshotPoint point, ITextEdit edit)
        {
            return RemoveIndentAtPoint(point, edit, failOnNonWhitespaceCharacter: true, useBufferPrimitives: true) ? 0 : default(int?);
        }

        /// <summary>
        /// Remove an "indent" to the left of the given point.  If <paramref name="columnsToRemove"/> is specified, remove exactly
        /// that amount of indent, instead of the normal logic of finding the previous indent stop.
        /// </summary>
        private bool RemoveIndentAtPoint(SnapshotPoint point, ITextEdit edit, bool failOnNonWhitespaceCharacter, bool useBufferPrimitives = false, int? columnsToRemove = null)
        {
            // If we've been asked to remove *no* whitespace, then we're done.
            if (columnsToRemove.HasValue && columnsToRemove.Value == 0)
                return true;

            // The logic for removing an indent is:
            // 1) Find the previous indent level from the span's start position (or use columnsToRemove, if specified).
            // 2) Delete characters back to the previous indent level, but
            // 3) If the deletion goes past the previous indent level (i.e. by deleting a tab), insert spaces to bring the text
            //    back to the correct indent level.

            ITextSnapshot snapshot = edit.Snapshot;

            int indentSize = _editorOptions.GetIndentSize();

            TextPoint startPointAsTextPoint;
            if (useBufferPrimitives)
                startPointAsTextPoint = _editorPrimitives.Buffer.GetTextPoint(point);
            else
                startPointAsTextPoint = _editorPrimitives.View.GetTextPoint(point);

            int currentColumn = startPointAsTextPoint.Column;
            int startOfLine = startPointAsTextPoint.StartOfLine;

            if (startPointAsTextPoint.CurrentPosition == startOfLine)
                return true;

            // Determine the previous indent level; if columnsToRemove is specified, just subtract that
            // from our current column.  Otherwise, find the previous indent stop.
            int previousIndentLevel;
            if (columnsToRemove.HasValue)
                previousIndentLevel = Math.Max(0, currentColumn - columnsToRemove.Value);
            else
                previousIndentLevel = ((currentColumn - 1) / indentSize) * indentSize;

            int startPointForReplace = point.Position - 1;
            int columnDeletedTo = 0;

            // First, move to the left to find the first non-space character
            for (; startPointForReplace >= startOfLine; startPointForReplace--)
            {
                // First, see if we *can* delete the character
                char c = snapshot[startPointForReplace];
                if (c != ' ' && c != '\t')
                {
                    // We can't delete this character, so move forward
                    startPointForReplace++;
                    break;
                }

                // Now, see if we *should* delete the character
                if (useBufferPrimitives)
                    columnDeletedTo = _editorPrimitives.Buffer.GetTextPoint(startPointForReplace).Column;
                else
                    columnDeletedTo = _editorPrimitives.View.GetTextPoint(startPointForReplace).Column;

                if (columnDeletedTo <= previousIndentLevel)
                    break;
            }

            // If the start point is at or past the span start, there isn't anything to replace.
            if (startPointForReplace >= point)
                return true;

            // If we can't delete the full indent level and we've been asked to fail in this case, return false.
            if (failOnNonWhitespaceCharacter && columnDeletedTo > previousIndentLevel)
                return false;

            string textToInsert = string.Empty;

            if (columnDeletedTo < previousIndentLevel)
                textToInsert = GetWhiteSpaceForPositionAndVirtualSpace(new SnapshotPoint(snapshot, startPointForReplace), previousIndentLevel - columnDeletedTo, useBufferPrimitives);

            return edit.Replace(Span.FromBounds(startPointForReplace, point.Position), textToInsert);
        }

        /// <summary>
        /// Used by the un-indenting logic to determine what an unindent means in virtual space.
        /// </summary>
        private VirtualSnapshotPoint GetPreviousIndentStopInVirtualSpace(VirtualSnapshotPoint point)
        {
            Debug.Assert(point.IsInVirtualSpace);

            // If the point is in virtual space, then just move to the previous indent stop
            // NOTE: this is a slight change in behavior from VS9.  In VS9, the previous indent stop in
            // virtual space would move the caret a indent size worth of characters to the left.
            TextPoint textPoint = _editorPrimitives.Buffer.GetTextPoint(point.Position);
            int endOfLineColumn = textPoint.Column;
            int column = endOfLineColumn + point.VirtualSpaces;
            int indentSize = _editorOptions.GetIndentSize();
            int previousIndentSize = ((column - 1) / indentSize) * indentSize;

            if (previousIndentSize > endOfLineColumn)
                return new VirtualSnapshotPoint(point.Position, previousIndentSize - endOfLineColumn);
            else
                return new VirtualSnapshotPoint(point.Position);
        }

        #endregion

        #region Box Selection indent/unindent helpers

        /// <summary>
        /// Given a "fix-up" anchor/active point determined before the box operation, fix up the current selection's
        /// anchor/active point.  Either/both points may be null, if no correction is necessary.
        /// </summary>
        private void FixUpSelectionAfterBoxOperation(VirtualSnapshotPoint? anchorPoint, VirtualSnapshotPoint? activePoint)
        {
            if (anchorPoint.HasValue || activePoint.HasValue)
            {
                VirtualSnapshotPoint newAnchor = anchorPoint.HasValue ? anchorPoint.Value.TranslateTo(_textView.TextSnapshot) :
                                                                        _textView.Selection.AnchorPoint;
                VirtualSnapshotPoint newActive = activePoint.HasValue ? activePoint.Value.TranslateTo(_textView.TextSnapshot) :
                                                                        _textView.Selection.ActivePoint;
                SelectAndMoveCaret(newAnchor, newActive, TextSelectionMode.Box, EnsureSpanVisibleOptions.None);
            }
        }

        /// <summary>
        /// If a selection point (anchor or active) is in virtual space, it will need to move after an unindent
        /// operation to leave the selection in a proper box.
        /// </summary>
        /// <returns>The new point to use after the unindent operation, or <c>null</c> if no special move logic
        /// will be required.</returns>
        private VirtualSnapshotPoint? CalculateBoxUnindentForSelectionPoint(VirtualSnapshotPoint point, int unindentAmount)
        {
            if (!point.IsInVirtualSpace)
                return null;

            var containingLine = _textView.GetTextViewLineContainingBufferPosition(point.Position);
            var selectionOnLine = _textView.Selection.GetSelectionOnTextViewLine(containingLine);

            // If the selection on this line isn't in virtual space, it'll track correctly
            if (!selectionOnLine.HasValue || !selectionOnLine.Value.Start.IsInVirtualSpace)
                return null;

            int spaces = Math.Max(0, point.VirtualSpaces - unindentAmount);
            return new VirtualSnapshotPoint(point.Position, spaces);
        }

        /// <summary>
        /// If a selection point (anchor or active) is in virtual space, it will need to move after an indent
        /// operation to leave the selection in a proper box.
        /// </summary>
        /// <returns>The new point to use after the indent operation, or <c>null</c> if no special move logic
        /// will be required.</returns>
        private VirtualSnapshotPoint? CalculateBoxIndentForSelectionPoint(VirtualSnapshotPoint point, int indentSize)
        {
            if (!point.IsInVirtualSpace)
                return null;

            TextPoint textPoint = _editorPrimitives.View.GetTextPoint(point.Position.Position);
            int column = textPoint.Column + point.VirtualSpaces;
            int distanceToNextIndentStop = indentSize - (column % indentSize);

            return new VirtualSnapshotPoint(point.Position, point.VirtualSpaces + distanceToNextIndentStop);
        }

        /// <summary>
        /// Determine the maximum amount the (current) box selection can be unindented, in columns, so that it will continue
        /// to be a box after the indent operation.
        /// </summary>
        private int DetermineMaxBoxUnindent()
        {
            // We need to determine the correct amount of whitespace that can be unindented from each line.
            // The amount removed should never be larger than the size of an indent, but it should also be consistent,
            // meaning that the the smallest amount of whitespace to remove on any line becomes the amount removed
            // from every line.
            //
            // This is *different* from how Orcas/Dev10RTM computed box unindent.  These special cased much
            // of the logic around moving an indent into the middle of a tab character, which we can now
            // correctly handle.
            int tabSize = _editorOptions.GetTabSize();

            //  The most we can indent, to start with, is an indentSize worth of indentation
            int maxColumnUnindent = _editorOptions.GetIndentSize();

            foreach (var point in _textView.Selection.VirtualSelectedSpans.Select(s => s.Start))
            {
                int columnsToAccountFor = maxColumnUnindent - point.VirtualSpaces;

                if (columnsToAccountFor <= 0)
                    continue;

                TextPoint textPoint = _editorPrimitives.View.GetTextPoint(point.Position);
                int column = textPoint.Column;

                // Walk the line backwards until we run out of columns to account for whitespace
                // Note that targetColumn may be negative.
                int targetColumn = column - columnsToAccountFor;

                int startOfLine = textPoint.StartOfLine;

                for (int i = point.Position.Position - 1; i >= startOfLine && textPoint.Column >= targetColumn; i--)
                {
                    textPoint.MoveTo(i);
                    string character = textPoint.GetNextCharacter();
                    if (!string.Equals(character, " ", StringComparison.Ordinal) && !string.Equals(character, "\t", StringComparison.Ordinal))
                        break;

                    column = textPoint.Column;
                    columnsToAccountFor = Math.Max(0, (column - targetColumn));

                    if (column <= targetColumn)
                        break;
                }

                maxColumnUnindent -= columnsToAccountFor;
            }

            Debug.Assert(maxColumnUnindent <= _editorOptions.GetIndentSize());
            return maxColumnUnindent;
        }

        #endregion

        #region Miscellaneous line helpers

        private DisplayTextRange GetFullLines()
        {
            DisplayTextRange selectionRange = _editorPrimitives.Selection.Clone();
            DisplayTextPoint startPoint = selectionRange.GetDisplayStartPoint();
            DisplayTextPoint endPoint = selectionRange.GetDisplayEndPoint();
            startPoint.MoveTo(startPoint.StartOfLine);
            endPoint.MoveToBeginningOfNextLine();

            return startPoint.GetDisplayTextRange(endPoint);
        }

        /// <summary>
        /// Get the fully visible text line.
        /// </summary>
        /// <param name="textLine">The first text line to check.</param>
        /// <param name="indexOfNextLine">The index of the next text line to check</param>
        /// <returns>
        /// The text line passed in if it is fully visible, there is only one text line, or the next text line is not fully visible.  
        /// Otherwise returns the text line at indexOfNextTextLine.
        /// </returns>
        private ITextViewLine FindFullyVisibleLine(ITextViewLine textLine, int indexOfNextLine)
        {
            if (textLine.VisibilityState != VisibilityState.FullyVisible)
            {
                if ((indexOfNextLine >= 0) && (_editorPrimitives.View.AdvancedTextView.TextViewLines.Count > indexOfNextLine))
                {
                    if (_editorPrimitives.View.AdvancedTextView.TextViewLines[indexOfNextLine].VisibilityState == VisibilityState.FullyVisible)
                    {
                        return _editorPrimitives.View.AdvancedTextView.TextViewLines[indexOfNextLine];
                    }
                }
            }

            return textLine;
        }

        private void MoveCaretToTextLine(ITextViewLine textLine, bool select)
        {
            VirtualSnapshotPoint anchor = _textView.Selection.AnchorPoint;
            _textView.Caret.MoveTo(textLine);
            if (select)
            {
                _textView.Selection.Select(anchor.TranslateTo(_textView.TextSnapshot), _textView.Caret.Position.VirtualBufferPosition);
            }
            else
            {
                _textView.Selection.Clear();
            }
        }

        static bool IsPointOnBlankLine(TextPoint textPoint)
        {
            TextPoint firstTextColumn = textPoint.GetFirstNonWhiteSpaceCharacterOnLine();
            return firstTextColumn.CurrentPosition == textPoint.EndOfLine;
        }

        static bool IsPointOnBlankViewLine(DisplayTextPoint displayTextPoint)
        {
            DisplayTextPoint firstTextColumn = displayTextPoint.GetFirstNonWhiteSpaceCharacterOnViewLine();
            return firstTextColumn.CurrentPosition == displayTextPoint.EndOfViewLine;
        }

        #endregion

        #region Tabs <-> spaces

        private bool ConvertSpacesAndTabsHelper(bool toTabs)
        {
            bool wasEmpty = true;
            bool wasReversed = false;
            SnapshotSpan conversionRange;
            if (_textView.Selection.IsEmpty)
            {
                conversionRange = _textView.Caret.ContainingTextViewLine.Extent; //This could contain multiple lines due to elisions.
            }
            else
            {
                conversionRange = _textView.Selection.StreamSelectionSpan.SnapshotSpan;
                wasReversed = _textView.Selection.IsReversed;
                wasEmpty = false;
            }

            using (ITextEdit textEdit = _textView.TextBuffer.CreateEdit())
            {
                SnapshotPoint currentPosition = conversionRange.Start;
                int tabSize = _editorOptions.GetTabSize();

                while (currentPosition < conversionRange.End.Position)
                {
                    //Do the conversion a line at a time since we are using the underlying ITextSnapshotLines to determin column position.
                    ITextSnapshotLine line = currentPosition.GetContainingLine();

                    int startOfConversion = Math.Max(line.Start.Position, conversionRange.Start.Position);
                    int endOfConversion = Math.Min(line.End.Position, conversionRange.End.Position);

                    //Get a TextPoint so we can use the editor primitives logic for handling multicharacter sequences, etc.
                    TextPoint point = _editorPrimitives.Buffer.GetTextPoint(startOfConversion);

                    int column = point.Column;

                    int whiteSpaceStart = point.CurrentPosition;
                    int whiteSpaceColumnStart = column;

                    while (point.CurrentPosition < endOfConversion)
                    {
                        char c = textEdit.Snapshot[point.CurrentPosition];

                        if (c == '\t')
                        {
                            column = ((column / tabSize) + 1) * tabSize;
                        }
                        else
                        {
                            if (c != ' ')
                            {
                                if (!ConvertTabsAndSpaces(textEdit, toTabs, tabSize, whiteSpaceStart, whiteSpaceColumnStart, point.CurrentPosition, column))
                                    return false;
                            }
                            ++column;
                        }

                        //This handles multicharacter glyphs (at least to the degree that the TextPoint primitive understands them). We want this instead of DisplayTextPoint
                        //since we do not want this conversion to be affected by elisions or word wrap.
                        point.MoveToNextCharacter();

                        if ((c != ' ') && (c != '\t'))
                        {
                            whiteSpaceStart = point.CurrentPosition;
                            whiteSpaceColumnStart = column;
                        }
                    }

                    if (!ConvertTabsAndSpaces(textEdit, toTabs, tabSize, whiteSpaceStart, whiteSpaceColumnStart, point.CurrentPosition, column))
                        return false;

                    currentPosition = line.EndIncludingLineBreak;
                }

                textEdit.Apply();

                if (textEdit.Canceled)
                    return false;
            }

            if (wasEmpty)
            {
                _textView.Caret.MoveTo(conversionRange.Start.TranslateTo(_textView.TextSnapshot, PointTrackingMode.Negative));
            }
            else
            {
                _textView.Selection.Select(conversionRange.TranslateTo(_textView.TextSnapshot, SpanTrackingMode.EdgeInclusive), wasReversed);
                _textView.Caret.MoveTo(_textView.Selection.ActivePoint);
            }

            return true;
        }

        private static bool ConvertTabsAndSpaces(ITextEdit textEdit, bool toTabs, int tabSize, int whiteSpaceStart, int whiteSpaceColumnStart, int whiteSpaceEnd, int whiteSpaceColumnEnd)
        {
            if (whiteSpaceEnd > whiteSpaceStart)
            {
                Debug.Assert(whiteSpaceColumnEnd > whiteSpaceColumnStart);

                string textToInsert;
                if (toTabs)
                {
                    int spacesAfterPreviousTabStop = whiteSpaceColumnEnd % tabSize;

                    int columnOfPreviousTabStop = whiteSpaceColumnEnd - spacesAfterPreviousTabStop;

                    int requiredTabs = ((columnOfPreviousTabStop - whiteSpaceColumnStart) + tabSize - 1) / tabSize;
                    if (requiredTabs > 0)
                        textToInsert = new string('\t', requiredTabs) + new string(' ', spacesAfterPreviousTabStop);
                    else
                        textToInsert = new string(' ', whiteSpaceColumnEnd - whiteSpaceColumnStart);
                }
                else
                {
                    textToInsert = new string(' ', whiteSpaceColumnEnd - whiteSpaceColumnStart);
                }

                Span replaceSpan = Span.FromBounds(whiteSpaceStart, whiteSpaceEnd);
                if ((replaceSpan.Length != textToInsert.Length) || (!string.Equals(textToInsert, textEdit.Snapshot.GetText(replaceSpan), StringComparison.Ordinal))) //performance hack: don't get the text if we know they'll be different.
                    return textEdit.Replace(replaceSpan, textToInsert);
            }

            return true;
        }

        #endregion

        #region Edit/Replace/Delete helpers

        internal bool EditHelper(Func<ITextEdit, bool> editAction)
        {
            using (var edit = _textView.TextBuffer.CreateEdit())
            {
                if (!editAction(edit))
                    return false;
                edit.Apply();
                return !edit.Canceled;
            }
        }

        internal bool ReplaceHelper(Span span, string text)
        {
            return EditHelper(e => e.Replace(span, text));
        }

        internal bool ReplaceHelper(VirtualSnapshotSpan virtualSnapshotSpan, string text)
        {
            if (virtualSnapshotSpan.Start.IsInVirtualSpace)
                text = GetWhitespaceForVirtualSpace(virtualSnapshotSpan.Start) + text;
            return EditHelper(e => e.Replace(virtualSnapshotSpan.SnapshotSpan, text));
        }

        internal bool ReplaceHelper(NormalizedSpanCollection spans, string text)
        {
            return EditHelper((e) =>
            {
                foreach (var span in spans)
                {
                    if (!e.Replace(span, text) || e.Canceled)
                        return false;
                }

                return true;
            });
        }

        internal bool ReplaceHelper(IEnumerable<VirtualSnapshotSpan> spans, string text)
        {
            return EditHelper((e) =>
            {
                foreach (var span in spans)
                {
                    string lineText = text;

                    if (span.Start.IsInVirtualSpace)
                        lineText = GetWhitespaceForVirtualSpace(span.Start) + text;

                    if (!e.Replace(span.SnapshotSpan, lineText) || e.Canceled)
                        return false;
                }

                return true;
            });
        }

        internal bool DeleteHelper(Span span)
        {
            return EditHelper(e => e.Delete(span));
        }

        internal bool DeleteHelper(NormalizedSpanCollection spans)
        {
            return EditHelper((e) =>
            {
                foreach (var span in spans)
                {
                    if (!e.Delete(span) || e.Canceled)
                        return false;
                }

                return true;
            });
        }

        #endregion

        internal bool IsEmptyBoxSelection()
        {
            return !_textView.Selection.IsEmpty &&
                    _textView.Selection.VirtualSelectedSpans.All(s => s.IsEmpty);
        }

        /// <summary>
        /// Position the caret using the smart indent service.  Optionally, the caller can choose between
        /// using only virtual space (and failing if that isn't possible) or using real spaces when the
        /// line is not empty.
        /// </summary>
        /// <param name="useOnlyVirtualSpace">If <c>true</c>, don't ever use real spaces to position the caret,
        /// even if there is no other way to position it with smart indent.  Defaults to <c>true</c>.</param>
        /// <param name="extendSelection">If <c>true</c>, extend the current selection, from the existing anchor point,
        /// to the new caret position.</param>
        /// <returns><c>true</c> if the caret was positioned in virtual space.</returns>
        private bool PositionCaretWithSmartIndent(ISelectionTransformer transformer, bool useOnlyVirtualSpace = true, bool extendSelection = false)
        {
            var caretPosition = transformer.Selection.InsertionPoint;
            var caretLine = caretPosition.Position.GetContainingLine();

            int? indentation = _factory.SmartIndentationService.GetDesiredIndentation(_textView, caretLine);
            if (indentation.HasValue)
            {
                if (caretPosition.Position == caretLine.End)
                {
                    //Position the caret in virtual space at the appropriate indentation.
                    var newCaretPoint = new VirtualSnapshotPoint(caretPosition.Position, Math.Max(0, indentation.Value - caretLine.Length));
                    var anchorPoint = (extendSelection) ? transformer.Selection.AnchorPoint : newCaretPoint;
                    transformer.MoveTo(anchorPoint, newCaretPoint, newCaretPoint, PositionAffinity.Successor);
                    return true;
                }
                else if (!useOnlyVirtualSpace)
                {
                    // See how much whitespace already exists in the new line
                    int existingWhitespaceChars = GetLeadingWhitespaceChars(caretLine, caretPosition.Position);

                    // Insert the appropriate amount of leading whitespace, potentially replacing existing whitespace
                    string whitespace = GetWhiteSpaceForPositionAndVirtualSpace(caretPosition.Position, indentation.Value);
                    return ReplaceHelper(new SnapshotSpan(caretPosition.Position, existingWhitespaceChars), whitespace);
                }
            }

            return false;
        }

        /// <summary>
        /// Returns number of whitespace characters
        /// between startPosition and first non-whitespace character in the specified line
        /// </summary>
        /// <param name="line">Which line to evaluate</param>
        /// <param name="startPosition">Position where the count starts</param>
        /// <returns>Number of leading whitespace characters located after startPosition</returns>
        private static int GetLeadingWhitespaceChars(ITextSnapshotLine line, SnapshotPoint startPosition)
        {
            int whitespace = 0;
            for (int i = startPosition.Position; i < line.End; ++i)
            {
                if (IsSpaceCharacter(line.Snapshot[i]))
                {
                    whitespace++;
                }
                else
                    break;
            }
            return whitespace;
        }

        private bool ConvertLeadingWhitespace(string actionName, bool convertTabsToSpaces)
        {
            Func<bool> action = () =>
            {
                using (ITextEdit textEdit = _editorPrimitives.Buffer.AdvancedTextBuffer.CreateEdit())
                {
                    ITextSnapshot snapshot = textEdit.Snapshot;

                    for (int i = _editorPrimitives.Selection.GetStartPoint().LineNumber; i <= _editorPrimitives.Selection.GetEndPoint().LineNumber; i++)
                    {
                        ITextSnapshotLine line = snapshot.GetLineFromLineNumber(i);

                        TextPoint startOfLine = _editorPrimitives.Buffer.GetTextPoint(line.Start.Position);
                        TextPoint firstNonWhitespaceCharacter = startOfLine.GetFirstNonWhiteSpaceCharacterOnLine();
                        int columnOfFirstNonWhitespaceCharacter = firstNonWhitespaceCharacter.Column;

                        string whitespace = GetWhiteSpaceForPositionAndVirtualSpace(line.Start, columnOfFirstNonWhitespaceCharacter, /* useBufferPrimitives: */ true, convertTabsToSpaces);

                        SnapshotSpan currentWhiteSpace = new SnapshotSpan(line.Start, firstNonWhitespaceCharacter.AdvancedTextPoint);

                        if (!string.Equals(whitespace, currentWhiteSpace.GetText(), StringComparison.Ordinal))
                        {
                            if (!textEdit.Replace(currentWhiteSpace, whitespace))
                                return false;
                        }
                    }

                    textEdit.Apply();

                    if (textEdit.Canceled)
                        return false;
                }

                if (_editorPrimitives.Selection.IsEmpty)
                {
                    _editorPrimitives.Caret.MoveTo(_editorPrimitives.Caret.StartOfLine);
                }

                return true;
            };

            return ExecuteAction(actionName, action);
        }

        /// <summary>
        /// Expand a range to include the selection as well, if it is not empty, and return it.
        /// </summary>
        private TextRange ExpandRangeToIncludeSelection(TextRange range)
        {
            if (!_editorPrimitives.Selection.IsEmpty)
            {
                if (range.GetStartPoint().CurrentPosition > _editorPrimitives.Selection.GetStartPoint().CurrentPosition)
                {
                    range.SetStart(_editorPrimitives.Selection.GetStartPoint());
                }

                if (range.GetEndPoint().CurrentPosition < _editorPrimitives.Selection.GetEndPoint().CurrentPosition)
                {
                    range.SetEnd(_editorPrimitives.Selection.GetEndPoint());
                }
            }
            return range;
        }

        private void ScrollByLineAndMoveCaretIfNecessary(ScrollDirection direction)
        {
            _textView.ViewScroller.ScrollViewportVerticallyByPixels(direction == ScrollDirection.Up ? _textView.LineHeight : (-_textView.LineHeight));

            //If the caret is not on a fully visible line, move it to the first/last line in the view
            ITextViewLine line = _textView.Caret.ContainingTextViewLine;
            if (line.VisibilityState != VisibilityState.FullyVisible)
            {
                //Decide whether or not the caret is above the top or bottom of the view (since it doesn't lie
                //on any of the fully visible lines in the middle of the view).
                ITextViewLineCollection textLines = _textView.TextViewLines;
                ITextViewLine newCaretLine;

                //The end of the first line should be the start of the fully visible lines so use it to decide
                //whether the caret is above or below.
                ITextViewLine firstVisible = textLines.FirstVisibleLine;
                if (_textView.Caret.Position.BufferPosition < firstVisible.EndIncludingLineBreak)
                {
                    //The caret is above the top of the view, move it to the first fully visible line
                    //(or first partially visible if there is no fully visible line).
                    newCaretLine = firstVisible;
                    if (firstVisible.VisibilityState != VisibilityState.FullyVisible)
                    {
                        ITextViewLine nextLine = textLines.GetTextViewLineContainingBufferPosition(firstVisible.EndIncludingLineBreak);
                        if ((nextLine != null) && (nextLine.VisibilityState == VisibilityState.FullyVisible))
                            newCaretLine = nextLine;
                    }
                }
                else
                {
                    //Otherwise the caret is below the bottom of the view. Move it to the last fully visible line.
                    ITextViewLine lastVisible = textLines.LastVisibleLine;
                    newCaretLine = lastVisible;

                    if ((lastVisible.VisibilityState != VisibilityState.FullyVisible) && (lastVisible.Start > 0))
                    {
                        ITextViewLine previousLine = textLines.GetTextViewLineContainingBufferPosition(lastVisible.Start - 1);
                        if ((previousLine != null) && (previousLine.VisibilityState == VisibilityState.FullyVisible))
                            newCaretLine = previousLine;
                    }
                }

                // Clear any selections
                _textView.Selection.Clear();
                _textView.Caret.MoveTo(newCaretLine);

                //Since the caret is on a fully visible line, this should only scroll horizontally.
                _textView.Caret.EnsureVisible();
            }
        }

        /// <summary>
        /// Determine if the character is a space, which includes everything in
        /// the space separator category *plus* tab and a few others (for Orcas parity).
        /// </summary>
        private static bool IsSpaceCharacter(char c)
        {
            return c == ' ' || c == '\t' ||
                   (int) c == 0x200B ||
                   char.GetUnicodeCategory(c) == UnicodeCategory.SpaceSeparator;
        }

        private bool ExecuteAction(string undoText, Func<bool> action, SelectionUpdate preserveCaretAndSelection = SelectionUpdate.Ignore, bool ensureVisible = false)
        {
            using (ITextUndoTransaction undoTransaction = _undoHistory.CreateTransaction(undoText))
            {
                ITextSnapshot snapshotBeforeEdit = _textView.TextSnapshot;

                // 1. Before performing the action, add a Before undo primitive and remember the current
                //    state of the caret/selection
                AddBeforeTextBufferChangePrimitive();

                CaretPosition oldCaretPosition = _textView.Caret.Position;
                VirtualSnapshotPoint oldSelectionAnchor = _textView.Selection.AnchorPoint;
                VirtualSnapshotPoint oldSelectionActive = _textView.Selection.ActivePoint;

                // 2. Perform the specified action
                if (!action())
                    return false;

                // 3. Take care of the requested SelectionUpdate
                if (preserveCaretAndSelection == SelectionUpdate.Preserve)
                {
                    _textView.Caret.MoveTo(new VirtualSnapshotPoint(new SnapshotPoint(_textView.TextSnapshot,
                                                                                      oldCaretPosition.BufferPosition.Position),
                                                                    oldCaretPosition.VirtualSpaces), oldCaretPosition.Affinity);
                    _textView.Selection.Select(new VirtualSnapshotPoint(new SnapshotPoint(_textView.TextSnapshot,
                                                                                          oldSelectionAnchor.Position),
                                                                        oldSelectionAnchor.VirtualSpaces),
                                                new VirtualSnapshotPoint(new SnapshotPoint(_textView.TextSnapshot,
                                                                                           oldSelectionActive.Position),
                                                                        oldSelectionActive.VirtualSpaces));
                }
                else if (preserveCaretAndSelection == SelectionUpdate.ClearVirtualSpace)
                {
                    _textView.Caret.MoveTo(_textView.Caret.Position.BufferPosition);
                    _textView.Selection.Select(new VirtualSnapshotPoint(_textView.Selection.AnchorPoint.Position),
                                               new VirtualSnapshotPoint(_textView.Selection.ActivePoint.Position));
                }
                else if (preserveCaretAndSelection == SelectionUpdate.ResetUnlessEmptyBox)
                {
                    if (!IsEmptyBoxSelection())
                        ResetSelection();
                }
                else if (preserveCaretAndSelection == SelectionUpdate.Reset)
                {
                    ResetSelection();
                }

                // 4. If requested, make sure the caret is visible
                if (ensureVisible)
                    _textView.Caret.EnsureVisible();

                // 5. Finish up the undo transaction
                AddAfterTextBufferChangePrimitive();

                if (snapshotBeforeEdit != _textView.TextSnapshot)
                {
                    undoTransaction.Complete();
                }
            }

            return true;
        }

        private bool NormalizeLineEndingsHelper(string replacement)
        {
            // what do we do about projection? may wish to change this to navigate to the document buffer via
            // text data model.
            ITextBuffer textBuffer = _editorPrimitives.Buffer.AdvancedTextBuffer;
            using (ITextEdit edit = textBuffer.CreateEdit())
            {
                ITextSnapshot snapshot = edit.Snapshot;
                foreach (ITextSnapshotLine line in snapshot.Lines)
                {
                    if (line.LineBreakLength != 0)
                    {
                        string breakText = line.GetLineBreakText();
                        if (!string.Equals(breakText, replacement, StringComparison.Ordinal))
                        {
                            if (!edit.Replace(line.End, line.LineBreakLength, replacement))
                                return false;
                        }
                    }
                }
                edit.Apply();
                return !edit.Canceled;
            }
        }

        private void DuplicateLine(SnapshotPoint triggerPoint, ITextEdit edit, bool insertBelow = false)
        {
            var line = triggerPoint.GetContainingLine();
            string textToInsert = line.GetText();
            int whereToInsert;

            if (insertBelow)
            {
                whereToInsert = line.End;
                edit.Insert(whereToInsert, TextBufferOperationHelpers.GetNewLineCharacterToInsert(line, _editorOptions));
                edit.Insert(whereToInsert, textToInsert);
            }
            else
            {
                whereToInsert = line.Start;
                edit.Insert(whereToInsert, textToInsert);
                edit.Insert(whereToInsert, TextBufferOperationHelpers.GetNewLineCharacterToInsert(line, _editorOptions));
            }
        }

        public bool DuplicateSelection()
        {
            Func<bool> func = () =>
            {
                using (ITextEdit edit = _textView.TextBuffer.CreateEdit())
                {
                    if (_textView.Selection.IsEmpty)
                    {
                        DuplicateLine(_textView.Caret.Position.BufferPosition, edit);
                    }
                    else
                    {
                        var virtualSelectedSpans = _textView.Selection.VirtualSelectedSpans;

                        // This is used only in the case of zero width block selection in order to maintain the block selection after
                        // our edit. On the first span we want to insert the copied text after the current row so that the top of the block
                        // selection doesn't move. An all others, we want to insert above so the bottom of the block selection does move.
                        bool insertBelow = virtualSelectedSpans.Count > 1;
                        foreach (var virtualSpan in virtualSelectedSpans)
                        {
                            if (virtualSpan.Length > 0)
                            {
                                if (!virtualSpan.IsInVirtualSpace)
                                {
                                    edit.Insert(virtualSpan.Start.Position, virtualSpan.GetText());
                                }
                                else
                                {
                                    // Is this all in virtual space, if so, do nothing
                                    if (!virtualSpan.Start.IsInVirtualSpace)
                                    {
                                        // Ok, we need to pad with whitespace as well as duplicate. Append the amount of virtual space as spaces after the inserted text
                                        // since we're inserting before the caret.
                                        edit.Insert(virtualSpan.Start.Position, virtualSpan.GetText());

                                        int whiteSpaceSize = virtualSpan.Length - virtualSpan.SnapshotSpan.Length;
                                        string insertedSpace = GetWhiteSpaceForPositionAndVirtualSpace(virtualSpan.SnapshotSpan.End, whiteSpaceSize, useBufferPrimitives: true);
                                        edit.Insert(virtualSpan.Start.Position, insertedSpace);
                                    }
                                }
                            }
                            else
                            {
                                // This must be a zero-width block selection, treat like several instances of no-selection and just duplicate the lines.
                                DuplicateLine(virtualSpan.Start.Position, edit, insertBelow);
                                insertBelow = false;
                            }
                        }
                    }

                    edit.Apply();
                    return !(edit.HasFailedChanges || edit.Canceled);
                }
            };
            return ExecuteAction(Strings.DuplicateSelection, func, ensureVisible: true);
        }

        /// <summary>
        /// Sorts the selected lines in alphabetical order.
        /// </summary>
        public void SortSelectedLines()
        {
            // we are technically caching the selections, although we need to
            // be really careful - all we really care about are the positions
            // as we'll want to recreate them when we finish modifying the text
            var selectionCache = new List<Selection>();

            using (var batchOp = _multiSelectionBroker.BeginBatchOperation())
            {
                _multiSelectionBroker.PerformActionOnAllSelections(transformer =>
                {
                    // we start by getting all the lines that are in the current selection
                    var span = transformer.Selection.Extent.SnapshotSpan;

                    // we need the text view lines, specifically, so we ask for those
                    // that are intersecting the span that defines our selection
                    var lines = this.TextView
                        .TextViewLines
                        .GetTextViewLinesIntersectingSpan(span)
                        .Select(lx => new SnapshotSpan(lx.Start, lx.EndIncludingLineBreak))
                        .ToArray();

                    if (lines.Length <= 1) return;

                    var text = lines.Select(x => x.GetText()).ToArray();

                    // we only create this copy so that we can reverse it, in case the array
                    // is already sorted
                    var copy = (string[])text.Clone();
                    Array.Sort(text, StringComparer.Ordinal);

                    if (Enumerable.SequenceEqual(text, copy))
                    {
                        Array.Reverse(text);
                    }

                    selectionCache.Add(transformer.Selection);

                    ExecuteAction("SortLines", () =>
                    {
                        // we can simply replace the *entire* text, from the start of
                        // the first line, to the last, without caring about the
                        // lines themsleves (unlike in JoinSelectedLines),
                        // so this is much nicer!
                        var l = string.Concat(text);
                        var span2 = Span.FromBounds(lines.First().Start, lines.Last().End);

                        return ReplaceHelper(span2, l);
                    }, SelectionUpdate.Ignore, true);
                });
            }

            foreach (var item in selectionCache)
            {
                // we need to create a new snapshot point, otherwise the
                // broker (rightfully) throws an exception about the selection
                // existing in a different snapshot
                var anchorPoint = new VirtualSnapshotPoint(_textView.TextSnapshot, item.AnchorPoint.Position);
                var activePoint = new VirtualSnapshotPoint(_textView.TextSnapshot, item.ActivePoint.Position);

                _multiSelectionBroker.AddSelection(new Selection(anchorPoint, activePoint));
            }
        }

        /// <summary>
        /// Joins the selected lines into a single one.
        /// </summary>
        public void JoinSelectedLines()
        {
            using (var batchOp = _multiSelectionBroker.BeginBatchOperation())
            {
                // since the user could have multiple selections, we should
                // perform this merge/join on each of them separately, but
                // we need to be careful to make sure the selections are
                // maintained as we change the buffer underneath them, so we'll
                // use the stuff built in
                _multiSelectionBroker.PerformActionOnAllSelections(transformer =>
                {
                    // we start by getting all the lines that are in the current selection
                    var span = transformer.Selection.Extent.SnapshotSpan;

                    // we need the text view lines, specifically, so we ask for those
                    // that are intersecting the span that defines our selection
                    var lines = this.TextView.TextViewLines.GetTextViewLinesIntersectingSpan(span);

                    // we are going to replace the line break, so we can include
                    // that/those particular character(s) in our array
                    var arLines = lines.Select(line =>
                               new SnapshotSpan(line.Start, line.EndIncludingLineBreak));

                    // we trim to get rid of spaces, tabs, etc., and replace them
                    var newLine = string.Join(" ", arLines.Select(x => x.GetText().Trim()));

                    // we are genuinely only interested in the first and last lines,
                    // as their start and end might differ from the extent of the
                    // selection that is defined. But to avoid iterating, we can
                    // store them in a variable here.
                    var firstLine = lines.First();
                    var lastLine = lines.Last();

                    // next, we need to leave the indent of the first line alone, as
                    // otherwise we need to be calling some weird functions and creating
                    // new edits, etc. Easiest way to do this is to simply go from
                    // the start position and move the position to the right until
                    // we find a non whitespace character (or reach the end of the line)
                    int startPosition = firstLine.Start.Position;

                    while (firstLine.End > startPosition &&
                        IsSpaceCharacter(firstLine.Snapshot[startPosition]))
                    {
                        startPosition++;
                    }

                    // to get the correct new line character(s), we need a snapshot
                    // line, which is obviously different, so let's get that one
                    VirtualSnapshotPoint caret = transformer.Selection.InsertionPoint;
                    ITextSnapshotLine snapshotLine = caret.Position.GetContainingLine();

                    // while this is not the prettiest thing in the world, it's going
                    // to likely be just as efficient as calling string.Join for
                    // just two strings
                    newLine += TextBufferOperationHelpers.GetNewLineCharacterToInsert(snapshotLine, _editorOptions);

                    // in order to preserve the selection, we need to call this method
                    // with SelectionUpdate.Ignore. This will ensure the following
                    // selections are left alone. Additionally, each call will create
                    // a new undo transaction - the user can then undo one by one.
                    ExecuteAction(Strings.ReplaceText, () =>
                    {
                        // while the extent of the selection is also a span, we likely
                        // moved the starting point, to match the first non-whitespace
                        // character, so we can just create a new span which will
                        // be used for replacing
                        var span2 = Span.FromBounds(startPosition,
                        lastLine.EndIncludingLineBreak.Position);
                        return ReplaceHelper(span2, newLine);
                    }, SelectionUpdate.Ignore, true);
                });
            }
        }

        public string NormalizeNewlinesInString(string text)
        {
            if (_factory.WhitespaceManagerFactory.TryGetExistingWhitespaceManager(_textView.TextDataModel.DocumentBuffer, out var whitespaceManager))
            {
                return whitespaceManager.NewlineState.NormalizeNewlines(text);
            }
            else
            {
                return text;
            }
        }
    }

    /// <summary>
    /// The maximum number of characters copied as rich text to the clipboard.
    /// </summary>
    /// <remarks>
    /// Added as a local class here to avoid needing to patch text.logic (where this would, normally, go).
    /// </remarks>
    [Export(typeof(EditorOptionDefinition))]
    [Name(MaxRtfCopyLength.OptionName)]
    [Shared]
    public sealed class MaxRtfCopyLength : EditorOptionDefinition<int>
    {
        public const string OptionName = "MaxRtfCopyLength";
        public static readonly EditorOptionKey<int> OptionKey = new EditorOptionKey<int>(MaxRtfCopyLength.OptionName);

        /// <summary>
        /// Gets the default value (10K).
        /// </summary>
        public override int Default { get { return 10 * 1024; } }

        /// <summary>
        /// Gets the editor option key.
        /// </summary>
        public override EditorOptionKey<int> Key { get { return MaxRtfCopyLength.OptionKey; } }
    }

    [Export(typeof(EditorOptionDefinition))]
    [Name(EnableRtfCopy.OptionName)]
    [Shared]
    public sealed class EnableRtfCopy : EditorOptionDefinition<bool>
    {
        public const string OptionName = "EnableRtfCopy";
        public static readonly EditorOptionKey<bool> OptionKey = new EditorOptionKey<bool>(EnableRtfCopy.OptionName);

        public override bool Default { get { return true; } }

        /// <summary>
        /// Gets the editor option key.
        /// </summary>
        public override EditorOptionKey<bool> Key { get { return EnableRtfCopy.OptionKey; } }
    }

    [Export(typeof(EditorOptionDefinition))]
    [Name(UseAccurateClassificationForRtfCopy.OptionName)]
    [Shared]
    public sealed class UseAccurateClassificationForRtfCopy : EditorOptionDefinition<bool>
    {
        public const string OptionName = "UseAccurateClassificationForRtfCopy";
        public static readonly EditorOptionKey<bool> OptionKey = new EditorOptionKey<bool>(UseAccurateClassificationForRtfCopy.OptionName);

        public override bool Default { get { return false; } }

        /// <summary>
        /// Gets the editor option key.
        /// </summary>
        public override EditorOptionKey<bool> Key { get { return UseAccurateClassificationForRtfCopy.OptionKey; } }
    }
}
