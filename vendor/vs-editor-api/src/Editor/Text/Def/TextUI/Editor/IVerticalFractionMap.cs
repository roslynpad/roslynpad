//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Editor
{
    using System;
    using Microsoft.VisualStudio.Text;

    /// <summary>
    /// <para>Maps between character positions and fractions of the total vertical extent of an <see cref="ITextView"/>.</para>
    /// </summary>
    /// <remarks>
    /// <para>Valid text positions range are [0...TextView.TextSnapshot.Length].
    /// Valid scrollbar coordinates are [0.0 ... 1.0].
    /// 0.0 corresponds to the top of the first line in the text view; 1.0 corresponds to the bottom of the last line in the view.
    /// Not every text position will have a unique value. For example, every character on
    /// the same text buffer line will have the same value, assuming that word wrap is not enabled.</para>
    /// <para>This interface is the base type of the <see cref="IScrollMap"/> interface, 
    /// which is created using the <see cref="IScrollMapFactoryService"/>.</para>
    /// </remarks>
    public interface IVerticalFractionMap
    {
        /// <summary>
        /// Gets the text view to which this fraction map applies.
        /// </summary>
        ITextView TextView { get; }

        /// <summary>
        /// Gets the fraction of the vertical extent of the view that corresponds to the specified buffer position.
        /// </summary>
        /// <param name="bufferPosition">The buffer position.</param>
        /// <returns>The corresponding fraction of the vertical extent of the view.</returns>
        double GetFractionAtBufferPosition(SnapshotPoint bufferPosition);

        /// <summary>
        /// Gets the buffer position that corresponds to a fraction of the vertical extent of the view,
        /// if it exists.
        /// </summary>
        /// <param name="fraction">The fraction of the vertical extent of the view.</param>
        /// <returns>The corresponding character position.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="fraction"/> is NaN, less than 0.0 or greater than 1.0.</exception>
        /// <remarks>Different buffer positions can have the same fractions. This method is guaranteed only to be consistent: it will
        /// return the same position for the same fraction. The exact character returned depends on the implementation of the fraction map.
        /// It will, generally, be the first character on the line, but this is not guaranteed.</remarks>
        SnapshotPoint GetBufferPositionAtFraction(double fraction);

        /// <summary>
        /// Occurs when the mapping between character position and its vertical fraction has changed.
        /// For example, the view may have re-rendered some lines, changing their font size.
        /// </summary>
        event EventHandler MappingChanged;
    }
}
