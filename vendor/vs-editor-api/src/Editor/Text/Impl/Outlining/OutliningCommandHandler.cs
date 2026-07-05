using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;

using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.Text.Outlining.Implementation
{
    /// <summary>
    /// Commands for collapsing and expanding text
    /// </summary>
    [Name(nameof(OutliningCommandHandler))]
    [ContentType("text")]
    [Export(typeof(ICommandHandler))]
    [Shared]
    public sealed class OutliningCommandHandler :
        ICommandHandler<ToggleOutliningExpansionCommandArgs>,
        ICommandHandler<ToggleAllOutliningCommandArgs>,
        ICommandHandler<ToggleOutliningDefinitionsCommandArgs>,
        ICommandHandler<ToggleOutliningEnabledCommandArgs>
    {
        [Import]
        public IOutliningManagerService outliningManagerService { get; set; }

        string INamed.DisplayName => nameof(OutliningCommandHandler);

        bool ICommandHandler<ToggleOutliningExpansionCommandArgs>.ExecuteCommand(ToggleOutliningExpansionCommandArgs args, CommandExecutionContext executionContext)
        {
            var outliningManager = outliningManagerService.GetOutliningManager(args.TextView);
            if (outliningManager == null || !outliningManager.Enabled)
            {
                return false;
            }

            var collapsibles = GetCollapsiblesForCurrentSelection(outliningManager, args.TextView);
            if ((collapsibles != null) && (collapsibles.Any()))
            {
                var firstCollapsible = collapsibles.First();
                var lastCollapsible = collapsibles.Last();
                if ((firstCollapsible != null) && (lastCollapsible != null))
                {
                    var snapshot = args.TextView.TextSnapshot;
                    var sortedCollapsedMatcher = new SortedCollapsedMatcher(collapsibles.Select((collapsible) => collapsible.Extent), snapshot);
                    var firstSnapshotSpan = firstCollapsible.Extent.GetSpan(snapshot);
                    var lastSnapshotSpan = ((lastCollapsible == firstCollapsible) ? firstSnapshotSpan : lastCollapsible.Extent.GetSpan(snapshot));
                    var containingSnapshotSpan = new SnapshotSpan(firstSnapshotSpan.Start, lastSnapshotSpan.End);

                    if (ShouldExpandToggledCollapsibles(collapsibles))
                    {
                        outliningManager.ExpandAll(containingSnapshotSpan, sortedCollapsedMatcher.Match);
                    }
                    else
                    {
                        outliningManager.CollapseAll(containingSnapshotSpan, sortedCollapsedMatcher.Match);
                    }

                    // Ensure caret visible
                    args.TextView.Caret.EnsureVisible();
                }
            }

            return true;
        }

        bool ICommandHandler<ToggleAllOutliningCommandArgs>.ExecuteCommand(ToggleAllOutliningCommandArgs args, CommandExecutionContext executionContext)
        {
            var outliningManager = outliningManagerService.GetOutliningManager(args.TextView);
            if (outliningManager == null || !outliningManager.Enabled)
            {
                return false;
            }

            var snapshot = args.TextView.TextBuffer.CurrentSnapshot;
            var snapshotSpan = new SnapshotSpan(snapshot, 0, snapshot.Length);

            if (outliningManager.GetCollapsedRegions(snapshotSpan).Any())
            {
                outliningManager.ExpandAll(snapshotSpan, collapsed => true);
            }
            else
            {
                outliningManager.CollapseAll(snapshotSpan, collapsed => true);
            }

            args.TextView.Caret.EnsureVisible();

            return true;
        }

        bool ICommandHandler<ToggleOutliningDefinitionsCommandArgs>.ExecuteCommand(ToggleOutliningDefinitionsCommandArgs args, CommandExecutionContext executionContext)
        {
            // NOTE: For VSwin, this isn't something you toggle. The single related command is "collapse to definitions".
            //       It expands everthing then collapses all implementation blocks.
            //       VSmac's old editor lets you toggle, which is a little less clear-cut when other folding has occurred.

            var outliningManager = outliningManagerService.GetOutliningManager(args.TextView) as IAccurateOutliningManager;
            if (outliningManager == null)
            {
                return false;
            }

            // Expand non-implementation
            var snapshot = args.TextView.TextBuffer.CurrentSnapshot;
            var snapshotSpan = new SnapshotSpan(snapshot, 0, snapshot.Length);

            // If any definitions are already collapsed, we will be expanding
            var expandDefinitions = outliningManager.GetCollapsedRegions(snapshotSpan).Any(r => r.Tag.IsImplementation);

            outliningManager.ExpandAll(snapshotSpan, (collapsible => !collapsible.Tag.IsImplementation));

            if (expandDefinitions)
            {
                outliningManager.ExpandAll(snapshotSpan, (collapsible => collapsible.Tag.IsImplementation));
            }
            else
            {
                outliningManager.CollapseAll(snapshotSpan, (collapsible => collapsible.Tag.IsImplementation));
            }

            args.TextView.Caret.EnsureVisible();

            return true;
        }

        bool ICommandHandler<ToggleOutliningEnabledCommandArgs>.ExecuteCommand(ToggleOutliningEnabledCommandArgs args, CommandExecutionContext executionContext)
        {
            var outliningManager = outliningManagerService.GetOutliningManager(args.TextView);
            if (outliningManager == null)
            {
                return false;
            }

            outliningManager.Enabled = !outliningManager.Enabled;

            // TODO: What about adhoc regions? (Windows has special logic here, but Mac does not have AdhocOutliner)

            return true;
        }

        CommandState ICommandHandler<ToggleOutliningExpansionCommandArgs>.GetCommandState(ToggleOutliningExpansionCommandArgs args)
        {
            var outliningManager = outliningManagerService.GetOutliningManager(args.TextView);
            if (outliningManager != null && outliningManager.Enabled)
            {
                var collapsibles = GetCollapsiblesForCurrentSelection(outliningManager, args.TextView);
                if (collapsibles != null && collapsibles.Any())
                {
                    return CommandState.Available;
                }
            }

            return CommandState.Unavailable;
        }

        CommandState ICommandHandler<ToggleAllOutliningCommandArgs>.GetCommandState(ToggleAllOutliningCommandArgs args)
        {
            return outliningManagerService.GetOutliningManager(args.TextView) is IOutliningManager mgr && mgr.Enabled
                ? CommandState.Available
                : CommandState.Unavailable;
        }

        CommandState ICommandHandler<ToggleOutliningDefinitionsCommandArgs>.GetCommandState(ToggleOutliningDefinitionsCommandArgs args)
        {
            return outliningManagerService.GetOutliningManager(args.TextView) is IAccurateOutliningManager mgr && mgr.Enabled
                ? CommandState.Available
                : CommandState.Unavailable;
        }

        CommandState ICommandHandler<ToggleOutliningEnabledCommandArgs>.GetCommandState(ToggleOutliningEnabledCommandArgs args)
        {
            return CommandState.Available;
        }

        internal IEnumerable<ICollapsible> GetCollapsiblesForCurrentSelection(IOutliningManager outliningManager, ITextView textView)
        {
            if (outliningManager == null)
            {
                return null;
            }

            // Try for all collapsibles which are contained within the selection span
            SnapshotSpan selectionSnapshotSpan = textView.Selection.StreamSelectionSpan.SnapshotSpan;
            ITextSnapshot selectionSnapshot = selectionSnapshotSpan.Snapshot;
            var intersectingCollapsibles = outliningManager.GetAllRegions(selectionSnapshotSpan);
            var filteredCollapsibles = intersectingCollapsibles.Where(collapsible => selectionSnapshotSpan.Contains(collapsible.Extent.GetSpan(selectionSnapshot)));
            var tornoffCollapsibles = GetTornoffCollapsibles(filteredCollapsibles);
            if (tornoffCollapsibles != null)
            {
                return tornoffCollapsibles;
            }

            // Try for innermost collapsibles which intersect and start within the selection span
            filteredCollapsibles = intersectingCollapsibles.Where(collapsible => selectionSnapshotSpan.Contains(collapsible.Extent.GetSpan(selectionSnapshot).Start));
            tornoffCollapsibles = GetInnermostCollapsibles(selectionSnapshot, filteredCollapsibles);
            if (tornoffCollapsibles != null)
            {
                return tornoffCollapsibles;
            }

            // Try for all collapsibles which are contained within the selection's first line
            ITextSnapshotLine selectionStartLine = selectionSnapshot.GetLineFromPosition(selectionSnapshotSpan.Start);
            SnapshotSpan selectionFirstLine = new SnapshotSpan(selectionSnapshot, selectionStartLine.Start, selectionStartLine.Length);
            intersectingCollapsibles = outliningManager.GetAllRegions(selectionFirstLine);
            filteredCollapsibles = intersectingCollapsibles.Where(collapsible => selectionFirstLine.Contains(collapsible.Extent.GetSpan(selectionSnapshot)));
            tornoffCollapsibles = GetTornoffCollapsibles(filteredCollapsibles);
            if (tornoffCollapsibles != null)
            {
                return tornoffCollapsibles;
            }

            // Try for innermost collapsibles which intersect and start within the selection's first line
            filteredCollapsibles = intersectingCollapsibles.Where(collapsible => selectionFirstLine.Contains(collapsible.Extent.GetSpan(selectionSnapshot).Start));
            tornoffCollapsibles = GetInnermostCollapsibles(selectionSnapshot, filteredCollapsibles);
            if (tornoffCollapsibles != null)
            {
                return tornoffCollapsibles;
            }

            // Try for innermost collapsibles which simply intersect the selection's first line
            tornoffCollapsibles = GetInnermostCollapsibles(selectionSnapshot, intersectingCollapsibles);
            if (tornoffCollapsibles != null)
            {
                return tornoffCollapsibles;
            }

            return null;
        }

        internal static IEnumerable<ICollapsible> GetTornoffCollapsibles(IEnumerable<ICollapsible> collapsibles)
        {
            List<ICollapsible> tornoffCollapsibles = null;

            if (collapsibles != null)
            {
                foreach (ICollapsible collapsible in collapsibles)
                {
                    AddCollapsible(ref tornoffCollapsibles, collapsible);
                }
            }

            return tornoffCollapsibles;
        }

        internal static IEnumerable<ICollapsible> GetInnermostCollapsibles(ITextSnapshot snapshot, IEnumerable<ICollapsible> collapsibles)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            List<ICollapsible> innermostCollapsibles = null;

            if (collapsibles != null)
            {
                // OutliningManager returns collapsibles "sorted" such that contained follow containing and preceed not contained
                // Include collapsibles which have no following collapsibles which are contained within them
                ICollapsible previousCollapsible = null;
                foreach (ICollapsible collapsible in collapsibles)
                {
                    if (previousCollapsible != null)
                    {
                        if (!previousCollapsible.Extent.GetSpan(snapshot).Contains(collapsible.Extent.GetSpan(snapshot)))
                        {
                            AddCollapsible(ref innermostCollapsibles, previousCollapsible);
                        }
                    }
                    previousCollapsible = collapsible;
                }
                if (previousCollapsible != null)
                {
                    AddCollapsible(ref innermostCollapsibles, previousCollapsible);
                }
            }

            return innermostCollapsibles;
        }

        internal static void AddCollapsible(ref List<ICollapsible> collapsibles, ICollapsible collapsible)
        {
            if (collapsibles == null)
            {
                collapsibles = new List<ICollapsible>();
            }
            collapsibles.Add(collapsible);
        }

        internal static bool ShouldExpandToggledCollapsibles(IEnumerable<ICollapsible> collapsibles)
        {
            if (collapsibles != null)
            {
                // Determine whether homogenous collapse state for given collapsibles
                bool homogenousCollapseState = true;
                ICollapsible previousCollapsible = null;
                foreach (ICollapsible collapsible in collapsibles)
                {
                    if (previousCollapsible != null)
                    {
                        if (previousCollapsible.IsCollapsed != collapsible.IsCollapsed)
                        {
                            homogenousCollapseState = false;
                            break;
                        }
                    }
                    previousCollapsible = collapsible;
                }

                // Check whether non empty collection of collapsibles
                if (previousCollapsible != null)
                {
                    // Check whether homogenous collapse state and collapsed
                    if (homogenousCollapseState && previousCollapsible.IsCollapsed)
                    {
                        // Should expand collapsibles
                        return true;
                    }
                    else
                    {
                        // Should collapse collapsibles
                        return false;
                    }
                }
            }

            return false;
        }

        internal static bool AreExpandable(IEnumerable<ICollapsible> collapsibles)
        {
            if (collapsibles != null)
            {
                foreach (ICollapsible collapsible in collapsibles)
                {
                    if (collapsible.IsCollapsed)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        internal bool AreCollapsible(IEnumerable<ICollapsible> collapsibles, ITextSnapshot snapshot)
        {
            if (collapsibles != null)
            {
                // OutliningManager returns collapsibles "sorted" such that contained follow containing and preceed not contained
                // Return true if find uncollapsed not nested within collapsed
                ICollapsible previousCollapsed = null;
                foreach (ICollapsible collapsible in collapsibles)
                {
                    bool nestedInPreviousCollapsed = ((previousCollapsed != null) && previousCollapsed.Extent.GetSpan(snapshot).Contains(collapsible.Extent.GetSpan(snapshot)));
                    if (!nestedInPreviousCollapsed)
                    {
                        if (!collapsible.IsCollapsed)
                        {
                            return true;
                        }
                        previousCollapsed = collapsible;
                    }
                }
            }

            return false;
        }

        internal class SortedCollapsibleMatcher
        {
            private IEnumerator<ITrackingSpan> _trackingSpanEnum;
            private ITextSnapshot _textSnapshot;
            private ITrackingSpan _currentTrackingSpan;
            internal bool MatchReturn { get; set; }

            internal SortedCollapsibleMatcher(IEnumerable<ITrackingSpan> trackingSpans, ITextSnapshot textSnapshot)
            {
                _trackingSpanEnum = ((trackingSpans != null) ? trackingSpans.GetEnumerator() : null);
                _textSnapshot = textSnapshot;
                if ((_trackingSpanEnum != null) && (_textSnapshot != null))
                {
                    _currentTrackingSpan = (_trackingSpanEnum.MoveNext() ? _trackingSpanEnum.Current : null);
                }
                MatchReturn = true;
            }
            internal bool Match(ICollapsible givenCollapsible)
            {
                if (givenCollapsible != null)
                {
                    while (_currentTrackingSpan != null)
                    {
                        SnapshotSpan currentSnapshotSpan = _currentTrackingSpan.GetSpan(_textSnapshot);
                        SnapshotSpan givenSnapshotSpan = givenCollapsible.Extent.GetSpan(_textSnapshot);
                        int comparison = ((currentSnapshotSpan.Start != givenSnapshotSpan.Start) ?
                                           currentSnapshotSpan.Start.CompareTo(givenSnapshotSpan.Start) :
                                           -currentSnapshotSpan.Length.CompareTo(givenSnapshotSpan.Length));
                        if (comparison <= 0)
                        {
                            // Advance current
                            _currentTrackingSpan = (_trackingSpanEnum.MoveNext() ? _trackingSpanEnum.Current : null);

                            if (comparison == 0)
                            {
                                // Match
                                return MatchReturn;
                            }

                            // Continue loop to try updated current
                        }
                        else
                        {
                            // Break loop to advance given
                            break;
                        }
                    }
                }
                return !MatchReturn;
            }
        }

        internal class SortedCollapsedMatcher : SortedCollapsibleMatcher
        {
            internal SortedCollapsedMatcher(IEnumerable<ITrackingSpan> trackingSpans, ITextSnapshot textSnapshot) :
                base(trackingSpans, textSnapshot)
            {
            }
            internal bool Match(ICollapsed givenCollapsed)
            {
                return base.Match(givenCollapsed);
            }
        }
    }
}
