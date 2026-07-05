//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Editor
{
    using System;
    using Microsoft.VisualStudio.Text;

    /// <summary>
    /// View state at a particular point in time.
    /// </summary>
    public class ViewState
    {
        /// <summary>
        /// Gets the X-coordinate of the viewport's left edge.
        /// </summary>
        public double ViewportLeft { get; private set; }

        /// <summary>
        /// Gets the Y-coordinate of the viewport's top edge.
        /// </summary>
        public double ViewportTop { get; private set; }

        /// <summary>
        /// Gets the Width of the viewport.
        /// </summary>
        public double ViewportWidth { get; private set; }

        /// <summary>
        /// Gets the Height of the viewport.
        /// </summary>
        public double ViewportHeight { get; private set; }

        /// <summary>
        /// Gets the X-coordinate of the viewport's right edge.
        /// </summary>
        public double ViewportRight { get { return this.ViewportLeft + this.ViewportWidth; } }

        /// <summary>
        /// Gets the Y-coordinate of the viewport's bottom edge.
        /// </summary>
        public double ViewportBottom { get { return this.ViewportTop + this.ViewportHeight; } }

        /// <summary>
        /// Gets the View's visual snapshot.
        /// </summary>
        public ITextSnapshot VisualSnapshot { get; private set; }

        /// <summary>
        /// Gets the view's edit snapshot.
        /// </summary>
        public ITextSnapshot EditSnapshot { get; private set; }

        /// <summary>
        /// Constructs a <see cref="ViewState"/>.
        /// </summary>
        /// <param name="view">The <see cref="ITextView"/> for this view state.</param>
        /// <param name="effectiveViewportWidth">The width of the view port for <paramref name="view"/>.</param>
        /// <param name="effectiveViewportHeight">The height of the view port for <paramref name="view"/>.</param>
        public ViewState(ITextView view, double effectiveViewportWidth, double effectiveViewportHeight)
        {
            if (view == null)
                throw new ArgumentNullException(nameof(view));

            this.ViewportLeft = view.ViewportLeft;
            this.ViewportTop = view.ViewportTop;
            this.ViewportWidth = effectiveViewportWidth;
            this.ViewportHeight = effectiveViewportHeight;

            this.VisualSnapshot = view.VisualSnapshot;
            this.EditSnapshot = view.TextSnapshot;
        }

        /// <summary>
        /// Constructs a <see cref="ViewState"/>.
        /// </summary>
        /// <param name="view">The <see cref="ITextView"/> for this view state.</param>
        public ViewState(ITextView view)
            : this(view, (view != null) ? view.ViewportWidth : 0.0, (view != null) ? view.ViewportHeight : 0.0)
        {
        }
    }
}
