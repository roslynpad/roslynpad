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
    /// Defines the positioning of adornments.
    /// </summary>
    /// <remarks>
    /// This enum adds a mode to the AdornmentPositioningBehavior needed for diff but we don't want to expose. 
    /// </remarks>
    public enum AdornmentPositioningBehavior2
    {
        /// <summary>
        /// The adornment is not moved automatically.
        /// </summary>
        OwnerControlled = XPlatAdornmentPositioningBehavior.OwnerControlled,

        /// <summary>
        /// The adornment is positioned relative to the top left corner of the view.
        /// </summary>
        ViewportRelative = XPlatAdornmentPositioningBehavior.ViewportRelative,

        /// <summary>
        /// The adornment is positioned relative to the text in the view.
        /// </summary>
        TextRelative = XPlatAdornmentPositioningBehavior.TextRelative,

        /// <summary>
        /// Behaves like a AdornmentPositioningBehavior.TextRelative adornment but only scrolls vertically.
        /// </summary>
        TextRelativeVerticalOnly
    }
}