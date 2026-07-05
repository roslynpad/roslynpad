//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text
{
    /// <summary>
    /// Represents special tracking behaviors for <see cref="ITrackingPoint"/> and <see cref="ITrackingSpan"/> objects.
    /// </summary>
    public enum TrackingFidelityMode
    {
        /// <summary>
        /// When moving back to a previous version (either by explicitly 
        /// moving to that version or by undo or redo operations), the result may be different from the result
        /// that was originally given for that version. This mode is suitable for most purposes, 
        /// and is the most space-efficient mode.
        /// </summary>
        Forward,

        /// <summary>
        /// When mapping back to a previous version, the result is the same as the result from 
        /// mapping forward from the origin version. This mode should be used only 
        /// for short-lived points and spans.
        /// </summary>
        Backward,

        /// <summary>
        /// When mapping to a version that is the result of undo 
        /// or redo operations, the result will be the same as the result from mapping forward to the 
        /// version of which the undo or redo is a reiteration. This mode is more 
        /// expensive than <see cref="Forward"/> in both space and time and should be used only
        /// if necessary.
        /// </summary>
        UndoRedo
    }
}
