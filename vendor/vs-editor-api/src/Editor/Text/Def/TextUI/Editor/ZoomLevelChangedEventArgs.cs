//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//

using System;

namespace Microsoft.VisualStudio.Text.Editor
{
    public sealed class ZoomLevelChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the new zoom level for an <see cref="ITextView3"/>.
        /// </summary>
        public double NewZoomLevel { get; }

        /// <summary>
        /// Initializes a new instance of a <see cref="ZoomLevelChangedEventArgs"/>.
        /// </summary>
        /// <param name="newZoomLevel">The new zoom level for an <see cref="ITextView3"/>.</param>
        public ZoomLevelChangedEventArgs(double newZoomLevel)
        {
            NewZoomLevel = newZoomLevel;
        }
    }
}