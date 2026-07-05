//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Editor
{
    using Microsoft.VisualStudio.Text;

    /// <summary>
    /// Defines an element in an adornment layer.
    /// </summary>
    public interface IXPlatAdornmentLayerElement
    {
        /// <summary>
        /// Gets the snapshot span that is associated with the adornment.
        /// </summary>
        SnapshotSpan? VisualSpan { get; }
        /// <summary>
        /// Gets the positioning behavior of the adornment.
        /// </summary>
        XPlatAdornmentPositioningBehavior Behavior { get; }
        /// <summary>
        /// Gets the adornment.
        /// </summary>
        object Adornment { get; }

        /// <summary>
        /// Gets the tag associated with the adornment.
        /// </summary>
        object Tag { get; }
        /// <summary>
        /// Defines the behavior when an adornment has been removed.
        /// </summary>
        XPlatAdornmentRemovedCallback RemovedCallback { get; }
    }
}
