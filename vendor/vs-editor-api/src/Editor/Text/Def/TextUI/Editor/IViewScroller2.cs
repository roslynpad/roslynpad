//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Editor
{
    internal interface IViewScroller2 : IViewScroller
    {
        /// <summary>
        /// Gets the width of a column in pixels.
        /// </summary>
        double ColumnWidth { get; }

        /// <summary>
        /// Scrolls the view one column to the left.
        /// </summary>
        /// <remarks>
        /// A column is the width of a space in the default font.
        /// </remarks>
        void ScrollColumnLeft();

        /// <summary>
        /// Scrolls the view one column to the right.
        /// </summary>
        /// <remarks>
        /// A column is the width of a space in the default font.
        /// </remarks>
        void ScrollColumnRight();
    }
}
