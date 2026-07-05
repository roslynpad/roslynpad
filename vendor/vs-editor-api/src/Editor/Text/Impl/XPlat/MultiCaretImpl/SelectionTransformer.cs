using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Text.MultiSelection;

namespace Microsoft.VisualStudio.Text.MultiSelection.Implementation
{
    internal class SelectionTransformer : ISelectionTransformer, IDisposable
    {
        private Selection _selection;
        private SelectionUIProperties _uiProperties;
        private VirtualSnapshotPoint _preferredXReference;
        private MultiSelectionBroker _broker;
        private ITextSnapshot _currentSnapshot;
        private HashSet<Selection> _historicalSelections = new HashSet<Selection>();
        private bool _highFidelityMode;
        private bool _isDisposed = false;

        public SelectionTransformer(MultiSelectionBroker multiSelectionBroker, Selection selection)
        {
            _broker = multiSelectionBroker;
            _selection = selection;

            // This can be any of the points on the selection, since they are validated to be the same in the selection constructor.
            _currentSnapshot = _selection.InsertionPoint.Position.Snapshot;
            if (_currentSnapshot != _broker.CurrentSnapshot)
            {
                throw new ArgumentException("The provided selection is on a different snapshot than the broker.", nameof(selection));
            }

            CapturePreferredReferencePoint();
        }

        public Selection Selection
        {
            get
            {
                this.CheckIsValid();

                return _selection;
            }
            internal set
            {
                this.CheckIsValid();

                if (_selection != value)
                {
                    _selection = value;
                }
            }
        }

        public SelectionUIProperties UIProperties
        {
            get
            {
                this.CheckIsValid();

                if (_uiProperties == null)
                {
                    _uiProperties = new SelectionUIProperties(_broker.Factory, _broker, this);
                }
                return _uiProperties;
            }
        }

        public void CapturePreferredXReferencePoint()
        {
            this.CheckIsValid();

            if (_broker.TextView.TextViewLines == null)
            {
                // We're not initialized yet, just grab the beginning of the document
                _preferredXReference = new VirtualSnapshotPoint(_currentSnapshot, 0);
                this.UIProperties.SetPreferredXCoordinate(0.0);
            }
            else
            {
                _preferredXReference = _selection.InsertionPoint;
                var referenceLine = _broker.TextView.GetTextViewLineContainingBufferPosition(_preferredXReference.Position);
                this.UIProperties.SetPreferredXCoordinate(GetXCoordinateFromVirtualBufferPosition(referenceLine, _preferredXReference));
            }
        }

        public void CapturePreferredYReferencePoint()
        {
            this.CheckIsValid();

            if (_broker.TextView.TextViewLines == null)
            {
                // Quit early if we aren't working with an initialized view.
                this.UIProperties.SetPreferredYCoordinate(0.0);
                return;
            }

            //Use the bounds if they are meaningful (i.e. the caret is on a line formatted by the view).

            //Use the bounds if they are meaningful (i.e. the caret is on a line formatted by the view).
            ITextViewLine containingLine = _broker.TextView.GetTextViewLineContainingBufferPosition(_selection.InsertionPoint.Position);
            if ((containingLine.VisibilityState == VisibilityState.Unattached) || (containingLine.VisibilityState == VisibilityState.Hidden))
            {
                //Caret is not on screen ... use the first or last visible line instead of the containing line.
                containingLine = _broker.TextView.TextViewLines.LastVisibleLine;
                if (_selection.InsertionPoint.Position.Position < containingLine.Start.Position)
                {
                    containingLine = _broker.TextView.TextViewLines.FirstVisibleLine;
                }
            }

            //Since viewportTop is arbitrary, track only the distance from the top of the view to the line.
            this.UIProperties.SetPreferredYCoordinate((containingLine.TextTop + containingLine.TextHeight * 0.5) - _broker.TextView.ViewportTop);
        }

        public void CapturePreferredReferencePoint()
        {
            this.CheckIsValid();

            CapturePreferredXReferencePoint();
            CapturePreferredYReferencePoint();
        }

        internal IDisposable HighFidelityOperation()
        {
            Debug.Assert(!_highFidelityMode, "Already in high fidelity mode");

            _highFidelityMode = true;
            return new DelegateDisposable(() =>
            {
                _highFidelityMode = false;
                _historicalSelections.Clear();
            });
        }

        internal IReadOnlyCollection<Selection> HistoricalRegions => _historicalSelections;

