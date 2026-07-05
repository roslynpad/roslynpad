//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Formatting
{
    /// <summary>
    /// Specifies the visibility of an <see cref="ITextViewLine"/> with respect to the visible area when the line was rendered.
    /// </summary>
    /// <remarks>
    /// <para>An <see cref="ITextViewLine"/> is considered partially visible when its
    /// bottom is equal to the top of the visible area.</para>
    /// <para>Unattached lines are lines that were not formatted as part of a layout in the text view.</para>
    /// </remarks>
    public enum VisibilityState
    {
        /// <summary>
        /// The line is unattached, that is, it was not formatted as part of a layout in the text view.
        /// </summary>
        Unattached,

        /// <summary>
        /// The line is hidden, that is, not visible inside the view. Lines are also hidden when 
        /// their bottom edge is even with the top of the view or their top edge is even with the top of the view.
        /// </summary>
        Hidden,

        /// <summary>
        /// The line is partially visible, that is, 
        /// some portion of the line extends above the top of the view and/or below the bottom of the view.
        /// </summary>
        PartiallyVisible,

        /// <summary>
        /// The line is fully visible.
        /// </summary>
        FullyVisible
    };
}