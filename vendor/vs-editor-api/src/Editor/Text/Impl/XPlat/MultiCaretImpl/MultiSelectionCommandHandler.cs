using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Text.Outlining;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.Text.MultiSelection.Implementation
{
    [Export(typeof(ICommandHandler))]
    [Name(nameof(MultiSelectionCommandHandler))]
    [ContentType("any")]
    [TextViewRole(PredefinedTextViewRoles.Interactive)]
    [Shared]
    public sealed class MultiSelectionCommandHandler : ICommandHandler<EscapeKeyCommandArgs>,
                                                     ICommandHandler<InsertNextMatchingCaretCommandArgs>,
                                                     ICommandHandler<InsertAllMatchingCaretsCommandArgs>,
                                                     ICommandHandler<RotatePrimaryCaretNextCommandArgs>,
                                                     ICommandHandler<RotatePrimaryCaretPreviousCommandArgs>,
                                                     ICommandHandler<RemoveLastSecondaryCaretCommandArgs>,
                                                     ICommandHandler<MoveLastCaretDownCommandArgs>
    {
        [Import]
        public IMultiSelectionBrokerFactory MultiSelectionBrokerFactoryService { get; set; }

        [Import]
        public ITextSearchNavigatorFactoryService TextSearchNavigatorFactoryService { get; set; }

        [Import]
        public IOutliningManagerService OutliningManagerService { get; set; }

        public string DisplayName => Strings.MultiSelectionCancelCommandName;

        public CommandState GetCommandState(EscapeKeyCommandArgs args) => CommandState.Available;
        public CommandState GetCommandState(RotatePrimaryCaretNextCommandArgs args)
        {
            var broker = args.TextView.GetMultiSelectionBroker();
            return broker.HasMultipleSelections ? CommandState.Available : CommandState.Unavailable;
        }

        public CommandState GetCommandState(RotatePrimaryCaretPreviousCommandArgs args)
        {
            var broker = args.TextView.GetMultiSelectionBroker();
            return broker.HasMultipleSelections ? CommandState.Available : CommandState.Unavailable;
        }

        public CommandState GetCommandState(InsertNextMatchingCaretCommandArgs args)
        {
            return CommandState.Available;
        }

        public CommandState GetCommandState(InsertAllMatchingCaretsCommandArgs args)
        {
            return CommandState.Available;
        }

        public CommandState GetCommandState(RemoveLastSecondaryCaretCommandArgs args)
        {
            var broker = args.TextView.GetMultiSelectionBroker();
            return broker.HasMultipleSelections || broker.PrimarySelection.Extent.Length > 0 ? CommandState.Available : CommandState.Unavailable;
        }

        public CommandState GetCommandState(MoveLastCaretDownCommandArgs args)
        {
            var broker = args.TextView.GetMultiSelectionBroker();
            return broker.HasMultipleSelections && !broker.PrimarySelection.IsEmpty ? CommandState.Available : CommandState.Unavailable;
        }

        public bool ExecuteCommand(EscapeKeyCommandArgs args, CommandExecutionContext executionContext)
        {
            var broker = args.TextView.GetMultiSelectionBroker();
            if (broker.IsBoxSelection)
            {
                using (var batchOp = broker.BeginBatchOperation())
                {
                    broker.ClearSecondarySelections();
                    broker.PerformActionOnAllSelections(PredefinedSelectionTransformations.ClearSelection);
                    return true;
                }
            }
            else if (broker.HasMultipleSelections)
            {
                broker.ClearSecondarySelections();
                return true;
            }
            else if (!broker.PrimarySelection.IsEmpty)
            {
                broker.PerformActionOnAllSelections(PredefinedSelectionTransformations.ClearSelection);
                return true;
            }
            return false;
        }

        public bool ExecuteCommand(RotatePrimaryCaretNextCommandArgs args, CommandExecutionContext executionContext)
        {
            var broker = args.TextView.GetMultiSelectionBroker();
            var regionArray = broker.AllSelections.ToArray();

            int oldIndex = 0;
            for (; oldIndex < regionArray.Length; oldIndex++)
            {
                if (regionArray[oldIndex] == broker.PrimarySelection)
                {
                    break;
                }
            }

            int newIndex = Math.Min((oldIndex + 1), regionArray.Length - 1);

            Selection newPrimary = regionArray[newIndex];
            var result = broker.TrySetAsPrimarySelection(newPrimary);

            if (result == true)
            {
                broker.TryEnsureVisible(newPrimary, EnsureSpanVisibleOptions.None);
            }

            return result;
        }

        public bool ExecuteCommand(RotatePrimaryCaretPreviousCommandArgs args, CommandExecutionContext executionContext)
        {
            var broker = args.TextView.GetMultiSelectionBroker();
            var regionArray = broker.AllSelections.ToArray();

            int oldIndex = 0;
            for (; oldIndex < regionArray.Length; oldIndex++)
            {
                if (regionArray[oldIndex] == broker.PrimarySelection)
                {
                    break;
                }
            }

            int newIndex = Math.Max((oldIndex - 1), 0);

            Selection newPrimary = regionArray[newIndex];
            var result = broker.TrySetAsPrimarySelection(newPrimary);

            if (result == true)
            {
                broker.TryEnsureVisible(newPrimary, EnsureSpanVisibleOptions.None);
            }

            return result;
        }

        private static Selection InsertDiscoveredMatchRegion(IMultiSelectionBroker broker, Selection primaryRegion, SnapshotSpan found)
        { 
            var newSpan = new VirtualSnapshotSpan(found);
            var newSelection = new Selection(newSpan, primaryRegion.IsReversed);
            broker.AddSelection(newSelection);
            return newSelection;
        }

        public bool ExecuteCommand(InsertNextMatchingCaretCommandArgs args, CommandExecutionContext executionContext)
        {
            var broker = args.TextView.GetMultiSelectionBroker();

            if (broker.PrimarySelection.IsEmpty)
            {
                broker.TryPerformActionOnSelection(broker.PrimarySelection, PredefinedSelectionTransformations.SelectCurrentWord, out _);
                return true;
            }

            var navigator = TextSearchNavigatorFactoryService.CreateSearchNavigator(args.TextView.TextViewModel.EditBuffer);

            var primaryRegion = broker.PrimarySelection;
            string searchString = primaryRegion.Extent.GetText();

            // Intentionally look at all whitespace here
            if (!string.IsNullOrEmpty(searchString))
            {
                navigator.SearchTerm = searchString;
                navigator.SearchOptions = FindOptions.MatchCase | FindOptions.Multiline;

                var regionArray = broker.AllSelections.ToArray();
                int primaryIndex = 0;
                for (; primaryIndex < regionArray.Length; primaryIndex++)
                {
                    if (regionArray[primaryIndex] == broker.PrimarySelection)
                    {
                        break;
                    }
                }

                if (primaryIndex != 0)
                {
                    var wrappedLastRegion = regionArray[primaryIndex - 1];

                    navigator.StartPoint = wrappedLastRegion.End.Position;
                    var spanStart = wrappedLastRegion.End.Position;
                    var spanLength = primaryRegion.Start.Position - spanStart;
                    navigator.SearchSpan = args.TextView.TextViewModel.EditBuffer.CurrentSnapshot.CreateTrackingSpan(
                                                new Span(spanStart, spanLength),
                                                SpanTrackingMode.EdgeInclusive);
                }
                else
                {
                    navigator.StartPoint = broker.SelectionExtent.End.Position;
                    navigator.SearchSpan = args.TextView.TextViewModel.EditBuffer.CurrentSnapshot.CreateTrackingSpan(
                                                new Span(0, args.TextView.TextViewModel.EditBuffer.CurrentSnapshot.Length),
                                                SpanTrackingMode.EdgeInclusive);

                    navigator.SearchOptions = navigator.SearchOptions | FindOptions.Wrap;
                }

                if (navigator.Find())
                {
                    var found = navigator.CurrentResult.Value;

                    if (!broker.SelectedSpans.OverlapsWith(found))
                    {
                        var outliningManager = OutliningManagerService.GetOutliningManager(args.TextView);
                        if (outliningManager != null)
                        {
                            outliningManager.ExpandAll(found, collapsible => true);
                        }

                        var addedRegion = InsertDiscoveredMatchRegion(broker, primaryRegion, found);
                        broker.TryEnsureVisible(addedRegion, EnsureSpanVisibleOptions.None);

                        return true;
                    }
                }
            }

            return false;
        }

        public bool ExecuteCommand(InsertAllMatchingCaretsCommandArgs args, CommandExecutionContext executionContext)
        {
            var broker = args.TextView.GetMultiSelectionBroker();

            if (broker.PrimarySelection.IsEmpty)
            {
                broker.TryPerformActionOnSelection(broker.PrimarySelection, PredefinedSelectionTransformations.SelectCurrentWord, out _);
            }

            var navigator = TextSearchNavigatorFactoryService.CreateSearchNavigator(args.TextView.TextViewModel.EditBuffer);

            var primaryRegion = broker.PrimarySelection;
            string searchString = primaryRegion.Extent.GetText();

            // Intentionally look at all whitespace here
            if (!string.IsNullOrEmpty(searchString))
            {
                var snapshot = args.TextView.TextViewModel.EditBuffer.CurrentSnapshot;
                var documentSpan = snapshot.CreateTrackingSpan(0, snapshot.Length, SpanTrackingMode.EdgeInclusive);

                navigator.SearchSpan = documentSpan;
                navigator.SearchTerm = searchString;
                navigator.StartPoint = broker.PrimarySelection.Start.Position;
                navigator.SearchOptions = FindOptions.MatchCase | FindOptions.Multiline | FindOptions.Wrap;

                navigator.Find(); // Get and ignore the primary region

                var newlySelectedSpans = new List<SnapshotSpan>();
                var oldSelectedSpans = broker.SelectedSpans;

                while (navigator.Find())
                {
                    var found = navigator.CurrentResult.Value;

                    // If we have found the primary region again, we've gone the whole way around the document.
                    if (found.OverlapsWith(primaryRegion.Extent.SnapshotSpan))
                    {
                        break;
                    }

                    if (!oldSelectedSpans.OverlapsWith(found))
                    {
                        newlySelectedSpans.Add(found);
                    }
                }

                // Make sure that none of the newly selected spans overlap
                for(int i = 0; i < (newlySelectedSpans.Count - 1); i++)
                {
                    if (newlySelectedSpans[i].OverlapsWith(newlySelectedSpans[i+1]))
                    {
                        newlySelectedSpans.RemoveAt(i + 1);

                        // decrement 1 so we can compare i and what used to be i+2 next time
                        i--;
                    }
                }

                var newlySelectedSpanCollection = new NormalizedSnapshotSpanCollection(newlySelectedSpans);

                // Ok, we've figured out what selections we want to add. Now we need to expand any outlining regions before finally adding the selections
                var outliningManager = OutliningManagerService.GetOutliningManager(args.TextView);
                if (outliningManager != null)
                {
                    var extent = new SnapshotSpan(
                        newlySelectedSpanCollection[0].Start,
                        newlySelectedSpanCollection[newlySelectedSpanCollection.Count - 1].End);
                    outliningManager.ExpandAll(extent, collapsible =>
                    {
                        return newlySelectedSpanCollection.IntersectsWith(collapsible.Extent.GetSpan(broker.CurrentSnapshot));
                    });
                }

                // Yay, we can finally actually add the selections
                for (int i = 0; i < newlySelectedSpans.Count; i++)
                {
                    broker.AddSelectionRange(newlySelectedSpans.Select(span => new Selection(span, broker.PrimarySelection.IsReversed)));
                }

                return true;
            }

            return false;
        }

        private static Selection FindLastSelection(IMultiSelectionBroker broker)
        {
            int primaryIndex = 0;
            var selections = broker.AllSelections;
            for (; selections[primaryIndex] != broker.PrimarySelection; primaryIndex++) ;
            // Intentional empty loop.

            if (primaryIndex != 0)
            {
                return selections[primaryIndex - 1];
            }
            else
            {
                return selections[selections.Count - 1];
            }
        }

        public bool ExecuteCommand(RemoveLastSecondaryCaretCommandArgs args, CommandExecutionContext executionContext)
        {
            var broker = args.TextView.GetMultiSelectionBroker();

            if (broker.HasMultipleSelections)
            {
                Selection toRemove = FindLastSelection(broker);

                if (broker.TryRemoveSelection(toRemove))
                {
                    return broker.TryEnsureVisible(FindLastSelection(broker), EnsureSpanVisibleOptions.None);
                }
            }
            else if (broker.PrimarySelection.Extent.Length > 0)
            {
                broker.TryPerformActionOnSelection(broker.PrimarySelection, PredefinedSelectionTransformations.ClearSelection, out _);
            }

            return false;
        }

        public bool ExecuteCommand(MoveLastCaretDownCommandArgs args, CommandExecutionContext executionContext)
        {
            var broker = args.TextView.GetMultiSelectionBroker();

            if (broker.HasMultipleSelections)
            {
                Selection toRemove = FindLastSelection(broker);
                var addNextArgs = new InsertNextMatchingCaretCommandArgs(args.TextView, args.SubjectBuffer);
                if (ExecuteCommand(addNextArgs, executionContext))
                {
                    if (broker.TryRemoveSelection(toRemove))
                    {
                        return broker.TryEnsureVisible(FindLastSelection(broker), EnsureSpanVisibleOptions.None);
                    }
                }
            }

            return false;
        }
    }
}