        internal ITextSnapshot CurrentSnapshot
        {
            get
            {
                return _currentSnapshot;
            }
            set
            {
                if (_currentSnapshot != value)
                {
                    if (_highFidelityMode)
                    {
                        _historicalSelections.Add(_selection);
                    }

                    _currentSnapshot = value;
                    bool emptyBefore = _selection.IsEmpty;
                    _selection = _selection.MapToSnapshot(value, _broker.TextView);

                    if (emptyBefore != _selection.IsEmpty)
                    {
                        _broker.QueueCaretUpdatedEvent(this);
                    }

                    CapturePreferredReferencePoint();
                }
            }
        }

        private ITextViewLine GetPreferredLine()
        {
            return _broker.TextView.TextViewLines.GetTextViewLineContainingYCoordinate(PreferredYCoordinate) ??
                _broker.TextView.TextViewLines.LastVisibleLine;
        }

        private VirtualSnapshotPoint GetPreferredXLocationOnLine(ITextViewLine line)
        {
            var referenceLine = _broker.TextView.GetTextViewLineContainingBufferPosition(_preferredXReference.Position);
            var preferredXCoordinate = GetXCoordinateFromVirtualBufferPosition(referenceLine, _preferredXReference);
            var adjustedXCoord = line.MapXCoordinate(_broker.TextView, preferredXCoordinate, _broker.Factory.SmartIndentationService, userSpecifiedXCoordinate: false);
            return line.GetInsertionBufferPositionFromXCoordinate(adjustedXCoord);
        }

        /// <summary>
        /// Get the caret x coordinate for a virtual buffer position.
        /// </summary>
        /// <remarks>
        /// The x coordinate is always on the trailing edge of the previous character,
        /// *unless* the supplied buffer position is the first character on the line or
        /// is in virtual space.
        /// </remarks>
        internal static double GetXCoordinateFromVirtualBufferPosition(ITextViewLine textLine, VirtualSnapshotPoint bufferPosition)
        {
            return (bufferPosition.IsInVirtualSpace || bufferPosition.Position == textLine.Start) ?
                textLine.GetExtendedCharacterBounds(bufferPosition).Leading :
                textLine.GetExtendedCharacterBounds(bufferPosition.Position - 1).Trailing;
        }

        private double PreferredYCoordinate
        {
            get
            {
                //In the cases where the caret extends off the top and bottom of the view, capture only the visible bounds.
                return Math.Max(_broker.TextView.ViewportTop,
                    Math.Min(_broker.TextView.ViewportBottom, this.UIProperties.PreferredYCoordinate + _broker.TextView.ViewportTop));
            }
        }

        /// <summary>
        /// Determines whether this transformer should be considered as changed by the current operation. This is used
        /// by normalization code which checks after all operations have completed to ensure that we don't leave a caret
        /// or selection sitting in the middle of a collapsed region or multi-byte character. It starts as true so that we
        /// check by default for all new transformers. We need this because the actual normalizing check does expensive formatting
        /// which we'd like to avoid if possible for selections that were not changed by an operation.
        /// </summary>
        internal bool ModifiedByCurrentOperation { get; set; } = true;

        private static SnapshotPoint GetFirstNonWhiteSpaceCharacterInSpan(SnapshotSpan span)
        {
            var toReturn = span.Start;

            while (toReturn != span.End && char.IsWhiteSpace(span.Snapshot[toReturn]))
            {
                toReturn += 1;
            }

            return toReturn;
        }

        /// <summary>
        /// Determine if a the next word should stop at the end of the line.
        /// </summary>
        private static bool ShouldStopAtEndOfLine(int endOfWord, int endOfLine, int currentPosition)
        {
            // If the current word ends at the end of the line and the current position is not the end of the line,
            // then the word movement should stop at the end of the line.
            return (endOfWord == endOfLine) && (endOfLine > currentPosition);
        }

        private static bool IsSpanABlankLine(SnapshotSpan currentWord, ITextSnapshotLine currentLine)
        {
            return currentWord.IsEmpty && currentWord == currentLine.Extent;
        }

        /// <summary>
        /// Determine if the checking of the previous word should go past the previous word.
        /// </summary>
        /// <param name="previousWord">The current previous word.</param>
        /// <param name="line">The line containing previous word.</param>
        private static bool ShouldContinuePastPreviousWord(SnapshotSpan previousWord, ITextSnapshotLine line)
        {
            // If the previous word is whitespace, and the previous word is not the start of the line
            // then it should be included in the previous word.
            return char.IsWhiteSpace(previousWord.Snapshot[previousWord.Start]) && previousWord.Start != line.Start;
        }

