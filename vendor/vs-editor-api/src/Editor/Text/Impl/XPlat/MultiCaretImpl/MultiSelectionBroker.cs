using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.Text.MultiSelection.Implementation
{
    internal class MultiSelectionBroker : IMultiSelectionBroker
    {
        private ITextView _textView;
        private SelectionTransformer _primaryTransformer;
        private List<SelectionTransformer> _selectionTransformers = new List<SelectionTransformer>();
        private ITextSnapshot _currentSnapshot;
        private IDisposable _batchOperation;
        private bool _fireEvents;
        private bool _isActive;
        private double _boxLeft;
        private double _boxRight;
        private SelectionTransformer _boxSelection;
        private ITextStructureNavigator _textStructureNavigator;
        internal readonly MultiSelectionBrokerFactory Factory;
        private SelectionTransformer _mergeWinner;
        private int _maxSelections = 1; // Everybody starts with 1.
        private bool _activationTracksFocus;
        private IDisposable _completionDisabler;
        private IEditorOptions _editorOptions;
        private SelectionTransformer _standaloneTransformation;

        public MultiSelectionBroker(ITextView textView, MultiSelectionBrokerFactory factory)
        {
            Factory = factory;
            _textView = textView;

            if (textView.ToString().Contains("ExtensibleTextEditor"))
            {
                IsOldEditor = true;
            }

            _currentSnapshot = _textView.TextSnapshot;
            var documentStart = new VirtualSnapshotPoint(_textView.TextSnapshot, 0);
            _primaryTransformer = new SelectionTransformer(this, new Selection(documentStart));
            _selectionTransformers.Add(_primaryTransformer);

            // Ignore normal text structure navigators and take the plain text version to keep ownership of word navigation.
            _textStructureNavigator = Factory.TextStructureNavigatorSelectorService.GetTextStructureNavigator(_textView.TextViewModel.EditBuffer);

            _textView.LayoutChanged += OnTextViewLayoutChanged;
            _textView.Closed += OnTextViewClosed;
        }

        internal bool IsOldEditor { get; set; }

        private IEditorOptions EditorOptions
        {
            get
            {
                if (_editorOptions == null)
                {
                    _editorOptions = Factory.EditorOptionsFactoryService.GetOptions(this.TextView);
                }
                return _editorOptions;
            }
        }

        private void OnTextViewLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
        {
            if (IsOldEditor)
            {
                if (CurrentSnapshot != e.NewSnapshot)
                {
                    CurrentSnapshot = e.NewSnapshot;
                }

                return;
            }

            using (var batchOp = BeginBatchOperation())
            {
                // If we get a text change, we need to go through all the selections and update them to be in the
                // new snapshot. If there is just a visual change, we could still need to update selections because
                // word wrap or collapsed regions might have moved around.
                if (CurrentSnapshot != e.NewSnapshot)
                {
                    CurrentSnapshot = e.NewSnapshot;
                }
                else if (e.NewViewState.VisualSnapshot != e.OldViewState.VisualSnapshot)
                {
                    // Box selection is special. Moving _boxSelection is easy, but InnerSetBoxSelection will totally
                    // reset all the selections. It's easier to go a different path here than it is to special case
                    // NormalizeSelections, which is also called when adding an individual selection.
                    if (IsBoxSelection)
                    {
                        // MapToSnapshot does take the visual buffer into account as well. Calling it here should do the right thing
                        // for collapsed regions and word wrap.
                        _boxSelection.Selection = _boxSelection.Selection.MapToSnapshot(_currentSnapshot, _textView);
                        InnerSetBoxSelection();
                    }
                    else
                    {
                        NormalizeSelections(true);
                    }
                }
            }
        }

        private void OnTextViewClosed(object sender, EventArgs e)
        {
            Factory.LoggingService?.PostEvent(@"VS/Editor/MultiSelection", "VS.Editor.MultiSelection.MaxSelections", _maxSelections);
        }

        public ITextView TextView => _textView;

        public IReadOnlyList<Selection> AllSelections
        {
            get
            {
                var selections = new Selection[_selectionTransformers.Count];

                for (int i = 0; i < _selectionTransformers.Count; i++)
                {
                    selections[i] = _selectionTransformers[i].Selection;
                }

                return selections;
            }
        }

        public bool HasMultipleSelections => _selectionTransformers.Count > 1;

        internal ITextStructureNavigator TextStructureNavigator { get => _textStructureNavigator; }

        public NormalizedSnapshotSpanCollection SelectedSpans
        {
            get
            {
                return new NormalizedSnapshotSpanCollection(_selectionTransformers.Select(c => c.Selection.Extent.SnapshotSpan));
            }
        }

        public IReadOnlyList<VirtualSnapshotSpan> VirtualSelectedSpans
        {
            get
            {
                return _selectionTransformers.Select(c => c.Selection.Extent).ToArray();
            }
        }

        public bool AreSelectionsActive
        {
            get
            {
                return _isActive;
            }
            set
            {
                if (value != _isActive)
                {
                    _isActive = value;
                }
            }
        }

        public bool ActivationTracksFocus
        {
            get
            {
                return _activationTracksFocus;
            }
            set
            {
                if (_activationTracksFocus != value)
                {
                    _activationTracksFocus = value;

                    if (_activationTracksFocus)
                        AreSelectionsActive = _textView.HasAggregateFocus;
                }
            }
        }

        public ITextSnapshot CurrentSnapshot
        {
            get
            {
                return _currentSnapshot;
            }
            set
            {
                if (_currentSnapshot != value)
                {
                    using (var batchOp = BeginBatchOperation())
                    {
                        _currentSnapshot = value;

                        if (IsBoxSelection)
                        {
                            _boxSelection.CurrentSnapshot = _currentSnapshot;
                            InnerSetBoxSelection();
                        }
                        else
                        {
                            for (int i = 0; i < _selectionTransformers.Count; i++)
                            {
                                _selectionTransformers[i].CurrentSnapshot = _currentSnapshot;
                            }
                        }
                    }
                }
            }
        }

        public Selection PrimarySelection
        {
            get
            {
                return _primaryTransformer.Selection;
            }
        }

        public VirtualSnapshotSpan SelectionExtent
        {
            get
            {
                if (IsBoxSelection)
                {
                    return _boxSelection.Selection.Extent;
                }

                VirtualSnapshotPoint start, end;
                start = _selectionTransformers[0].Selection.Start;
                end = _selectionTransformers[_selectionTransformers.Count - 1].Selection.End;

                return new VirtualSnapshotSpan(start, end);
            }
        }

        internal void QueueCaretUpdatedEvent(SelectionTransformer transformer)
        {
            // This is set for calls to TransformSelection which doesn't
            // make permanent changes to _selectionTransformers.
            if (transformer == _standaloneTransformation)
            {
                return;
            }

            if (transformer == _boxSelection)
            {
                InnerSetBoxSelection();
            }
            else
            {
                transformer.ModifiedByCurrentOperation = true;
            }

            _fireEvents = true;
            FireSessionUpdatedIfNotBatched();
        }

        public event EventHandler MultiSelectionSessionChanged;

        public void SetSelection(Selection selection)
        {
            if (selection.InsertionPoint.Position.Snapshot != _primaryTransformer.CurrentSnapshot)
            {
                throw new ArgumentOutOfRangeException(nameof(selection), "Selection is on an incompatible snapshot");
            }

            if (_boxSelection != null || _selectionTransformers.Count > 1 || _primaryTransformer.Selection != selection)
            {
                using (var batchOp = BeginBatchOperation())
                {
                    ClearSecondarySelections();

                    _primaryTransformer.Selection = selection;
                    _primaryTransformer.CapturePreferredReferencePoint();

                    _fireEvents = true;
                }
            }
        }

        public void SetSelectionRange(IEnumerable<Selection> range, Selection primary)
        {
            using (var batchOp = BeginBatchOperation())
            {
                if (range == null)
                {
                    SetSelection(primary);
                }
                else
                {
                    this.ClearSecondarySelections();
                    _selectionTransformers.Clear();

                    foreach (var selection in range)
                    {
                        InsertSelectionInOrder(selection);
                    }

                    SelectionTransformer primaryTransformer = null;
                    for (int i = 0; i < _selectionTransformers.Count; i++)
                    {
                        if (_selectionTransformers[i].Selection == primary)
                        {
                            primaryTransformer = _selectionTransformers[i];
                            break;
                        }
                    }

                    if (primaryTransformer == null)
                    {
                        InsertSelectionInOrder(primary);
                    }

                    _primaryTransformer = primaryTransformer;

                    _fireEvents = true;
                }
            }
        }

        public void AddSelection(Selection selection)
        {
            using (var batchOp = BeginBatchOperation())
            {
                if (IsBoxSelection)
                {
                    BreakBoxSelection();
                }

                InsertSelectionInOrder(selection);

                _fireEvents = true;
            }
        }


        public void AddSelectionRange(IEnumerable<Selection> range)
        {
            using (var batchOp = BeginBatchOperation())
            {
                if (IsBoxSelection)
                {
                    BreakBoxSelection();
                }

                foreach (var selection in range)
                {
                    InsertSelectionInOrder(selection);
                }

                _fireEvents = true;
            }
        }

        private void InsertSelectionInOrder(Selection selection)
        {
            var newRegion = new SelectionTransformer(this, selection);
            int insertPoint = GenerateInsertionIndex(selection);
            _selectionTransformers.Insert(insertPoint, newRegion);
        }

        private int GenerateInsertionIndex(Selection inserted)
        {
            int insertPoint = 0;
            for (; insertPoint < _selectionTransformers.Count; insertPoint++)
            {
                if (_selectionTransformers[insertPoint].Selection.InsertionPoint >= inserted.InsertionPoint)
                {
                    break;
                }
            }

            return insertPoint;
        }

        public bool IsBoxSelection
        {
            get
            {
                return _boxSelection != null;
            }
        }

        public Selection BoxSelection { get => _boxSelection?.Selection ?? Selection.Invalid; }

        public IDisposable BeginBatchOperation()
        {
            if (_batchOperation != null)
            {
                var oldBatchOp = _batchOperation;
                return _batchOperation = new DelegateDisposable(() =>
                {
                    _batchOperation = oldBatchOp;
                });
            }

            HashSet<IDisposable> highFidelityOperations = new HashSet<IDisposable>();
            for (int i = 0; i < _selectionTransformers.Count; i++)
            {
                highFidelityOperations.Add(_selectionTransformers[i].HighFidelityOperation());
            }

            return _batchOperation = new DelegateDisposable(() =>
            {
                foreach (var operation in highFidelityOperations)
                {
                    operation.Dispose();
                }

                _batchOperation = null;
                FireSessionUpdated();
            });
        }

        private void FireSessionUpdatedIfNotBatched()
        {
            if (_batchOperation == null)
            {
                FireSessionUpdated();
            }
        }

        private void FireSessionUpdated()
        {
            if (!IsOldEditor)
            {
                var changesFromNormalization = NormalizeSelections();
                _fireEvents = _fireEvents || changesFromNormalization;
            }

            // Perform merges as late as possible so that each region can act independently for operations.
            MergeSelections();

            UpdateTelemetryCounters();
            SetCompletionEnableState();

            if (_fireEvents)
            {
                _fireEvents = false;

                var evt = MultiSelectionSessionChanged;
                if (evt != null)
                {
                    Factory.GuardedOperations.RaiseEvent(this, evt);
                }
            }
        }

        /// <summary>
        /// Takes selections and makes sure they do not occupy space within text elements like collapsed regions or multi-byte
        /// characters.
        /// </summary>
        /// <param name="overrideModifiedFlags">If specified, ignores dirty flags and normalizes everything.</param>
        /// <returns>True if anything changed, false otherwise.</returns>
        private bool NormalizeSelections(bool overrideModifiedFlags = false)
        {
            bool selectionsChanged = false;

            // Normalizing for box selection is a more drastic action, and needs to be done with perf in mind since it can throw away,
            // and recreate large numbers of selections. 
            if (_boxSelection == null)
            {
                for (int i = 0; i < _selectionTransformers.Count; i++)
                {
                    if (overrideModifiedFlags || _selectionTransformers[i].ModifiedByCurrentOperation)
                    {
                        _selectionTransformers[i].ModifiedByCurrentOperation = false;

                        // Mapping to the current snapshot has the side-affect of moving points away from the middle of text elements,
                        // or in other words collapsed regions and multi-byte characters.
                        var normalizedSelection = _selectionTransformers[i].Selection.MapToSnapshot(_currentSnapshot, _textView);

                        if (_selectionTransformers[i].Selection != normalizedSelection)
                        {
                            selectionsChanged = true;
                            _selectionTransformers[i].Selection = normalizedSelection;
                        }
                    }
                }
            }

            return selectionsChanged;
        }

        private IFeatureService FeatureService
        {
            get
            {
                return Factory.FeatureServiceFactory.GetOrCreate(_textView);
            }
        }

        private void SetCompletionEnableState()
        {
            if (this.HasMultipleSelections)
            {
                if (_completionDisabler == null)
                {
                    _completionDisabler = this.FeatureService.Disable(PredefinedEditorFeatureNames.Completion, Factory);
                }
            }
            else
            {
                if (_completionDisabler != null)
                {
                    _completionDisabler.Dispose();
                    _completionDisabler = null;
                }
            }
        }

        private void UpdateTelemetryCounters()
        {
            _maxSelections = Math.Max(_maxSelections, _selectionTransformers.Count);
        }

        private void MergeSelections()
        {
            // _selectionTransformers should already be sorted but, since extenders can do anything in a custom operation,
            // sort just in case.
            _selectionTransformers.Sort(CompareSelections);

            int i = 1;
            while (i < _selectionTransformers.Count)
            {
                var first = _selectionTransformers[i - 1];
                var second = _selectionTransformers[i];

                // Merge either if insertion points match, or spans overlap.
                // If there's no overlap, there still might be an empty selection, check for a contained active point both directions
                if (first.Selection.InsertionPoint == second.Selection.InsertionPoint
                    || first.Selection.Extent.OverlapsWith(second.Selection.Extent)
                    || first.Selection.Extent.Contains(second.Selection.ActivePoint)
                    || second.Selection.Extent.Contains(first.Selection.ActivePoint))
                {
                    SelectionTransformer keep;
                    SelectionTransformer toss;
                    if (_mergeWinner == second)
                    {
                        toss = first;
                        keep = second;
                        _selectionTransformers.RemoveAt(i - 1);
                    }
                    else
                    {
                        toss = second;
                        keep = first;
                        _selectionTransformers.RemoveAt(i);
                    }

                    // Get merged points
                    VirtualSnapshotPoint newAnchor, newActive, newInsertion;
                    PositionAffinity newInsertionAffinity;

                    // First see if we have a winner from earlier manipulations
                    if (_mergeWinner == first || _mergeWinner == second)
                    {
                        // We do, just take their points.
                        newAnchor = _mergeWinner.Selection.AnchorPoint;
                        newActive = _mergeWinner.Selection.ActivePoint;
                        newInsertion = _mergeWinner.Selection.InsertionPoint;
                        newInsertionAffinity = _mergeWinner.Selection.InsertionPointAffinity;
                        // Keep the merge winner around for the rest of the loop, in case they need to win multiple times.
                    }
                    else
                    {
                        (newAnchor, newActive, newInsertion, newInsertionAffinity) = InnerMergeSelections(first, second);
                    }

                    // Don't do SelectionTransformer.MoveTo here because that will fire events we don't want.
                    keep.Selection = new Selection(newInsertion, newAnchor, newActive, newInsertionAffinity);

                    if (toss == _primaryTransformer)
                    {
                        _primaryTransformer = keep;
                        _fireEvents = true;
                    }

                    toss.Dispose();

                    // Don't increment i. We need to merge the winner with the next selection.
                }
                else
                {
                    ++i;
                }
            }

            _mergeWinner = null;
        }

        private static int CompareSelections(SelectionTransformer left, SelectionTransformer right)
        {
            var leftMinimum = left.Selection.Start;
            var rightMinimum = right.Selection.Start;
            return leftMinimum < rightMinimum ? -1 : ((leftMinimum > rightMinimum) ? 1 : 0);
        }

        private static (VirtualSnapshotPoint anchor,
                        VirtualSnapshotPoint active,
                        VirtualSnapshotPoint insertion,
                        PositionAffinity insertionAffinity) InnerMergeSelections(SelectionTransformer first, SelectionTransformer second)
        {
            VirtualSnapshotPoint newAnchor, newActive, newInsertion;
            PositionAffinity newInsertionAffinity = PositionAffinity.Successor;

            // Get the whole span. If an insertion point is on an end, that gets to be the active point.
            var newStart = first.Selection.Start < second.Selection.Start ? first.Selection.Start : second.Selection.Start;
            var newEnd = first.Selection.End > second.Selection.End ? first.Selection.End : second.Selection.End;

            if (first.Selection.InsertionPoint == newStart || second.Selection.InsertionPoint == newStart)
            {
                newInsertion = newStart;
                newActive = newStart;
                newAnchor = newEnd;
            }
            else if (first.Selection.InsertionPoint == newEnd || second.Selection.InsertionPoint == newEnd)
            {
                newInsertion = newEnd;
                newActive = newEnd;
                newAnchor = newStart;
            }
            else
            {
                // Ok, neither one has an insertion point at an end. Just pick a winner, default to anchor is earlier in the document.
                newAnchor = newStart;
                newActive = newEnd;
                newInsertion = first.Selection.InsertionPoint;
            }

            return (newAnchor, newActive, newInsertion, newInsertionAffinity);
        }

        public void ClearSecondarySelections()
        {
            if (_boxSelection != null || _selectionTransformers.Count > 1)
            {
                using (var batchOp = BeginBatchOperation())
                {
                    if (IsBoxSelection)
                    {
                        _boxSelection.Dispose();
                        _boxSelection = null;
                    }

                    for (int i = 0; i < _selectionTransformers.Count; i++)
                    {
                        if (_selectionTransformers[i] != _primaryTransformer)
                        {
                            _selectionTransformers[i].Dispose();
                        }
                    }

                    // Perf: It is faster to clear and add one than it is to remove each as we find them.
                    _selectionTransformers.Clear();
                    _selectionTransformers.Add(_primaryTransformer);

                    _fireEvents = true;
                }
            }
        }

        public void SetBoxSelection(Selection selection)
        {
            if (_boxSelection != null)
            {
                if (_boxSelection.Selection == selection)
                {
                    return;
                }

                _boxSelection?.Dispose();
            }

            _boxSelection = new SelectionTransformer(this, selection);
            InnerSetBoxSelection();
        }

        private void InnerSetBoxSelection()
        {
            using (var batchOp = BeginBatchOperation())
            {
                var startLine = _textView.GetTextViewLineContainingBufferPosition(SelectionExtent.Start.Position);
                var endLine = _textView.GetTextViewLineContainingBufferPosition(SelectionExtent.End.Position);

                for (int i = 0; (i < _selectionTransformers.Count); ++i)
                    _selectionTransformers[i].Dispose();

                _selectionTransformers.Clear();
                _primaryTransformer = null;

                // Purposefully do not use CaretElement.GetXCoordinateFromVirtualBufferPosition, as that will end
                // up giving coordinates that won't work correctly when we try to get the selection position on a
                // given line with ITextViewLine.GetInsertionBufferPositionFromXCoordinate
                var startX = startLine.GetExtendedCharacterBounds(SelectionExtent.Start).Leading;
                var endX = endLine.GetExtendedCharacterBounds(SelectionExtent.End).Leading;

                _boxLeft = Math.Min(startX, endX);
                _boxRight = Math.Max(startX, endX);

                var anchorLine = _textView.GetTextViewLineContainingBufferPosition(_boxSelection.Selection.AnchorPoint.Position);
                var anchorX = anchorLine.GetExtendedCharacterBounds(_boxSelection.Selection.AnchorPoint).Leading;

                bool isLeftRightReversed = (_boxRight == anchorX);

                SnapshotPoint current = _boxSelection.Selection.Start.Position;
                VirtualSnapshotPoint end = _boxSelection.Selection.End;

                int insertionIndex = -1;
                int activeIndex = -1;
                do
                {
                    ITextViewLine line = _textView.GetTextViewLineContainingBufferPosition(current);

                    VirtualSnapshotSpan? spanOnLine = this.GetBoxSelectionSpanOnLine(line);
                    if (spanOnLine.HasValue)
                    {
                        var span = spanOnLine.Value;
                        VirtualSnapshotPoint newAnchor = isLeftRightReversed ? span.End : span.Start;
                        VirtualSnapshotPoint newActive = isLeftRightReversed ? span.Start : span.End;
                        var newSelection = new Selection(span, isLeftRightReversed);
                        if (insertionIndex == -1)
                        {
                            if (span.IntersectsWith(new VirtualSnapshotSpan(_boxSelection.Selection.InsertionPoint, _boxSelection.Selection.InsertionPoint)))
                            {
                                newSelection = new Selection(insertionPoint: _boxSelection.Selection.InsertionPoint, anchorPoint: newAnchor, activePoint: newActive);
                                insertionIndex = _selectionTransformers.Count;
                            }
                            else if ((activeIndex == -1) &&
                                     span.IntersectsWith(new VirtualSnapshotSpan(_boxSelection.Selection.ActivePoint, _boxSelection.Selection.ActivePoint)))
                            {
                                activeIndex = _selectionTransformers.Count;
                            }
                        }

                        var newTransformer = new SelectionTransformer(this, newSelection);
                        _selectionTransformers.Add(newTransformer);
                    }

                    if (line.LineBreakLength == 0 && line.IsLastTextViewLineForSnapshotLine)
                        break;      //Just processed last text view line in buffer.

                    current = line.EndIncludingLineBreak;
                }
                while ((current.Position <= end.Position.Position) ||                               //Continue while the virtual space version of current
                       (end.IsInVirtualSpace && (current.Position == end.Position.Position)));      //is less than the virtual space position of the end of selection.

                _primaryTransformer = _selectionTransformers[(insertionIndex != -1) ? insertionIndex : activeIndex];
                _fireEvents = true;
            }
        }

        private VirtualSnapshotSpan? GetBoxSelectionSpanOnLine(ITextViewLine line)
        {
            Debug.Assert(this.IsBoxSelection == true, "Requesting box selection spans without a box selection.");
            if (!IsBoxSelection)
            {
                return null;
            }

            if (line == null)
            {
                throw new ArgumentNullException(nameof(line));
            }

            if (line.Snapshot != _currentSnapshot)
            {
                throw new ArgumentException("The supplied ITextViewLine is on an incorrect snapshot.", nameof(line));
            }

            if (this.SelectionExtent.IsEmpty)
            {
                VirtualSnapshotPoint caretPoint = _boxSelection.Selection.ActivePoint; // == _boxSelection.AnchorPoint
                if (line.ContainsBufferPosition(caretPoint.Position))
                {
                    return new VirtualSnapshotSpan(caretPoint, caretPoint);
                }
            }
            else
            {
                VirtualSnapshotPoint start = SelectionExtent.Start;
                VirtualSnapshotPoint end = SelectionExtent.End;
                if ((end.Position.Position >= line.Start) && (start.Position.Position <= line.End))
                {
                    //The line intersects the virtual span of the selection
                    VirtualSnapshotPoint startPoint = line.GetInsertionBufferPositionFromXCoordinate(_boxLeft);
                    VirtualSnapshotPoint endPoint = line.GetInsertionBufferPositionFromXCoordinate(_boxRight);

                    if (startPoint <= endPoint)
                        return new VirtualSnapshotSpan(startPoint, endPoint);
                    else
                        return new VirtualSnapshotSpan(endPoint, startPoint);
                }
            }
            return null;
        }

        public IReadOnlyList<VirtualSnapshotSpan> GetSelectionsOnTextViewLine(ITextViewLine line)
        {
            var lineSpan = new VirtualSnapshotSpan(new VirtualSnapshotPoint(line.Extent.Start),
                                                       new VirtualSnapshotPoint(line.Extent.End, int.MaxValue));
            return GetSelectionsIntersectingSpan(line.Extent).Where(caret => caret.Extent.IntersectsWith(lineSpan))
                                                      .Select(caret => caret.Extent.Intersection(lineSpan).Value)
                                                      .ToArray();
        }

        public bool TryRemoveSelection(Selection region)
        {
            // You can't remove the last one.
            if (!this.HasMultipleSelections)
            {
                return false;
            }

            using (var batchOp = BeginBatchOperation())
            {
                if (IsBoxSelection)
                {
                    BreakBoxSelection();
                }

                var toRemove = _selectionTransformers.FirstOrDefault(transformer => transformer.Selection == region);
                if (toRemove != null)
                {
                    _selectionTransformers.Remove(toRemove);

                    if (_primaryTransformer.Selection == region)
                    {
                        _primaryTransformer = _selectionTransformers.First();
                    }

                    _fireEvents = true;
                    toRemove.Dispose();
                }

                return (toRemove != null);
            }
        }

        public IReadOnlyList<Selection> GetSelectionsIntersectingSpans(NormalizedSnapshotSpanCollection spanCollection)
        {
            return _selectionTransformers.Where(transformer => spanCollection.IntersectsWith(transformer.Selection.Extent.SnapshotSpan))
                .Select(transformer => transformer.Selection)
                .ToArray();
        }

        public IReadOnlyList<Selection> GetSelectionsIntersectingSpan(SnapshotSpan span)
        {
            return _selectionTransformers.Where(transformer => span.IntersectsWith(transformer.Selection.Extent.SnapshotSpan))
                                  .Select(transformer => transformer.Selection)
                                  .ToArray();
        }

        public void BreakBoxSelection()
        {
            _boxSelection.Dispose();
            _boxSelection = null;
        }

        private bool TryBrokerHandledManipulation(PredefinedSelectionTransformations action)
        {
            switch (action)
            {
                case PredefinedSelectionTransformations.MoveToNextCaretPosition:
                    return TryMoveToNextCaretPosision();
                case PredefinedSelectionTransformations.MoveToPreviousCaretPosition:
                    return TryMoveToPreviousCaretPosition();
                case PredefinedSelectionTransformations.MovePageDown:
                case PredefinedSelectionTransformations.SelectPageDown:
                case PredefinedSelectionTransformations.MovePageUp:
                case PredefinedSelectionTransformations.SelectPageUp:
                    // We really can't do this operation for multiple Selections.
                    ClearSecondarySelections();

                    // Also, these can cause layouts, so batching the operations can be hurtful
                    _batchOperation.Dispose();
                    return false;
                case PredefinedSelectionTransformations.MoveToEndOfDocument:
                case PredefinedSelectionTransformations.MoveToStartOfDocument:
                case PredefinedSelectionTransformations.SelectToEndOfDocument:
                case PredefinedSelectionTransformations.SelectToStartOfDocument:
                    // These will ultimately clear all the selections, just preempt then let the action happen once.
                    ClearSecondarySelections();
                    return false;
                default:
                    return false;
            }
        }

        private bool TryMoveToPreviousCaretPosition()
        {
            if (IsBoxSelection)
            {
                var start = _boxSelection.Selection.Start;
                ClearSecondarySelections();
                _primaryTransformer.MoveTo(start, false, PositionAffinity.Successor);
                return true;
            }

            return false;
        }

        private bool TryMoveToNextCaretPosision()
        {
            if (IsBoxSelection)
            {
                var end = _boxSelection.Selection.End;
                ClearSecondarySelections();
                _primaryTransformer.MoveTo(end, false, PositionAffinity.Successor);
                return true;
            }

            return false;
        }

        public void PerformActionOnAllSelections(PredefinedSelectionTransformations action)
        {
            using (var batchOp = BeginBatchOperation())
            {
                if (!TryBrokerHandledManipulation(action))
                {
                    if (IsBoxSelection)
                    {
                        _boxSelection.PerformAction(action);
                        if (IsDestructiveToBoxSelection(action))
                        {
                            ClearSecondarySelections();
                        }
                    }
                    else
                    {
                        for (int i = 0; i < _selectionTransformers.Count; i++)
                        {
                            _selectionTransformers[i].PerformAction(action);
                        }
                    }
                }
            }
        }

        private static bool IsDestructiveToBoxSelection(PredefinedSelectionTransformations action)
        {
            switch (action)
            {
                // All caret move actions clear box selection
                case PredefinedSelectionTransformations.MovePageDown:
                case PredefinedSelectionTransformations.MovePageUp:
                case PredefinedSelectionTransformations.MoveToBeginningOfLine:
                case PredefinedSelectionTransformations.MoveToEndOfDocument:
                case PredefinedSelectionTransformations.MoveToEndOfLine:
                case PredefinedSelectionTransformations.MoveToHome:
                case PredefinedSelectionTransformations.MoveToNextCaretPosition:
                case PredefinedSelectionTransformations.MoveToNextLine:
                case PredefinedSelectionTransformations.MoveToNextWord:
                case PredefinedSelectionTransformations.MoveToPreviousCaretPosition:
                case PredefinedSelectionTransformations.MoveToPreviousLine:
                case PredefinedSelectionTransformations.MoveToPreviousWord:
                case PredefinedSelectionTransformations.MoveToStartOfDocument:
                    return true;
                default:
                    return false;
            }
        }

        public void PerformActionOnAllSelections(Action<ISelectionTransformer> action)
        {
            using (var batchOp = BeginBatchOperation())
            {
                if (IsBoxSelection)
                {
                    action(_boxSelection);
                }
                else
                {
                    for (int i = 0; i < _selectionTransformers.Count; i++)
                    {
                        action(_selectionTransformers[i]);
                    }
                }
            }
        }

        public bool TryPerformActionOnSelection(Selection before, PredefinedSelectionTransformations action, out Selection after)
        {
            // Get the desired transformer
            SelectionTransformer transformer = null;

            // It could be the box selection, check first, since it's easier than looping.
            if ((_boxSelection != null) && (before == _boxSelection.Selection))
            {
                transformer = _boxSelection;
            }

            if (transformer == null)
            {
                // Ok, we have to loop then.
                for (int i = 0; i < _selectionTransformers.Count; i++)
                {
                    if (_selectionTransformers[i].Selection == before || _selectionTransformers[i].HistoricalRegions.Contains(before))
                    {
                        transformer = _selectionTransformers[i];
                        break;
                    }
                }

                // If you intentionally are performing changes on one caret. We should not merge it.
                _mergeWinner = transformer;
            }

            using (var batchOp = BeginBatchOperation())
            {
                transformer.PerformAction(action);
                after = transformer.Selection;

                return true;
            }
        }

        public bool TryPerformActionOnSelection(Selection before, Action<ISelectionTransformer> action, out Selection after)
        {
            // Get the desired transformer
            SelectionTransformer transformer = null;

            // It could be the box selection, check first, since it's easier than looping.
            if ((_boxSelection != null) && (before == _boxSelection.Selection))
            {
                transformer = _boxSelection;
            }

            if (transformer == null)
            {
                // Ok, we have to loop then.
                transformer = _selectionTransformers.FirstOrDefault(m => m.Selection == before || m.HistoricalRegions.Contains(before));

                // If you intentionally are performing changes on one caret. We should not merge it.
                _mergeWinner = transformer;
            }

            if (transformer == null)
            {
                // Still haven't found it, we can't do anything more.
                after = default(Selection);
                return false;
            }

            using (var batchOp = BeginBatchOperation())
            {
                action(transformer);
            }

            after = transformer.Selection;
            return true;

        }

        public bool TrySetAsPrimarySelection(Selection candidate)
        {
            var transformer = _selectionTransformers.FirstOrDefault(m => m.Selection == candidate);
            if (transformer == null)
            {
                return false;
            }

            using (var batchOp = BeginBatchOperation())
            {
                if (_primaryTransformer != transformer)
                {
                    // Only fire events if this is actually changing things.
                    _fireEvents = true;
                }

                _primaryTransformer = transformer;

                // Return true either if we could set it, or it already was what was asked for.
                return true;
            }
        }

        public bool TryEnsureVisible(Selection region, EnsureSpanVisibleOptions options)
        {
            var transformer = _selectionTransformers.FirstOrDefault(m => m.Selection == region);
            if (transformer == null)
            {
                return false;
            }

            var selectionOptions = options & (EnsureSpanVisibleOptions.AlwaysCenter | EnsureSpanVisibleOptions.MinimumScroll);
            selectionOptions = selectionOptions | (region.IsReversed ? EnsureSpanVisibleOptions.ShowStart : EnsureSpanVisibleOptions.None);

            _textView.ViewScroller.EnsureSpanVisible(region.Extent, selectionOptions);

            if (region.InsertionPoint != region.ActivePoint)
            {
                _textView.ViewScroller.EnsureSpanVisible(new VirtualSnapshotSpan(region.InsertionPoint, region.InsertionPoint), EnsureSpanVisibleOptions.MinimumScroll);
            }

            return true;
        }

        public bool TryGetSelectionPresentationProperties(Selection region, out AbstractSelectionPresentationProperties properties)
        {
            // Get the desired transformer
            SelectionTransformer transformer = null;

            // It could be the box selection, check first, since it's easier than looping.
            if ((_boxSelection != null) && (region == _boxSelection.Selection))
            {
                transformer = _boxSelection;
            }

            if (transformer == null)
            {
                // Ok, we have to loop then.
                transformer = _selectionTransformers.FirstOrDefault(m => m.Selection == region || m.HistoricalRegions.Contains(region));
            }

            if (transformer == null)
            {
                // Nope, don't have it. Just fail fast.
                properties = null;
                return false;
            }
            else
            {
                properties = transformer.UIProperties;
                return true;
            }
        }

        public Selection TransformSelection(Selection source, PredefinedSelectionTransformations transformation)
        {
            // Spin up a new transformer for this operation
            var newTransformer = new SelectionTransformer(this, source);

            // Set this to track that we don't want to fire any events as a result of the transformation
            _standaloneTransformation = newTransformer;
            newTransformer.PerformAction(transformation);
            _standaloneTransformation = null;

            return newTransformer.Selection;
        }
    }
}
