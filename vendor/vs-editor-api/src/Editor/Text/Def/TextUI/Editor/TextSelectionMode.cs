//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Editor
{
    /// <summary>
    /// Specifies the text selection mode.
    /// </summary>
    public enum TextSelectionMode
    {
        /// <summary>
        /// A simple selection (only one span)
        /// </summary>
        Stream,
        /// <summary>
        /// A box selection (from a start line and column to an end line and column).
        /// </summary>
        Box
    }
}
