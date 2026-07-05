//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text
{
    /// <summary>
    /// Allows changing existing <see cref="ISelection"/> objects as part of <see cref="IMultiSelectionBroker.PerformActionOnAllSelections(System.Action{ISelectionTransformer})"
    /// and <see cref="IMultiSelectionBroker.TryPerformActionOnRegion(ISelection, out ISelection, System.Action{ISelectionTransformer})"/>./>
    /// </summary>
    public interface ISelectionTransformer
    {
        /// <summary>
        /// Gets the Selection to transform. This will change through calls to <see cref="PerformAction(PredefinedSelectionTransformations)"/>,
        /// <see cref="MoveTo(VirtualSnapshotPoint, bool, PositionAffinity)"/>, and
        /// <see cref="MoveTo(VirtualSnapshotPoint, VirtualSnapshotPoint, VirtualSnapshotPoint, PositionAffinity)"/>.
        /// </summary>
        Selection Selection { get; }

        /// <summary>
        /// Moves the insertion and active points to the given location.
        /// </summary>
        /// <param name="point">The point to move to.</param>
        /// <param name="select">If <c>true</c>, leaves the anchor point where it is. If <c>false</c>, moves the anchor point too.</param>
        /// <param name="insertionPointAffinity">
        /// The affinity of the insertion point. This is used in places like word-wrap where one buffer position can represent both the
        /// end of one line and the beginning of the next.
        /// </param>
        void MoveTo(VirtualSnapshotPoint point, bool select, PositionAffinity insertionPointAffinity);

        /// <summary>
        /// Sets the anchor, active, and insertion points to the specified locations.
        /// </summary>
        /// <param name="anchorPoint">Specifies the stationary end of the selection span.</param>
        /// <param name="activePoint">Specifies the mobile end of the selection span.</param>
        /// <param name="insertionPoint">Specifies the location of the caret.</param>
        /// <param name="insertionPointAffinity">
        /// Specifies the affinity of the insertion point. This is used in places like word-wrap where one buffer position can represent both the
        /// end of one line and the beginning of the next.
        /// </param>
        void MoveTo(VirtualSnapshotPoint anchorPoint, VirtualSnapshotPoint activePoint, VirtualSnapshotPoint insertionPoint, PositionAffinity insertionPointAffinity);

        /// <summary>
        /// Updates internal state to cache the current location as the desired reference point for navigation events.
        /// </summary>
        /// <remarks>
        /// This affects events like <see cref="PredefinedSelectionTransformations.MoveToPreviousLine"/> where the current
        /// X location of the rendered caret is used to project to the new location. Typically this method should be called
        /// in cases where the user is stating where they want to focus. Since this grabs the current state, there is no
        /// equivalent release method.
        /// </remarks>
        void CapturePreferredReferencePoint();

        /// <summary>
        /// Updates internal state to cache the current x location as the desired reference point for navigation events.
        /// </summary>
        /// <remarks>
        /// This affects events like <see cref="PredefinedSelectionTransformations.MoveToPreviousLine"/> where the current
        /// X location of the rendered caret is used to project to the new location. Typically this method should be called
        /// in cases where the user is stating where they want to focus. Since this grabs the current state, there is no
        /// equivalent release method.
        /// </remarks>
        void CapturePreferredXReferencePoint();

        /// <summary>
        /// Updates internal state to cache the current y location as the desired reference point for navigation events.
        /// </summary>
        /// <remarks>
        /// This affects events like <see cref="PredefinedSelectionTransformations.MovePageUp"/> where the current
        /// Y location of the rendered caret is used to project to the new location. Typically this method should be called
        /// in cases where the user is stating where they want to focus. Since this grabs the current state, there is no
        /// equivalent release method.
        /// </remarks>
        void CapturePreferredYReferencePoint();

        /// <summary>
        /// Transforms <see cref="Selection"/> in a predefined way.
        /// </summary>
        /// <param name="action">The kind of transformation to perform</param>
        void PerformAction(PredefinedSelectionTransformations action);
    }
}
