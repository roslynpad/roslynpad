//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.Text.Differencing
{
    public static class DifferenceViewerOptions
    {
        /// <summary>
        /// The <see cref="DifferenceViewMode"/> the difference viewer should use (side-by-side, inline, one side at a time).
        /// </summary>
        public static readonly EditorOptionKey<DifferenceViewMode> ViewModeId = new EditorOptionKey<DifferenceViewMode>(DifferenceViewerOptions.ViewModeName);
        public const string ViewModeName = "Diff/View/ViewMode";

        /// <summary>
        /// The <see cref="DifferenceHighlightMode"/> for the line changed markers.
        /// </summary>
        public static readonly EditorOptionKey<DifferenceHighlightMode> HighlightModeId = new EditorOptionKey<DifferenceHighlightMode>(DifferenceViewerOptions.HighlightModeName);
        public const string HighlightModeName = "Diff/View/HighlightMode";

        /// <summary>
        /// If <c>true</c>, the difference viewer will scroll each contained view to the first visible difference after
        /// the files are compared.  If <c>false</c>, the scrolled area is left alone.
        /// </summary>
        public static readonly EditorOptionKey<bool> ScrollToFirstDiffId = new EditorOptionKey<bool>(ScrollToFirstDiffName);
        public const string ScrollToFirstDiffName = "Diff/View/ScrollToFirstDiff";

        /// <summary>
        /// If <c>true</c>, the left and right views of the side by side view are aligned with each other.
        /// </summary>
        /// <remarks>This option is ignored in the other view modes.</remarks>
        public static readonly EditorOptionKey<bool> SynchronizeSideBySideViewsId = new EditorOptionKey<bool>(DifferenceViewerOptions.SynchronizeSideBySideViewsName);
        public const string SynchronizeSideBySideViewsName = "Diff/View/SynchronizeSideBySideViews";

        /// <summary>
        /// If <c>true</c>, show the difference overview margin.
        /// </summary>
        public static readonly EditorOptionKey<bool> ShowDiffOverviewMarginId = new EditorOptionKey<bool>(DifferenceViewerOptions.ShowDiffOverviewMarginName);
        public const string ShowDiffOverviewMarginName = "Diff/View/ShowDiffOverviewMargin";

        /// <summary>
        /// If this is <c>false</c>, then the difference viewer will, even if a baseline has been specified, not show any differences.
        /// </summary>
        public static readonly EditorOptionKey<bool> ShowDifferencesId = new EditorOptionKey<bool>(DifferenceViewerOptions.ShowDifferencesName);
        public const string ShowDifferencesName = "ShowDifferences";
    }

    /// <summary>
    /// A base class that can be used for options that are specific to an <see cref="IDifferenceViewer"/>.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class DifferenceViewerOption<T> : EditorOptionDefinition<T>
    {
        public override bool IsApplicableToScope(IPropertyOwner scope)
        {
            return scope is IDifferenceViewer;
        }
    }
}
