//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.Text.Operations
{
#if false   // Work in progress
    public interface IUndoMetadataEditTag : IEditTag
    {
        /// <summary>
        /// The view from which the edit was initiated. May be null.
        /// </summary>
        ITextView InitiatingView { get; }

        /// <summary>
        /// A localized description of the edit (which can be displayed in the undo list).
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Consecutive edits with the same, non-null, may be merged.
        /// </summary>
        object MergeType { get; }
    }
#endif
}
