//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
using Microsoft.VisualStudio.Text.Outlining;
using Microsoft.VisualStudio.Text.Projection;

namespace Microsoft.VisualStudio.Text.Editor
{
    internal static class TextViewExtensions2
    {
        public static bool TryMoveCaretToAndEnsureVisible(
            this ITextView textView,
            SnapshotPoint point,
            IOutliningManagerService outliningManagerService = null,
            EnsureSpanVisibleOptions ensureSpanVisibleOptions = EnsureSpanVisibleOptions.None)
        {
            return textView.TryMoveCaretToAndEnsureVisible(
                new VirtualSnapshotPoint(point),
                outliningManagerService,
                ensureSpanVisibleOptions);
        }

        public static bool TryMoveCaretToAndEnsureVisible(
            this ITextView textView,
            VirtualSnapshotPoint point,
            IOutliningManagerService outliningManagerService = null,
            EnsureSpanVisibleOptions ensureSpanVisibleOptions = EnsureSpanVisibleOptions.None)
        {
            if (textView.IsClosed)
            {
                return false;
            }

            var pointInView = textView.GetPositionInView(point.Position);
            if (!pointInView.HasValue)
            {
                return false;
            }

            // If we were given an outlining service, we need to expand any outlines first, or else
            // the Caret.MoveTo won't land in the correct location if our target is inside a
            // collapsed outline.
            if (outliningManagerService != null)
            {
                var outliningManager = outliningManagerService.GetOutliningManager(textView);
                if (outliningManager != null)
                {
                    outliningManager.ExpandAll(new SnapshotSpan(pointInView.Value, length: 0), match: _ => true);
                }
            }

            var newPosition = textView.Caret.MoveTo(new VirtualSnapshotPoint(pointInView.Value, point.VirtualSpaces));

            // We use the caret's position in the view's current snapshot here in case something
            // changed text in response to a caret move (e.g. line commit)
            var spanInView = new SnapshotSpan(newPosition.BufferPosition, 0);
            textView.ViewScroller.EnsureSpanVisible(spanInView, ensureSpanVisibleOptions);

            return true;
        }

        public static SnapshotPoint GetSnapshotPoint(this ITextView textView, int lineNumber, int columnNumber = 0)
        {
            return textView.TextSnapshot.TryGetSnapshotPoint(lineNumber, columnNumber) ?? default;
        }

        public static void SelectSpan(
            this ITextView textView,
            SnapshotSpan span,
            IOutliningManagerService outliningManagerService = null,
            EnsureSpanVisibleOptions ensureSpanVisibleOptions = EnsureSpanVisibleOptions.None)
        {
            textView.TryMoveCaretToAndEnsureVisible(span.Start, outliningManagerService, ensureSpanVisibleOptions);
            textView.Selection.Select(span, isReversed: false);
        }

        public static SnapshotPoint GetPosition(this ITextView textView, int position)
        {
            var snapshot = textView.TextSnapshot;
            if (position < 0 || position > snapshot.Length)
            {
                return default;
            }

            return new SnapshotPoint(textView.TextSnapshot, position);
        }

        public static void NavigateToLineAndColumn(this ITextView textView, int lineNumber, int columnNumber = 0)
        {
            textView.Selection.Clear();
            var point = textView.TextSnapshot.TryGetSnapshotPoint(lineNumber, columnNumber) ?? default;
            if (point != default)
            {
                textView.TryMoveCaretToAndEnsureVisible(point);
            }
        }

        public static SnapshotPoint? GetPositionInView(this ITextView textView, SnapshotPoint point)
        {
            return textView.BufferGraph.MapUpToSnapshot(
                point,
                PointTrackingMode.Positive,
                PositionAffinity.Successor,
                textView.TextSnapshot);
        }

        public static SnapshotPoint? GetPositionInSubjectBuffer(this ITextView textView, SnapshotPoint point)
        {
            var dataBufferPosition = textView.BufferGraph.MapDownToFirstMatch(
                point,
                PointTrackingMode.Positive,
                t => !(t.TextBuffer is IProjectionBufferBase),
                PositionAffinity.Successor);
            return dataBufferPosition;
        }

        public static ITextBuffer GetSubjectBufferFromPosition(this ITextView textView, SnapshotPoint point)
        {
            var position = GetPositionInSubjectBuffer(textView, point);
            if (position.HasValue && position.Value.Snapshot.TextBuffer is ITextBuffer buffer)
            {
                return buffer;
            }

            return null;
        }

        public static ITextBuffer GetSubjectBufferFromCaret(this ITextView view)
            => view.GetSubjectBufferFromPosition(view.Caret.Position.BufferPosition);
    }
}