        private SnapshotPoint NextCharacter(SnapshotPoint current)
        {
            if (current.Snapshot.Length == current.Position)
            {
                return current;
            }

            return _broker.TextView.GetTextElementSpan(current).End;
        }

        private SnapshotPoint PreviousCharacter(SnapshotPoint current)
        {
            if (current.Position == 0)
            {
                return current;
            }

            return _broker.TextView.GetTextElementSpan(current - 1).Start;
        }

        private SnapshotSpan GetNextWord()
        {
            var currentPosition = _selection.InsertionPoint.Position;
            var currentWord = _broker.TextStructureNavigator.GetExtentOfWord(currentPosition);
            var currentLine = currentPosition.GetContainingLine();

            if (currentWord.Span.End < _currentSnapshot.Length)
            {
                // Get the current caret position
                int startPosition = currentPosition.Position;
                int endOfLine = currentLine.End.Position;

                if (startPosition >= endOfLine)
                {
                    // Move the caret to the next line since it is at the end of the current line
                    currentPosition = currentLine.EndIncludingLineBreak;
                    currentLine = currentPosition.GetContainingLine();
                    endOfLine = currentLine.End.Position;

                    // Move past whitespace on the next line
                    while (currentPosition.Position < endOfLine && char.IsWhiteSpace(currentPosition.Snapshot[currentPosition.Position]))
                    {
                        currentPosition = NextCharacter(currentPosition);
                    }

                    return new SnapshotSpan(CurrentSnapshot, currentPosition.Position, 0);
                }

                currentPosition = NextCharacter(currentPosition);

                // If we are at the end of the line, stop looking for the next word - we want the caret to
                // stop at the end of each line.
                if (currentPosition >= endOfLine)
                {
                    return new SnapshotSpan(CurrentSnapshot, currentPosition.Position, 0);
                }

                // Skip past whitespace.
                while (currentPosition < endOfLine && char.IsWhiteSpace(currentPosition.Snapshot[currentPosition.Position]))
                {
                    currentPosition = NextCharacter(currentPosition);
                }

                // If the position is still not at the end of the line get the current
                // word.
                if (currentPosition < endOfLine)
                {
                    currentWord = _broker.TextStructureNavigator.GetExtentOfWord(currentPosition);

                    if (currentWord.Span.Start < currentPosition)
                    {
                        // If the current word starts before the current position, move to the end of the 
                        // current word.
                        currentPosition = currentWord.Span.End;
                    }

                    bool hitWhitespace = false;
                    // Skip past whitespace at the end of the word.
                    while (currentPosition < endOfLine && char.IsWhiteSpace(currentPosition.Snapshot[currentPosition.Position]))
                    {
                        hitWhitespace = true;
                        currentPosition = NextCharacter(currentPosition);
                    }

                    return hitWhitespace ? new SnapshotSpan(CurrentSnapshot, currentPosition, 0) : _broker.TextStructureNavigator.GetExtentOfWord(currentPosition).Span;
                }
                else if (currentPosition == endOfLine)
                {
                    // Just wrap one line be done
                    currentPosition = NextCharacter(currentPosition);
                }

                return new SnapshotSpan(currentPosition, currentPosition);
            }

            return new SnapshotSpan(_currentSnapshot, _currentSnapshot.Length, 0);
        }

