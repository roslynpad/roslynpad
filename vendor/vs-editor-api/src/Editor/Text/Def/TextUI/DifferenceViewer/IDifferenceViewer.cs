//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
using System;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.Text.Differencing
{
    /// <summary>
    /// A difference viewer is a container for viewing an <see cref="IDifferenceBuffer"/> in an inline or side-by-side
    /// mode.  It keeps the scroll state of the different views in sync, and provides helpers for scrolling to differences
    /// and matches in all views.
    /// </summary>
    public interface IDifferenceViewer : IPropertyOwner
    {
        /// <summary>
        /// The <see cref="IDifferenceBuffer"/> that this viewer is displaying.
        /// </summary>
        IDifferenceBuffer DifferenceBuffer { get; }

        /// <summary>
        /// The view for displaying <see cref="DifferenceViewMode.Inline"/> differences.
        /// </summary>
        /// <remarks>Will never be <c>null</c>, but will only be visible when <see cref="ViewMode"/>
        /// is set to <see cref="DifferenceViewMode.Inline"/>.</remarks>
        ITextView InlineView { get; }

        /// <summary>
        /// The view for displaying the left buffer for <see cref="DifferenceViewMode.SideBySide"/> differences.
        /// </summary>
        /// <remarks>Will never be <c>null</c>, but will only be visible when <see cref="ViewMode"/>
        /// is set to <see cref="DifferenceViewMode.SideBySide"/>.</remarks>
        ITextView LeftView { get; }

        /// <summary>
        /// The view for displaying the right buffer for <see cref="DifferenceViewMode.SideBySide"/> differences.
        /// </summary>
        /// <remarks>Will never be <c>null</c>, but will only be visible when <see cref="ViewMode"/>
        /// is set to <see cref="DifferenceViewMode.SideBySide"/>.</remarks>
        ITextView RightView { get; }

        /// <summary>
        /// The view mode (inline or side-by-side).
        /// </summary>
        DifferenceViewMode ViewMode { get; set; }

        /// <summary>
        /// Raised when the <see cref="ViewMode"/> changes.
        /// </summary>
        event EventHandler<EventArgs> ViewModeChanged;

        /// <summary>
        /// Identifies the active view that last had focus.
        /// </summary>
        DifferenceViewType ActiveViewType { get; }

        /// <summary>
        /// Used to get or set general difference viewer options (<see cref="DifferenceViewerOptions"/>).
        /// </summary>
        IEditorOptions Options { get; }

        /// <summary>
        /// Are the left and right views are synchronized in the side by side view.
        /// </summary>
        /// <remarks>
        /// <para>In the side by side view, the left and right views are, normally, synchronized so that so that matching text always shown in each view.
        /// If this synchronization is turned off, then each view will scroll independently.</para>
        /// </remarks>
        bool AreViewsSynchronized { get; }

        /// <summary>
        /// Close the viewer and all contained hosts.
        /// </summary>
        void Close();

        /// <summary>
        /// Determine if this viewer is closed.
        /// </summary>
        bool IsClosed { get; }

        /// <summary>
        /// Raised when the view is closed.
        /// </summary>
        event EventHandler<EventArgs> Closed;

        #region Methods for scrolling

        /// <summary>
        /// Given the cursor position in the last focused text view, scroll and move the caret to the next difference.
        /// </summary>
        /// <param name="wrap">Wrap to the first difference if there is no next difference.</param>
        /// <returns><c>true</c> on success (if there was a next difference), <c>false</c> otherwise.</returns>
        bool ScrollToNextChange(bool wrap = false);

        /// <summary>
        /// Scroll and move the caret to the next difference after the specified location.
        /// </summary>
        /// <param name="point">Location to start scrolling from.</param>
        /// <param name="wrap">Wrap to the first difference if there is no next difference.</param>
        /// <returns><c>true</c> on success (if there was a next difference), <c>false</c> otherwise.</returns>
        bool ScrollToNextChange(SnapshotPoint point, bool wrap = false);

        /// <summary>
        /// Given the cursor position in the last focused text view, scroll and move the caret to the previous difference.
        /// </summary>
        /// <param name="wrap">Wrap to the last difference if there is no previous difference.</param>
        /// <returns><c>true</c> on success (if there was a previous difference), <c>false</c> otherwise.</returns>
        bool ScrollToPreviousChange(bool wrap = false);

        /// <summary>
        /// Scroll and move the caret to the previous difference before the specified location.
        /// </summary>
        /// <param name="point">Location to start scrolling from.</param>
        /// <param name="wrap">Wrap to the last difference if there is no previous difference.</param>
        /// <returns><c>true</c> on success (if there was a next difference), <c>false</c> otherwise.</returns>
        bool ScrollToPreviousChange(SnapshotPoint point, bool wrap = false);

        /// <summary>
        /// Scroll and move the caret to the start of the given difference.
        /// </summary>
        /// <param name="difference">The difference to scroll to.</param>
        void ScrollToChange(Difference difference);

        /// <summary>
        /// Scroll and move the caret to the start of the given match.
        /// </summary>
        void ScrollToMatch(Match match);

        #endregion
    }
}