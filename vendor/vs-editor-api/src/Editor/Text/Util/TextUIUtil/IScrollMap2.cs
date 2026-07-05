//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Utilities
{
    using Microsoft.VisualStudio.Text.Editor;

    /// <summary>
    /// <para>Defines the mapping between character positions and scrollmap coordinates. This is not
    /// the same as the coordinate system in which the scrollbar is rendered.</para>
    /// </summary>
    /// <remarks>
    /// <para>Valid text positions range are [0...TextView.TextSnapshot.Length].</para>
    /// <para>Corresponding scrollmap coordinates are [0.0 ... CoordinateOfBufferEnd].</para>
    /// <para>Not every buffer position will have a distinct scrollmap coordinate. For example, every character on the same line of text will,
    /// generally, have the same scrollmap coordinate.</para>
    /// <para>Different scrollmap coordinates may map to the same buffer position. For example, scrollmap coordinates in the range [0.0, 1.0) will, generally,
    /// map to the first character of the buffer.</para>
    /// </remarks>
    public interface IScrollMap2 : IScrollMap
    {
        void GetThumbTopAndBottom(out double thumbTop, out double thumbBottom);

        void ScrollToCoordinate(double coordinate);
        void CenterOnCoordinate(double coordinate);
    }
}
