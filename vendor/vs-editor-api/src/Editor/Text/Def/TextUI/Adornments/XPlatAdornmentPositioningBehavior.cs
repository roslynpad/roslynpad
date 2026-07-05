//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Editor
{
    /// <summary>
    /// Defines the positioning of adornments.
    /// </summary>
    public enum XPlatAdornmentPositioningBehavior
    {
        /// <summary>
        /// The adornment is not moved automatically.
        /// </summary>
        OwnerControlled,    

        /// <summary>
        /// The adornment is positioned relative to the top left corner of the view.
        /// </summary>
        ViewportRelative,

        /// <summary>
        /// The adornment is positioned relative to the text in the view.
        /// </summary>
        TextRelative,

        /// <summary>
        /// Behaves like a AdornmentPositioningBehavior.TextRelative adornment but only scrolls vertically.
        /// </summary>
        TextRelativeVerticalOnly
    }
}