        private SnapshotSpan GetPreviousWord()
        {
            var position = _selection.InsertionPoint.Position;
            var word = _broker.TextStructureNavigator.GetExtentOfWord(position);
            var line = position.GetContainingLine();

            if (word.Span.Start == 0)
            {
                // We can't move back anymore, just give the start of the document as an empty span.
                return new SnapshotSpan(_currentSnapshot, 0, 0);
            }

            if (word.Span.Start == line.Start && !_selection.InsertionPoint.IsInVirtualSpace)
            {
                // We're starting at the beginning of the line, jump to the end of the previous line, then continute the algorithm as normal.
                var lineNumber = line.LineNumber;
                line = _broker.CurrentSnapshot.GetLineFromLineNumber(Math.Max(0, lineNumber - 1));
                position = line.End;

                // If the line is empty, just return
                if (line.Extent.IsEmpty)
                {
                    return line.Extent;
                }

                // Make sure we're not in the middle of a text element like a hidden region, a multi-byte char or something else weird.
                position = _broker.TextView.GetTextElementSpan(position).Start;
                word = _broker.TextStructureNavigator.GetExtentOfWord(position);
            }

            // By default, VS stops at line breaks when determing word
            // boundaries.
            if ((word.Span.Start == line.Start) &&
                (position != line.Start || _selection.InsertionPoint.IsInVirtualSpace))
            {
                return new SnapshotSpan(line.Start, line.Start);
            }

            // If the point is not at the beginning of a word that is not whitespace, it is possible
            // that the "current word" is also the word we wish to navigate to the beginning of.
            if ((word.Span.Start != position) && (!word.Span.IsEmpty) && word.IsSignificant)
            {
                return word.Span;
            }

            // Ok, we have to start moving backwards through the document.
            do
            {
                position = PreviousCharacter(position);

                // Skip past whitespace at the start of the word.
            } while (position > line.Start && char.IsWhiteSpace(position.GetChar()));

            // Again, VS stops at line breaks when determining word boundaries
            if (position <= line.Start)
            {
                return new SnapshotSpan(position, position);
            }

            word = _broker.TextStructureNavigator.GetExtentOfWord(position);
            line = position.GetContainingLine();

            if (word.Span.Start > 0)
            {
                if (ShouldContinuePastPreviousWord(word.Span, line))
                {
                    // Move back one more time
                    position = PreviousCharacter(position);
                    word = _broker.TextStructureNavigator.GetExtentOfWord(position);
                }
            }

            return word.Span;
        }

        public void MoveTo(VirtualSnapshotPoint point, bool select, PositionAffinity insertionPointAffinity)
        {
            this.CheckIsValid();

            if (_selection.ActivePoint != point
                || _selection.InsertionPoint != point
                || (!select && _selection.AnchorPoint != point)
                || _selection.InsertionPointAffinity != insertionPointAffinity)
            {
                var newSelection = new Selection(point, select ? _selection.AnchorPoint : point, point, insertionPointAffinity);

                if (_broker.IsOldEditor)
                {
                    _selection = newSelection;
                }
                else
                {
                    // Using the ternary here to shortcut out if the snapshots are the same. There's a similar check in the
                    // MapSelectionToCurrentSnapshot method to avoid doing unneeded work, but even spinning up the method call can be expensive.
                    _selection = (newSelection.InsertionPoint.Position.Snapshot == this.CurrentSnapshot)
                        ? newSelection
                        : MapSelectionToCurrentSnapshot(newSelection);
                }

                _broker.QueueCaretUpdatedEvent(this);
            }
        }

        private bool LogException(Exception ex)
        {
            _broker.Factory.GuardedOperations.HandleException(this, ex);
            return true;
        }

        private Selection MapSelectionToCurrentSnapshot(Selection newSelection)
        {
            if (newSelection.InsertionPoint.Position.Snapshot != this.CurrentSnapshot)
            {
                try
                {
                    throw new InvalidOperationException("Selection does not match the current snapshot.");
                }
                catch (InvalidOperationException ex) when (LogException(ex))
                {
                    // This will catch every time, since LogException always returns true.

                    // We really should throw here every time, but to limit the number of crashes we see from this immediately
                    // we are doing a best effort here, and only throwing when we can't map forward. Additionally, we're logging
                    // faults with guarded operations in order to identify bad actors so we can fix them before converting this
                    // to the 'throw always' route.

                    // This can still throw if mapping doesn't happen, but it should be significantly less often than currently.
                    newSelection = newSelection.MapToSnapshot(this.CurrentSnapshot, _broker.TextView);
                }
            }

            return newSelection;
        }

        public void MoveTo(VirtualSnapshotPoint anchorPoint, VirtualSnapshotPoint activePoint, VirtualSnapshotPoint insertionPoint, PositionAffinity insertionPointAffinity)
        {
            // See other overload of MoveTo for interesting comments.
            this.CheckIsValid();

            if (_selection.AnchorPoint != anchorPoint
                || _selection.ActivePoint != activePoint
                || _selection.InsertionPoint != insertionPoint
                || _selection.InsertionPointAffinity != insertionPointAffinity)
            {
                var newSelection = new Selection(insertionPoint, anchorPoint, activePoint, insertionPointAffinity);
                _selection = (newSelection.InsertionPoint.Position.Snapshot == this.CurrentSnapshot)
                    ? newSelection
                    : MapSelectionToCurrentSnapshot(newSelection);
                _broker.QueueCaretUpdatedEvent(this);
            }
        }

