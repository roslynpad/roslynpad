//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text
{
    /// <summary>
    /// Extends <see cref="ITextChange"/> with the concept of an opaque change.
    /// </summary>
    public interface ITextChange2 : ITextChange
    {
        /// <summary>
        /// Indicates whether the change is opaque. Opaque changes are always replacements in which both the 
        /// old text and new text are non-empty. The differ from other changes in the
        /// manner in which <see cref="ITrackingPoint"/>s and <see cref="ITrackingSpan"/>s behave. When tracking
        /// across an opaque replacement, a point or span endpoint that lies within the deleted text will keep the same offset
        /// within the inserted text (normally a point would move either to the beginning or end of the inserted text,
        /// depending on its tracking mode). 
        /// </summary>
        bool IsOpaque { get; }
    }
}
