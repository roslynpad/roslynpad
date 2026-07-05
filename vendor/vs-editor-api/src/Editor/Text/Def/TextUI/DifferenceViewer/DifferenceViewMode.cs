//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Differencing
{
    /// <summary>
    /// The view mode for an <see cref="IDifferenceViewer"/>.
    /// </summary>
    public enum DifferenceViewMode
    {
        /// <summary>
        /// View differences inline, mixing the removed and added regions in one view pane.
        /// </summary>
        Inline = 0,
        
        /// <summary>
        /// Show only the left file.
        /// </summary>
        LeftViewOnly = 1,

        /// <summary>
        /// Show only the right file.
        /// </summary>
        RightViewOnly = 2,

        /// <summary>
        /// View differences side-by-side, where the left pane is the left file and the right pane is the right file.
        /// </summary>
        SideBySide = 3
    }
}