        public void PerformAction(PredefinedSelectionTransformations action)
        {
            this.CheckIsValid();

            switch (action)
            {
                case PredefinedSelectionTransformations.ClearSelection:
                    ClearSelection();
                    break;
                case PredefinedSelectionTransformations.MovePageDown:
                    MovePageDown(false);
                    break;
                case PredefinedSelectionTransformations.SelectPageDown:
                    MovePageDown(true);
                    break;
                case PredefinedSelectionTransformations.MovePageUp:
                    MovePageUp(false);
                    break;
                case PredefinedSelectionTransformations.SelectPageUp:
                    MovePageUp(true);
                    break;
                case PredefinedSelectionTransformations.MoveToBeginningOfLine:
                    MoveToBeginningOfLine(false);
                    break;
                case PredefinedSelectionTransformations.SelectToBeginningOfLine:
                    MoveToBeginningOfLine(true);
                    break;
                case PredefinedSelectionTransformations.MoveToEndOfDocument:
                    MoveToEndOfDocument(false);
                    break;
                case PredefinedSelectionTransformations.SelectToEndOfDocument:
                    MoveToEndOfDocument(true);
                    break;
                case PredefinedSelectionTransformations.MoveToEndOfLine:
                    MoveToEndOfLine(false);
                    break;
                case PredefinedSelectionTransformations.SelectToEndOfLine:
                    MoveToEndOfLine(true);
                    break;
                case PredefinedSelectionTransformations.MoveToHome:
                    MoveToHome(false);
                    break;
                case PredefinedSelectionTransformations.SelectToHome:
                    MoveToHome(true);
                    break;
                case PredefinedSelectionTransformations.MoveToNextCaretPosition:
                    MoveToNextCaretPosition(false);
                    break;
                case PredefinedSelectionTransformations.SelectToNextCaretPosition:
                    MoveToNextCaretPosition(true);
                    break;
                case PredefinedSelectionTransformations.MoveToNextLine:
                    MoveToNextLine(false);
                    break;
                case PredefinedSelectionTransformations.SelectToNextLine:
                    MoveToNextLine(true);
                    break;
                case PredefinedSelectionTransformations.MoveToNextWord:
                    MoveToNextWord(false);
                    break;
                case PredefinedSelectionTransformations.SelectToNextWord:
                    MoveToNextWord(true);
                    break;
                case PredefinedSelectionTransformations.MoveToPreviousCaretPosition:
                    MoveToPreviousCaretPosition(false);
                    break;
                case PredefinedSelectionTransformations.SelectToPreviousCaretPosition:
                    MoveToPreviousCaretPosition(true);
                    break;
                case PredefinedSelectionTransformations.MoveToPreviousLine:
                    MoveToPreviousLine(false);
                    break;
                case PredefinedSelectionTransformations.SelectToPreviousLine:
                    MoveToPreviousLine(true);
                    break;
                case PredefinedSelectionTransformations.MoveToPreviousWord:
                    MoveToPreviousWord(false);
                    break;
                case PredefinedSelectionTransformations.SelectToPreviousWord:
                    MoveToPreviousWord(true);
                    break;
                case PredefinedSelectionTransformations.MoveToStartOfDocument:
                    MoveToStartOfDocument(false);
                    break;
                case PredefinedSelectionTransformations.SelectToStartOfDocument:
                    MoveToStartOfDocument(true);
                    break;
                case PredefinedSelectionTransformations.SelectCurrentWord:
                    SelectCurrentWord();
                    break;
                default:
                    Debug.Fail("Using unknown 'predefined' edit manipulation");
                    break;
            }
        }

        #region Predefined Caret Manipulations
        public void ClearSelection()
        {
            if (!_selection.IsEmpty)
            {
                _selection = new Selection(_selection.InsertionPoint, Selection.InsertionPoint, Selection.InsertionPoint, PositionAffinity.Successor);
                _broker.QueueCaretUpdatedEvent(this);
            }
        }

