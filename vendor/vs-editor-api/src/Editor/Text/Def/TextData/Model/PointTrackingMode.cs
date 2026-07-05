//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text
{
    /// <summary>
    /// Represents tracking modes for <see cref="ITrackingPoint"/> objects.
    /// </summary>
    public enum PointTrackingMode
    {
        /// <summary>
        /// With this setting, a point tracks toward the end of the document, so that an
        /// insertion at the current position pushes the point to the end of the inserted text. 
        /// If a replacement contains the point, it will end up at the end of the replacement text.
        /// </summary>
        Positive,

        /// <summary>
        /// With this setting, a point tracks toward the beginning of the document, 
        /// so that an insertion at the current position leaves the point unaffected.  If a
        /// replacement contains the point, it will end up at the beginning of the replacement text.
        /// </summary>
        Negative
    }
}
