//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Editor
{
    /// <summary>
    /// Defines the constants used for zoom operations 
    /// </summary>
    public static class ZoomConstants
    {
        /// <summary>
        /// The maximum zoom allowed on the text view
        /// </summary>
        public const double MaxZoom = 400.0;

        /// <summary>
        /// The minimum zoom allowed on the text view
        /// </summary>
        public const double MinZoom = 20.0;

        /// <summary>
        /// The default zoom level on the text view
        /// </summary>
        public const double DefaultZoom = 100.0;

        /// <summary>
        /// The scaling factor used for zooming in and out of the view. The view zooms by a factor of 10%
        /// </summary>
        public const double ScalingFactor = 1.1; 
    }
}