        private void MovePageDown(bool select)
        {
            //If we scroll a full page, the bottom of the last fully visible line will be
            //positioned at 0.0.
            ITextViewLine lastVisibleLine = _broker.TextView.TextViewLines.LastVisibleLine;
            ITextViewLine newCaretLine;

            // If the last line in the buffer is fully visible , then just move the caret to
            // the last visible line
            if ((lastVisibleLine.VisibilityState == VisibilityState.FullyVisible) &&
                (lastVisibleLine.End == lastVisibleLine.Snapshot.Length))
            {
                newCaretLine = lastVisibleLine;
            }
            else
            {
                SnapshotPoint oldFullyVisibleStart = ((lastVisibleLine.VisibilityState == VisibilityState.FullyVisible) || (lastVisibleLine.Start == 0))
                                           ? lastVisibleLine.Start
                                           : (lastVisibleLine.Start - 1); //Actually just a point on the previous line.

                if (_broker.TextView.ViewScroller.ScrollViewportVerticallyByPage(ScrollDirection.Down))
                {
                    ITextViewLine newLastLine = _broker.TextView.TextViewLines.GetTextViewLineContainingBufferPosition(oldFullyVisibleStart);
                    if (newLastLine.Bottom > _broker.TextView.ViewportTop)
                    {
                        newCaretLine = _broker.TextView.TextViewLines.LastVisibleLine;
                    }
                    else
                    {
                        newCaretLine = GetPreferredLine();
                    }
                }
                else
                {
                    // Nothing to do here, since we can't scroll further
                    return;
                }
            }

            this.MoveTo(GetPreferredXLocationOnLine(newCaretLine), select, PositionAffinity.Successor);
        }

        private void MovePageUp(bool select)
        {
            //If we scrolled a full page, then the bottom of the first fully visible line will be below
            //the bottom of the view.
            ITextViewLine firstVisibleLine = _broker.TextView.TextViewLines.FirstVisibleLine;
            SnapshotPoint oldFullyVisibleStart = (firstVisibleLine.VisibilityState == VisibilityState.FullyVisible)
                                       ? firstVisibleLine.Start
                                       : firstVisibleLine.EndIncludingLineBreak; //Start of next line.

            if (_broker.TextView.ViewScroller.ScrollViewportVerticallyByPage(ScrollDirection.Up))
            {
                ITextViewLine newFirstLine = _broker.TextView.TextViewLines.GetTextViewLineContainingBufferPosition(oldFullyVisibleStart);

                ITextViewLine newCaretLine;
                //The old fully visible line should -- if we scrolled as much as we could -- be partially
                //obscured. The shortfall between a full page and what we actually scrolled is the distance
                //between the bottom of that line and the bottom of the screen.
                if (_broker.TextView.ViewportBottom > newFirstLine.Bottom)
                {
                    newCaretLine = _broker.TextView.TextViewLines.FirstVisibleLine;
                }
                else
                {
                    newCaretLine = GetPreferredLine();
                }

                this.MoveTo(GetPreferredXLocationOnLine(newCaretLine), select, PositionAffinity.Successor);
            }
        }
        private void MoveToBeginningOfLine(bool select)
        {
            var currentLineBasis = select ? _selection.InsertionPoint : Selection.Start;
            var line = _broker.TextView.GetTextViewLineContainingBufferPosition(currentLineBasis.Position);
            var startPoint = line.Start;
            this.MoveTo(new VirtualSnapshotPoint(startPoint), select, PositionAffinity.Successor);
            CapturePreferredReferencePoint();
        }

        private void MoveToEndOfDocument(bool select)
        {
            this.MoveTo(new VirtualSnapshotPoint(_currentSnapshot, _currentSnapshot.Length), select, PositionAffinity.Successor);
            CapturePreferredReferencePoint();
        }

        private void MoveToEndOfLine(bool select)
        {
            var currentLineBasis = select ? _selection.InsertionPoint : Selection.End;
            var line = _broker.TextView.GetTextViewLineContainingBufferPosition(currentLineBasis.Position);

            if (line.Extent.IsEmpty)
            {
                // Toggle using virtual whitespace to go to the SmartIndent or beginning of the line
                if (currentLineBasis.IsInVirtualSpace)
                {
                    this.MoveTo(new VirtualSnapshotPoint(currentLineBasis.Position), select, PositionAffinity.Successor);
                }
                else
                {
                    int? indentation = _broker.Factory.SmartIndentationService.GetDesiredIndentation(_broker.TextView, line.Start.GetContainingLine());
                    this.MoveTo(new VirtualSnapshotPoint(currentLineBasis.Position, indentation ?? 0), select, PositionAffinity.Successor);
                }
            }
            else
            {
                // Just go to the end of the line.
                var endPoint = line.End;
                this.MoveTo(new VirtualSnapshotPoint(endPoint), select, PositionAffinity.Successor);
            }
            CapturePreferredReferencePoint();
        }

