//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Differencing
{
    /// <summary>
    /// The view type for a view created by an <see cref="IDifferenceViewer"/>.
    /// </summary>
    public enum DifferenceViewType
    {
        /// <summary>
        /// View displaying the contents of the inline buffer (which is combines text from the left and right buffers).
        /// </summary>
        InlineView = 0,

        /// <summary>
        /// View containing the contents of the left buffer.
        /// </summary>
        LeftView = 1,

        /// <summary>
        /// View containing the contents of the right buffer.
        /// </summary>
        RightView = 2
    }
}
