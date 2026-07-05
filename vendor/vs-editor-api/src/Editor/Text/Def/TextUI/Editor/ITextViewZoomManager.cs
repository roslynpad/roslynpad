//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//

namespace Microsoft.VisualStudio.Text.Editor
{
    /// <summary>
    /// Provides methods to manipulate zoom level of text views.
    /// </summary>
    /// <remarks>This is a MEF Component, and should be imported as follows:
    /// <code>
    /// [Import]
    /// ITextViewZoomManager zoomManager = null;
    /// </code>
    /// </remarks>
    public interface ITextViewZoomManager
    {
        /// <summary>
        /// Zooms in to the text view by a scaling factor of 10%.
        /// </summary>
        /// <remarks>
        /// The maximum zooming scale is 400%.
        /// </remarks>
        void ZoomIn(ITextView textView);

        /// <summary>
        /// Zooms out of the text view by a scaling factor of 10%.
        /// </summary>
        /// <remarks>
        /// The minimum zooming scale is 20%.
        /// </remarks>
        void ZoomOut(ITextView textView);

        /// <summary>
        /// Applies the given zoomLevel to the text view.
        /// </summary>
        /// <param name="zoomLevel">The zoom level to apply between 20% to 400%.</param>
        void ZoomTo(ITextView textView, double zoomLevel);

        /// <summary>
        /// Gets ZoomLevel between 20% to 400% of text view.
        /// </summary>
        double ZoomLevel(ITextView textView);
    }
}
