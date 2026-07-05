//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Editor
{
    /// <summary>
    /// Specifies the names of the pre-defined margins supplied by Visual Studio.
    /// </summary>
    public static class PredefinedMarginNames
    {
        /// <summary>
        /// The margin to the left of the text view.
        /// </summary>
        public const string Left = "Left";

        /// <summary>
        /// The margin to the right of the text view.
        /// </summary>
        public const string Right = "Right";

        /// <summary>
        /// The margin above the text view.
        /// </summary>
        public const string Top = "Top";

        /// <summary>
        /// The margin below the text view.
        /// </summary>
        public const string Bottom = "Bottom";

        /// <summary>
        /// The margin to the left of the text view that implements mouse handlers for line selection.
        /// This behavior is inherited by margins contained in the left selection margin.
        /// </summary>
        public const string LeftSelection = "LeftSelection";

        /// <summary>
        /// The margin to the left of the text view that allows collapsing and expansion of outlining regions.
        /// </summary>
        public const string Outlining = "Outlining";

        /// <summary>
        /// The margin to the left of the text view that shows line numbers.
        /// </summary>
        public const string LineNumber = "LineNumber";

        /// <summary>
        /// The standard horizontal scrollbar.
        /// </summary>
        public const string HorizontalScrollBar = "HorizontalScrollBar";

        /// <summary>
        /// The container margin that contains the <see cref="HorizontalScrollBar"/> by default.
        /// </summary>
        /// <remarks>
        /// Other margins can be placed to the left or right of the <see cref="HorizontalScrollBar"/> depending on their order attribute.
        /// </remarks>
        public const string HorizontalScrollBarContainer = "HorizontalScrollBarContainer";

        /// <summary>
        /// The standard vertical scrollbar.
        /// </summary>
        public const string VerticalScrollBar = "VerticalScrollBar";

        /// <summary>
        /// The container margin that contains the <see cref="VerticalScrollBar"/> by default.
        /// </summary>
        /// <remarks>
        /// Other margins can be placed above or below the <see cref="VerticalScrollBar"/> depending on their order attribute.
        /// </remarks>
        public const string VerticalScrollBarContainer = "VerticalScrollBarContainer";

        /// <summary>
        /// A vertical margin container in the <see cref="Right"/> margin that contains the <see cref="VerticalScrollBarContainer"/>.
        /// </summary>
        /// <remarks>
        /// Margins that wish to appear on top or bottom of the vertical scrollbar and all its siblings should be added
        /// to this container margin.
        /// </remarks>
        public const string RightControl = "RightControl";

        /// <summary>
        /// A horizontal margin container in the <see cref="Bottom"/> margin that contains the <see cref="HorizontalScrollBarContainer"/>.
        /// </summary>
        /// <remarks>
        /// Margins that wish to appear to the left or right of the horizontal scrollbar and all its siblings should be added to
        /// this container margin.
        /// </remarks>
        public const string BottomControl = "BottomControl";

        /// <summary>
        /// The margin that appears between the line number and outlining margins and shows which text
        /// has changed in the current session.
        /// </summary>
        public const string Spacer = "Spacer";

        /// <summary>
        /// The margin to the left of the text view that shows breakpoint and other glyphs.
        /// </summary>
        public const string Glyph = "Glyph";

        /// <summary>
        /// The margin to the left of the text view that shows suggestion glyphs such as the Light Bulb.
        /// </summary>
        public const string Suggestion = "Suggestion";

        /// <summary>
        /// The margin to the left of the horizontal scroll bar that hosts a zoom control for zooming the view. 
        /// </summary>
        public const string ZoomControl = "ZoomControl";

        /// <summary>
        /// The margin to the right of the "Bottom" margin and below the "Right" margin.
        /// </summary>
        public const string BottomRightCorner = "BottomRightCorner";

        /// <summary>
        /// Name of the margin that shows changes in the entire file.
        /// </summary>
        public const string OverviewChangeTracking = "OverviewChangeTrackingMargin";

        /// <summary>
        /// Name of the margin that shows marks in the entire file.
        /// </summary>
        public const string OverviewMark = "OverviewMarkMargin";

        /// <summary>
        /// Name of the margin that shows errors in the entire file.
        /// </summary>
        public const string OverviewError = "OverviewErrorMargin";

        /// <summary>
        /// Name of the margin that shows a zoomed-out image of the entire file.
        /// </summary>
        public const string OverviewSourceImage = "OverviewSourceImageMargin";

        /// <summary>
        /// Name of the margin that shows potentially-actionable non-modal messages to the user.
        /// </summary>
        public const string InfoBar = "InfoBar";
    }
}
