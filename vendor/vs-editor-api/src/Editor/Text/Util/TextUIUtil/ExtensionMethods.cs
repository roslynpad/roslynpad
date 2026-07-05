using System;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;

namespace Microsoft.VisualStudio.Text.MultiSelection
{
    public static class ExtensionMethods
    {
        /// <summary>
        /// Remaps a given x-coordinate to a valid point. If the provided x-coordinate is past the right end of the line, it will
        /// be clipped to the correct position depending on the virtual space settings. If the ISmartIndent is providing indentation
        /// settings, the x-coordinate will be changed based on that.
        /// </summary>
        public static double MapXCoordinate(this ITextViewLine textLine, ITextView textView,
            double xCoordinate, ISmartIndentationService smartIndentationService, bool userSpecifiedXCoordinate)
        {
            if (textLine == null)
            {
                throw new ArgumentNullException(nameof(textLine));
            }

            if (textView == null)
            {
                throw new ArgumentNullException(nameof(textView));
            }

            // if the clicked point is to the right of the text and virtual space is disabled, the coordinate
            // needs to be fixed
            if ((xCoordinate > textLine.TextRight) && !textView.IsVirtualSpaceOrBoxSelectionEnabled())
            {
                double indentationWidth = 0.0;

                // ask the ISmartIndent to see if any indentation is necessary for empty lines
                if (textLine.End == textLine.Start)
                {
                    int? indentation = smartIndentationService?.GetDesiredIndentation(textView, textLine.Start.GetContainingLine());
                    if (indentation.HasValue)
                    {
                        //The indentation specified by the smart indent service is desired column position of the caret. Find out how much virtual space
                        //need to be at the end of the line to satisfy that.
                        double columnWidth = (textView.ViewScroller is IViewScroller2 viewScroller) ? viewScroller.ColumnWidth : 7;
                        indentationWidth = Math.Max(0.0, (((double)indentation.Value) * columnWidth - textLine.TextWidth));

                        // if the coordinate is specified by the user and the user has selected a coordinate to the left
                        // of the indentation suggested by ISmartIndent, overrule the ISmartIndent provided value and
                        // do not use any indentation.
                        if (userSpecifiedXCoordinate && (xCoordinate < (textLine.TextRight + indentationWidth)))
                            indentationWidth = 0.0;
                    }
                }

                xCoordinate = textLine.TextRight + indentationWidth;
            }

            return xCoordinate;
        }

        public static VirtualSnapshotPoint NormalizePoint(this ITextView view, VirtualSnapshotPoint point)
        {
            var line = view.GetTextViewLineContainingBufferPosition(point.Position);

            //If point is at the end of the line, return it (including any virtual space offset)
            if (point.Position >= line.End)
            {
                return new VirtualSnapshotPoint(line.End, point.VirtualSpaces);
            }
            else
            {
                //Otherwise align it with the begining of the containing text element &
                //return that (losing any virtual space).
                SnapshotSpan element = line.GetTextElementSpan(point.Position);
                return new VirtualSnapshotPoint(element.Start);
            }
        }

        public static Selection MapToSnapshot(this Selection region, ITextSnapshot snapshot, ITextView view)
        {
            var newInsertion = view.NormalizePoint(region.InsertionPoint.TranslateTo(snapshot));
            var newActive = view.NormalizePoint(region.ActivePoint.TranslateTo(snapshot));
            var newAnchor = view.NormalizePoint(region.AnchorPoint.TranslateTo(snapshot));
            PositionAffinity positionAffinity;

            if (region.Extent.Length == 0)
            {
                // Selection is just a caret, respect the caret's prefered affinity.
                positionAffinity = region.InsertionPointAffinity;
            }
            else
            {
                // Selection is non-zero length, adjust affinity so that it is always toward the body of the selection.
                // This attempts to ensure that the caret is always on the same line as the body of the selection in
                // word wrap scenarios.
                positionAffinity = newAnchor < newActive ? PositionAffinity.Predecessor : PositionAffinity.Successor;
            }

            return new Selection(newInsertion, newAnchor, newActive, positionAffinity);
        }

        /// <summary>
        /// If you are looking at this, you're likely maintaining selection code, and should be aware that
        /// virtual whitespace allowances are not simply checking a flag.
        /// 
        /// When dealing with virtual whitespace we have 3 major considerations:
        /// 1) Is the editor option enabled that allows arbitrary virtual whitespace navigation?
        /// 2) Is the current selection a box selection?
        /// 3) Are we at the beginning of a line that is impacted by Auto-Indent.
        ///
        /// This method ignores the 3rd element, since the virtual whitespace added there is not usually based
        /// on the previous whitespace, but on the auto-indent. This method is a convienence method that will return
        /// whether either of the first two conditions apply, and should be used anywhere arbitrary virtual whitespace
        /// is an option.
        /// </summary>
        public static bool IsVirtualSpaceOrBoxSelectionEnabled(this ITextView textView)
        {
            return textView.Options.IsVirtualSpaceEnabled() || textView.GetMultiSelectionBroker().IsBoxSelection;
        }

        public static bool TryGetClosestTextViewLine(this ITextView textView, double yCoordinate, out ITextViewLine closestLine)
        {
            if (textView == null)
            {
                throw new ArgumentNullException(nameof(textView));
            }

            if (textView.IsClosed || textView.InLayout)
            {
                closestLine = null;
                return false;
            }

            ITextViewLine textLine = null;

            ITextViewLineCollection textLines = textView.TextViewLines;

            if (textLines != null && textLines.Count > 0)
            {
                textLine = textLines.GetTextViewLineContainingYCoordinate(yCoordinate);

                if (textLine == null)
                {
                    if (yCoordinate <= textLines.FirstVisibleLine.Bottom)
                        textLine = textLines.FirstVisibleLine;
                    else if (yCoordinate >= textLines.LastVisibleLine.Top)
                        textLine = textLines.LastVisibleLine;
                }

                closestLine = textLine;
                return true;
            }

            closestLine = null;
            return false;
        }
    }
}
