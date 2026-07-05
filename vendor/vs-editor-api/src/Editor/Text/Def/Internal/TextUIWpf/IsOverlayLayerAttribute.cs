//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain internal APIs that are subject to change without notice.
// Use at your own risk.
//
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.Text.Editor
{
    /// <summary>
    /// Indicates that an <see cref="AdornmentLayerDefinition"/> is an overlay layer.
    /// </summary>
    /// <remarks>
    /// <para>Layers that do not specify this attribute will not be considered overlay layers.</para>
    /// <para>An overlay layer is not part of the normal view stack (and is not moved when the view is scrolled).</para>
    /// <para>It only supports adornments that have the <see cref="AdornmentPositioningBehavior.OwnerControlled"/>.</para>
    /// <para>Adorments placed in an overlay layer use a coordinate system where (0, 0) is the top-left corner of the view.</para>
    /// </remarks>
    public sealed class IsOverlayLayerAttribute: SingletonBaseMetadataAttribute
    {
        /// <summary>
        /// Indicates whether an <see cref="AdornmentLayerDefinition"/> defines an overlay adornment layer or not.
        /// </summary>
        public bool IsOverlayLayer { get; private set; }

        /// <summary>
        /// Creates new insatnce of the <see cref="IsOverlayLayerAttribute"/> class.
        /// </summary>
        /// <param name="isOverlayLayer">Sets whether the adornment layer is an overlay layer.</param>
        public IsOverlayLayerAttribute(bool isOverlayLayer)
        {
            this.IsOverlayLayer = isOverlayLayer;
        }
    }
}
