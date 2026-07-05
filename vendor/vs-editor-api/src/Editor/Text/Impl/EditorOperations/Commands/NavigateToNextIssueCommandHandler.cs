namespace Microsoft.VisualStudio.Text.Operations.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Composition;
    using System.Diagnostics;
    using System.Linq;
    using Microsoft.VisualStudio.Commanding;
    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
    using Microsoft.VisualStudio.Text.Tagging;
    using Microsoft.VisualStudio.Utilities;

    [Export(typeof(ICommandHandler))]
    [Name("default " + nameof(NavigateToNextIssueCommandHandler))]
    [ContentType("any")]
    [TextViewRole(PredefinedTextViewRoles.Analyzable)]
    [Shared]
    public sealed class NavigateToNextIssueCommandHandler :
        ICommandHandler<NavigateToNextIssueInDocumentCommandArgs>,
        ICommandHandler<NavigateToPreviousIssueInDocumentCommandArgs>,
        ICommandHandler<NavigateToNextErrorInDocumentCommandArgs>,
        ICommandHandler<NavigateToPreviousErrorInDocumentCommandArgs>
    {
        [Import]
        public Lazy<IBufferTagAggregatorFactoryService> tagAggregatorFactoryService { get; set; }

        public string DisplayName => Strings.NextIssue;

        private bool NavigateToNextIssue(ErrorCommandArgsBase args, CommandExecutionContext executionContext)
        {
            var snapshot = args.TextView.TextSnapshot;
            var spans = this.GetTagSpansCollection(snapshot, args.ErrorTagTypeNames);

            if (spans.Count == 0)
            {
                return true;
            }

            (int indexOfErrorSpan, bool containsPoint) = IndexOfTagSpanNearPoint(spans, args.TextView.Caret.Position.BufferPosition.Position);

            int nextIndex = indexOfErrorSpan + 1;
            if (containsPoint)
            {
                if (spans.Count == 1)
                {
                    // There is only one error tag and it contains the caret. Ensure it stays put.
                    return true;
                }
            }
            else
            {
                nextIndex = indexOfErrorSpan;
            }

            // Wrap if needed.
            if ((indexOfErrorSpan == -1) || (nextIndex >= spans.Count))
            {
                nextIndex = 0;
            }

            args.TextView.Caret.MoveTo(new SnapshotPoint(snapshot, spans[nextIndex].Start));
            args.TextView.Caret.EnsureVisible();
            return true;
        }

        private bool NavigateToPreviousIssue(ErrorCommandArgsBase args, CommandExecutionContext executionContext)
        {
            var snapshot = args.TextView.TextSnapshot;
            var spans = this.GetTagSpansCollection(snapshot, args.ErrorTagTypeNames);

            if (spans.Count == 0)
            {
                return true;
            }

            (int indexOfErrorSpan, bool containsPoint) = IndexOfTagSpanNearPoint(spans, args.TextView.Caret.Position.BufferPosition.Position);

            int nextIndex = indexOfErrorSpan - 1;
            if (containsPoint && (spans.Count == 1))
            {
                // There is only one error tag and it contains the caret. Ensure it stays put.
                return true;
            }

            // Wrap if needed.
            if (nextIndex < 0)
            {
                nextIndex = (spans.Count - 1);
            }

            args.TextView.Caret.MoveTo(new SnapshotPoint(snapshot, spans[nextIndex].Start));
            args.TextView.Caret.EnsureVisible();
            return true;
        }

        #region Previous Issue
        public CommandState GetCommandState(NavigateToPreviousIssueInDocumentCommandArgs args) => CommandState.Available;

        public bool ExecuteCommand(NavigateToPreviousIssueInDocumentCommandArgs args, CommandExecutionContext executionContext)
        {
            return NavigateToPreviousIssue(args, executionContext);
        }
        #endregion

        #region Next Issue
        public CommandState GetCommandState(NavigateToNextIssueInDocumentCommandArgs args) => CommandState.Available;

        public bool ExecuteCommand(NavigateToNextIssueInDocumentCommandArgs args, CommandExecutionContext executionContext)
        {
            return NavigateToNextIssue(args, executionContext);
        }
        #endregion


        #region Next Error
        public CommandState GetCommandState(NavigateToNextErrorInDocumentCommandArgs args) => CommandState.Available;

        public bool ExecuteCommand(NavigateToNextErrorInDocumentCommandArgs args, CommandExecutionContext executionContext)
        {
            return NavigateToNextIssue(args, executionContext);
        }
        #endregion

        #region Prev Error
        public CommandState GetCommandState(NavigateToPreviousErrorInDocumentCommandArgs args) => CommandState.Available;

        public bool ExecuteCommand(NavigateToPreviousErrorInDocumentCommandArgs args, CommandExecutionContext executionContext)
        {
            return NavigateToPreviousIssue(args, executionContext);
        }
        #endregion

        private static (int index, bool containsPoint) IndexOfTagSpanNearPoint(NormalizedSpanCollection spans, int point)
        {
            Debug.Assert(spans.Count > 0);
            Span? tagBefore = null;
            Span? tagAfter = null;

            for (int i = 0; i < spans.Count; i++)
            {
                tagBefore = tagAfter;
                tagAfter = spans[i];

                // Case 0: point falls within error tag. We use explicit comparisons instead
                // of 'Contains' so that we match a tag even if the caret at the end of it.
                if ((point >= tagAfter.Value.Start) && (point <= tagAfter.Value.End))
                {
                    // Return tag containing the point.
                    return (i, true);
                }

                // Case 1: point falls between two tags.
                if ((tagBefore != null) && (tagBefore.Value.End < point) && (tagAfter.Value.Start > point))
                {
                    // Return tag following the point.
                    return (i, false);
                }
            }

            // Case 2: point falls after all tags.
            return (-1, false);
        }

        private NormalizedSpanCollection GetTagSpansCollection(ITextSnapshot snapshot, IEnumerable<string> errorTagTypeNames)
        {
            using (var tagger = this.tagAggregatorFactoryService.Value.CreateTagAggregator<IErrorTag>(snapshot.TextBuffer))
            {
                var rawTags = tagger.GetTags(new SnapshotSpan(snapshot, 0, snapshot.Length));
                var curatedTags = (errorTagTypeNames?.Any() ?? false) ?
                    rawTags.Where(tag => errorTagTypeNames.Contains(tag.Tag.ErrorType)) :
                    rawTags;

                // In this case we only grab the first span that the IMappingTagSpan maps to because we always
                // want to place the caret at the start of the error, and so, don't care about possibly disjoint
                // subspans after mapping to the view's buffer. NormalizedSpanCollection takes care of sorting
                // and joining overlapping spans together for us. It's possible for a tag to map to zero spans
                // in projection scenarios in which the tag exists entirely within a region that doesn't map to
                // visible space.
                return new NormalizedSpanCollection(
                    curatedTags.Select(tagSpan => tagSpan.Span.GetSpans(snapshot))
                    .Where(spanCollection => spanCollection.Count > 0)
                    .Select(spanCollection => spanCollection[0].Span));
            }
        }
    }
}
