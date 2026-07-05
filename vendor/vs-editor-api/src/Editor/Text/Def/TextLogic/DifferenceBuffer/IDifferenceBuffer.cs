//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
using System;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.Text.Differencing
{
    /// <summary>
    /// A difference buffer constantly computes the differences between two <see cref="ITextBuffer"/>s,
    /// providing an <see cref="IProjectionBuffer"/>, <see cref="IDifferenceBuffer.InlineBuffer"/>, that
    /// contains the differences between the two <see cref="ITextBuffer"/>s in an inline difference.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The differences are computed on a background thread in response to various changes (text change,
    /// options changing, etc.), though all of the events around differencing, like <see cref="SnapshotDifferenceChanged"/>,
    /// will be raised on the thread that owns the <see cref="LeftBuffer"/> and <see cref="RightBuffer"/> (generally,
    /// the UI thread).
    /// </para>
    /// <para>
    /// Because the differences are computed asynchronously, the <see cref="CurrentSnapshotDifference"/> may be
    /// behind the current versions of any of the buffers, and will be <c>null</c> before the first difference
    /// is computed.
    /// </para>
    /// </remarks>
    public interface IDifferenceBuffer : IDisposable, IPropertyOwner
    {
        /// <summary>
        /// The source of the left buffer in the difference.
        /// </summary>
        ITextBuffer BaseLeftBuffer { get; }

        /// <summary>
        /// The left buffer of the difference.
        /// </summary>
        /// <remarks>This is a projection of the BaseLeftBuffer that has been made read-only. It's contents are identical to the contents of BaseLeftBuffer.</remarks>
        ITextBuffer LeftBuffer { get; }

        /// <summary>
        /// The source of the right buffer in the difference.
        /// </summary>
        ITextBuffer BaseRightBuffer { get; }

        /// <summary>
        /// The right buffer in the difference.
        /// </summary>
        /// <remarks>
        /// <para>This will either be equal to the BaseRightBuffer or it will be a projection of the BaseRightBuffer that has been made read-only. It's contents are identical to the contents of BaseRightBuffer.</para>
        /// 
        /// </remarks>
        ITextBuffer RightBuffer { get; }

        /// <summary>
        /// The top-level buffer, which contains the differences combined.
        /// </summary>
        IProjectionBuffer InlineBuffer { get; }

        /// <summary>
        /// The currently-used snapshot difference that matches up with the current snapshot
        /// of <see cref="InlineBuffer"/>.
        /// </summary>
        /// <remarks>Will be <c>null</c> before the first snapshot difference is computed.</remarks>
        ISnapshotDifference CurrentSnapshotDifference { get; }

        /// <summary>
        /// The snapshot of <see cref="InlineBuffer"/> that corresponds to the state at
        /// which <see cref="CurrentSnapshotDifference"/> is current.
        /// </summary>
        /// <remarks>Will be <c>null</c> if <see cref="CurrentSnapshotDifference"/> is <c>null</c>.</remarks>
        IProjectionSnapshot CurrentInlineBufferSnapshot { get; }
        
        /// <summary>
        /// Raised immediately before the <see cref="CurrentSnapshotDifference"/> and
        /// <see cref="InlineBuffer"/> are updated.
        /// </summary>
        event EventHandler<SnapshotDifferenceChangeEventArgs> SnapshotDifferenceChanging;

        /// <summary>
        /// Raised when the <see cref="CurrentSnapshotDifference"/> and
        /// <see cref="InlineBuffer"/> have changed.
        /// </summary>
        event EventHandler<SnapshotDifferenceChangeEventArgs> SnapshotDifferenceChanged;

        #region Customization settings

        /// <summary>
        /// Used to modify general difference buffer options (<see cref="DifferenceBufferOptions"/>).
        /// </summary>
        IEditorOptions Options { get; }

        /// <summary>
        /// Used to get or set the options used in differencing the two buffers. These options are used
        /// in calls to the <see cref="IHierarchicalStringDifferenceService"/> that performs the actual
        /// comparison.
        /// </summary>
        StringDifferenceOptions DifferenceOptions { get; set; }

        /// <summary>
        /// Is editing disabled in this <see cref="IDifferenceBuffer"/>?
        /// </summary>
        /// <remarks><para>If true, then this.RightBuffer is a read-only projection of this.BaseRightBuffer.</para></remarks>
        bool IsEditingDisabled { get; }

        /// <summary>
        /// Add a predicate to selectively ignore differences.
        /// </summary>
        /// <param name="predicate">A predicate to be called for every computed line difference.</param>
        void AddIgnoreDifferencePredicate(IgnoreDifferencePredicate predicate);

        /// <summary>
        /// Remove a predicate previously added with <see cref="AddIgnoreDifferencePredicate"/>.
        /// </summary>
        /// <param name="predicate">The predicate to remove.</param>
        /// <returns><c>true</c> if the predicate was found and removed, <c>false</c> otherwise.</returns>
        bool RemoveIgnoreDifferencePredicate(IgnoreDifferencePredicate predicate);

        /// <summary>
        /// Add a custom <see cref="SnapshotLineTransform"/>, which can modify lines of text before they are
        /// compared.
        /// </summary>
        /// <param name="transform">The transform to add.</param>
        void AddSnapshotLineTransform(SnapshotLineTransform transform);

        /// <summary>
        /// Remove a custom <see cref="SnapshotLineTransform"/> previously added with <see cref="AddSnapshotLineTransform"/>.
        /// </summary>
        /// <param name="transform">The transform to remove.</param>
        /// <returns><c>true</c> if the transform was found and removed, <c>false</c> otherwise.</returns>
        bool RemoveSnapshotLineTransform(SnapshotLineTransform transform);

        #endregion
    }
}
