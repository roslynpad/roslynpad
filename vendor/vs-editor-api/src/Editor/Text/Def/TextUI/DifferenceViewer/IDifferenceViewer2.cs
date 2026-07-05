//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Differencing
{
    using System;

    public interface IDifferenceViewer2 : IDifferenceViewer
    {
        /// <summary>
        /// Does the right view exist?
        /// </summary>
        /// <remarks>
        /// Differences views are created lazily <see cref="IDifferenceViewer.RightView"/> will create the view if it does not already exist.
        /// </remarks>
        bool RightViewExists { get; }

        /// <summary>
        /// Does the left view exist?
        /// </summary>
        /// <remarks>
        /// Differences views are created lazily <see cref="IDifferenceViewer.LeftView"/> will create the view if it does not already exist.
        /// </remarks>
        bool LeftViewExists { get; }

        /// <summary>
        /// Does the Inline view exist?
        /// </summary>
        /// <remarks>
        /// Differences views are created lazily <see cref="IDifferenceViewer.InlineView"/> will create the view if it does not already exist.
        /// </remarks>
        bool InlineViewExists { get; }
    }
}
