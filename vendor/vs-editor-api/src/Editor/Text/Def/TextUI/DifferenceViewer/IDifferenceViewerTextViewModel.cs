//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.Text.Differencing
{
    /// <summary>
    /// A <see cref="ITextViewModel"/> used by <see cref="IDifferenceViewer"/>.
    /// </summary>
    internal interface IDifferenceViewerTextViewModel
    {
        /// <summary>
        /// The view type for a view created by an <see cref="IDifferenceViewer"/>.
        /// </summary>
        DifferenceViewType ViewType { get; }

        /// <summary>
        /// The currently-used snapshot difference that matches up with the current snapshot
        /// of the inline buffer.
        /// </summary>
        /// <remarks>Will be <c>null</c> before the first snapshot difference is computed.</remarks>
        ISnapshotDifference CurrentSnapshotDifference { get; }

        /// <summary>
        /// Are the left and right views are synchronized in the side by side view.
        /// </summary>
        /// <remarks>
        /// <para>In the side by side view, the left and right views are, normally, synchronized so that so that matching text always shown in each view.
        /// If this synchronization is turned off, then each view will scroll independently.</para>
        /// </remarks>
        bool AreViewsSynchronized { get; }

        /// <summary>
        /// The view for displaying the left buffer for <see cref="DifferenceViewMode.SideBySide"/> differences.
        /// </summary>
        /// <remarks>Will never be <c>null</c>, but will only be visible when view mode
        /// is set to <see cref="DifferenceViewMode.SideBySide"/>.</remarks>
        ITextView LeftView { get; }

        /// <summary>
        /// The view for displaying the right buffer for <see cref="DifferenceViewMode.SideBySide"/> differences.
        /// </summary>
        /// <remarks>Will never be <c>null</c>, but will only be visible when view mode
        /// is set to <see cref="DifferenceViewMode.SideBySide"/>.</remarks>
        ITextView RightView { get; }
    }
}