        private void MoveToHome(bool select)
        {
            var insertionPoint = _selection.InsertionPoint;
            var line = _broker.TextView.GetTextViewLineContainingBufferPosition(insertionPoint.Position);
            var newPosition = GetFirstNonWhiteSpaceCharacterInSpan(line.Extent);

            // If the caret is already at the first non-whitespace character or
            // the line is entirely whitepsace, move to the start of the view line.
            if (newPosition == _selection.InsertionPoint.Position ||
                newPosition == line.End)
            {
                newPosition = line.Start;
            }

            this.MoveTo(new VirtualSnapshotPoint(newPosition), select, PositionAffinity.Successor);
            CapturePreferredReferencePoint();
        }

        private void MoveToNextCaretPosition(bool select)
        {
            // Is this trying to clear a selection?
            if (!select && !_selection.IsEmpty)
            {
                // Just move to the end of the selction
                this.MoveTo(_selection.End, select, PositionAffinity.Successor);
                CapturePreferredReferencePoint();
                return;
            }

            VirtualSnapshotPoint moveTo;
            ITextViewLine line = _broker.TextView.GetTextViewLineContainingBufferPosition(_selection.InsertionPoint.Position);
            if (_selection.InsertionPoint.Position == line.End && line.IsLastTextViewLineForSnapshotLine)
            {
                //At the physical end of a line, either increase virtual space or move to the start of the next line.
                if (_broker.TextView.IsVirtualSpaceOrBoxSelectionEnabled())
                {
                    //Increase virtual spaces by one.
                    moveTo = new VirtualSnapshotPoint(line.End, _selection.InsertionPoint.VirtualSpaces + 1);
                }
                else if (_selection.InsertionPoint.Position != _broker.TextView.TextSnapshot.Length)
                {
                    moveTo = new VirtualSnapshotPoint(line.EndIncludingLineBreak);
                }
                else
                {
                    //At the end of the document, still we might want to update the selection and reference points.
                    moveTo = _selection.InsertionPoint;
                }
            }
            else
            {
                //Not at the end of a line ... just move to the end of the current text element.
                SnapshotSpan textElementSpan = _broker.TextView.GetTextElementSpan(_selection.InsertionPoint.Position);
                moveTo = new VirtualSnapshotPoint(textElementSpan.End);
            }

            this.MoveTo(moveTo, select, PositionAffinity.Successor);
            CapturePreferredReferencePoint();
        }

        private void MoveToNextLine(bool select)
        {
            VirtualSnapshotPoint oldCaretPoint = _selection.InsertionPoint;

            ITextViewLine caretLine;
            if (_selection.IsEmpty || select)
            {
                caretLine = _broker.TextView.GetTextViewLineContainingBufferPosition(_selection.InsertionPoint.Position);
            }
            else
            {
                SnapshotPoint end = _selection.End.Position;
                caretLine = _broker.TextView.GetTextViewLineContainingBufferPosition(end);

                if ((!caretLine.IsFirstTextViewLineForSnapshotLine) && (end.Position == caretLine.Start.Position))
                {
                    //The end of the selection is at the seam between two word-wrapped lines. In this case, we want
                    //the line before (since the selection is drawn to the end of that line rather than to the beginning
                    //of the other).
                    caretLine = _broker.TextView.GetTextViewLineContainingBufferPosition(end - 1);
                }
            }

            if ((!caretLine.IsLastTextViewLineForSnapshotLine) || (caretLine.LineBreakLength != 0)) //If we are not on the last line of the file
            {
                caretLine = _broker.TextView.GetTextViewLineContainingBufferPosition(caretLine.EndIncludingLineBreak);
            }

            this.MoveTo(GetPreferredXLocationOnLine(caretLine), select, PositionAffinity.Successor);
            CapturePreferredYReferencePoint();
        }

        private void MoveToNextWord(bool select)
        {
            var nextWord = GetNextWord();
            this.MoveTo(new VirtualSnapshotPoint(nextWord.Start), select, PositionAffinity.Successor);
            CapturePreferredReferencePoint();
        }

        private void SelectCurrentWord()
        {
            var extent = _broker.TextStructureNavigator.GetExtentOfWord(this.Selection.InsertionPoint.Position);

            // Select word left of caret if the token to the right is just whitespace.
            if (!extent.IsSignificant && (extent.Span.Start.Position > 0))
            {
                extent = _broker.TextStructureNavigator.GetExtentOfWord(this.Selection.InsertionPoint.Position - 1);
            }

            var anchor = new VirtualSnapshotPoint(extent.Span.Start);
            var active = new VirtualSnapshotPoint(extent.Span.End);

            // We want to keep predecessor unless the span is empty because that way the insertion point will be near the blue box.
            this.MoveTo(anchor, active, active, anchor == active ? PositionAffinity.Successor : PositionAffinity.Predecessor);
        }

