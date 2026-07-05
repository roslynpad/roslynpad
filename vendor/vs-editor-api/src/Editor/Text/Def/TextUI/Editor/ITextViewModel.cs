//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Editor
{
    using System;
    using Microsoft.VisualStudio.Utilities;

    /// <summary>
    /// Represents a set of zero or more <see cref="ITextBuffer"/> objects that are unique to the presentation of text
    /// in a particular <see cref="ITextView"/>.
    /// </summary>
    public interface ITextViewModel : IPropertyOwner, IDisposable
    {
        /// <summary>
        /// The <see cref="ITextDataModel"/> that supplies the <see cref="DataBuffer"/> and the governing <see cref="IContentType"/> for the view.
        /// </summary>
        ITextDataModel DataModel { get; }

        /// <summary>
        /// Represents the <see cref="ITextBuffer"/> for the data level. The data level text buffer is the highest buffer in the graph that
        /// is shared across multiple views and is therefore the base of the view model.
        /// </summary>
        ITextBuffer DataBuffer { get; }

        /// <summary>
        /// The <see cref="ITextBuffer"/> in which editing positions are tracked and to which edits are applied.
        /// All the text that appears in the view must reside in this buffer.
        /// </summary>
        /// <remarks>
        /// This text buffer may be the same as the <see cref="DataBuffer"/>, or it may be a projection buffer
        /// or elision buffer whose ultimate source is the data buffer.
        /// </remarks>
        ITextBuffer EditBuffer { get; }

        /// <summary>
        /// The <see cref="ITextBuffer"/> whose contents should be presented in the editor. 
        /// </summary>
        /// <remarks>
        /// This text buffer may be the same as the <see cref="EditBuffer"/> or it may be a projection buffer
        /// or elision buffer whose ultimate source is the edit buffer.
        /// </remarks>
        ITextBuffer VisualBuffer { get; }

        /// <summary>
        /// Determines whether a point in the edit buffer is represented in the visual buffer.
        /// </summary>
        /// <param name="editBufferPoint">A point in the <see cref="EditBuffer"/>.</param>
        /// <param name="affinity">
        /// If the mapping is ambiguous, this parameter affects the mapping as follows:
        /// if <paramref name="affinity"/> is <see cref="PositionAffinity.Predecessor"/>, the mapping targets 
        /// the position immediately after the preceding character in the projection buffer; if <paramref name="affinity"/> is 
        /// <see cref="PositionAffinity.Successor"/>, the mapping targets the position immediately before the following character
        /// in the projection buffer. This parameter has no effect if the mapping is unambiguous.</param>
        /// <returns><c>true</c> if the point is represented in the visual buffer, otherwise <c>false</c>.</returns>
        /// <remarks>
        /// A point that is represented in the visual buffer may not be visible on screen, but if the view
        /// is scrolled to that position, then the point would become visible.
        /// </remarks>
        bool IsPointInVisualBuffer(SnapshotPoint editBufferPoint, PositionAffinity affinity);

        /// <summary>
        /// Gets a point in the <see cref="VisualBuffer"/> that corresponds to the specified point in the edit
        /// buffer. If the point is hidden or has an alternative representation, gets
        /// the nearest point to it.
        /// </summary>
        /// <remarks>The definition of "nearest" depends on the implementation of the text view model.</remarks>
        /// <param name="editBufferPoint">A point in the <see cref="EditBuffer"/>.</param>
        /// <returns>A point in the <see cref="VisualBuffer"/> that corresponds to the given point.</returns>
        SnapshotPoint GetNearestPointInVisualBuffer(SnapshotPoint editBufferPoint);

        /// <summary>
        /// Gets a point in the <see cref="VisualBuffer"/> that corresponds to the specified point in the edit
        /// buffer. If the point is hidden or has an alternative representation, gets
        /// the nearest point to it.
        /// </summary>
        /// <remarks>The definition of "nearest" depends on the implementation of the text view model.</remarks>
        /// <param name="editBufferPoint">A point in the <see cref="EditBuffer"/>.</param>
        /// <param name="targetVisualSnapshot">The snapshot of <see cref="VisualBuffer"/> to map to.</param>
        /// <param name="trackingMode">The <see cref="PointTrackingMode"/> to use when translating to targetVisualSnapshot.</param>
        /// <returns>A point in the <see cref="VisualBuffer"/> that corresponds to the given point in targetVisualSnapshot.</returns>
        SnapshotPoint GetNearestPointInVisualSnapshot(SnapshotPoint editBufferPoint, ITextSnapshot targetVisualSnapshot, PointTrackingMode trackingMode);
    }
}
