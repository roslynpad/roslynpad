//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Differencing
{
    /// <summary>
    /// The line type, as used in methods on <see cref="IDifferenceBuffer"/>.
    /// </summary>
    public enum LineType
    {
        /// <summary>
        /// A line that was added, meaning it only appears in the right buffer.
        /// </summary>
        Added,

        /// <summary>
        /// A line that was removed, meaning it only appears in the left buffer.
        /// </summary>
        Removed,

        /// <summary>
        /// A line that appears in both the left and right buffer.
        /// </summary>
        Matched
    }
}
