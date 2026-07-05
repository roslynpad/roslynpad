//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain internal APIs that are subject to change without notice.
// Use at your own risk.
//
namespace Microsoft.VisualStudio.Text.Editor
{
    /// <summary>
    /// Enumeration of the possible ways that an inter-line adornment can be positioned horizontally on a line.
    /// </summary>
    public enum HorizontalPositioningMode
    {
        /// <summary>
        /// Adornment is positioned with respect to the left edge of the character at the tag's position.
        /// </summary>
        TextRelative,

        /// <summary>
        /// Adornment is positioned with respect to the left edge of the viewport.
        /// </summary>
        ViewRelative,

        /// <summary>
        /// Adornment is positioned with respect to the left edge of the view.
        /// </summary>
        Absolute
    }
}