        private void MoveToPreviousCaretPosition(bool select)
        {
            var inVirtualSpace = _selection.InsertionPoint.IsInVirtualSpace;
            var textViewLine = _broker.TextView.GetTextViewLineContainingBufferPosition(_selection.InsertionPoint.Position);
            var insertionPointAtStartOfLine = (!inVirtualSpace && (_selection.InsertionPoint.Position == textViewLine.Start));

            // Prevent the caret from moving from column 0 to the end of the previous line if either:
            // - virtual space is turned on or
            // - the user is extending a box selection.
            if (insertionPointAtStartOfLine && _broker.TextView.IsVirtualSpaceOrBoxSelectionEnabled())
            {
                return;
            }

            // Is this trying to clear a selection?
            if (!select && !_selection.IsEmpty)
            {
                // Just move to the start of the selction
                this.MoveTo(_selection.Start, select, PositionAffinity.Successor);
                CapturePreferredReferencePoint();

                return;
            }

            // Ok, we actually have to move
            VirtualSnapshotPoint moveTo;
            if (_selection.InsertionPoint.IsInVirtualSpace)
            {
                ITextSnapshotLine line = _broker.TextView.TextSnapshot.GetLineFromPosition(_selection.InsertionPoint.Position);
                int newVirtualSpaces = _broker.TextView.IsVirtualSpaceOrBoxSelectionEnabled() ? (_selection.InsertionPoint.VirtualSpaces - 1) : 0;

                moveTo = new VirtualSnapshotPoint(line.End, newVirtualSpaces);
            }
            else if (_selection.InsertionPoint.Position.Position != 0)
            {
                //Move to the start of the previous text element.
                SnapshotSpan textElementSpan = _broker.TextView.GetTextElementSpan(_selection.InsertionPoint.Position - 1);
                moveTo = new VirtualSnapshotPoint(textElementSpan.Start);
            }
            else
            {
                //Oh, wait, we're at the start of the document. Still we might need to update the selection and preferred references.
                moveTo = _selection.InsertionPoint;
            }

            this.MoveTo(moveTo, select, PositionAffinity.Successor);
            CapturePreferredReferencePoint();
        }

        private void MoveToPreviousLine(bool select)
        {
            VirtualSnapshotPoint oldCaretPoint = _selection.InsertionPoint;

            ITextViewLine caretLine;
            if (_selection.IsEmpty || select)
            {
                caretLine = _broker.TextView.GetTextViewLineContainingBufferPosition(_selection.InsertionPoint.Position);
            }
            else
            {
                caretLine = _broker.TextView.GetTextViewLineContainingBufferPosition(_broker.TextView.Selection.Start.Position);
            }

            if (caretLine.Start != 0)
            {
                caretLine = _broker.TextView.GetTextViewLineContainingBufferPosition(caretLine.Start - 1);
            }

            this.MoveTo(GetPreferredXLocationOnLine(caretLine), select, PositionAffinity.Successor);
            CapturePreferredYReferencePoint();
        }

        private void MoveToPreviousWord(bool select)
        {
            var previousWord = GetPreviousWord();
            SnapshotPoint newInsertionPoint = previousWord.Start;

            if (_selection == _broker.BoxSelection)
            {
                // In extending a box selection, we don't want this to jump to the previous line (if
                // we are on the beginning of a line)
                var line = _broker.TextView.GetTextViewLineContainingBufferPosition(_selection.InsertionPoint.Position);

                if (previousWord.End < line.Start)
                {
                    newInsertionPoint = line.Start;
                }
            }

            this.MoveTo(new VirtualSnapshotPoint(newInsertionPoint), select, PositionAffinity.Successor);
            CapturePreferredReferencePoint();
        }

        private void MoveToStartOfDocument(bool select)
        {
            this.MoveTo(new VirtualSnapshotPoint(_broker.CurrentSnapshot, 0), select, PositionAffinity.Successor);
            CapturePreferredReferencePoint();
        }

        private void CheckIsValid()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(SelectionTransformer), "Using a selection transformer that is not associated with a selection.");
            }
        }

#pragma warning disable CA1063 // Implement IDisposable Correctly
        public void Dispose()
#pragma warning restore CA1063 // Implement IDisposable Correctly
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
                GC.SuppressFinalize(this);
            }
        }

        #endregion
    }
}
