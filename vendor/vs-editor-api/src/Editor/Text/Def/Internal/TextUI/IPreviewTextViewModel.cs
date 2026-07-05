//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain internal APIs that are subject to change without notice.
// Use at your own risk.
//
namespace Microsoft.VisualStudio.Text.Editor
{
    /// <summary>
    /// <see cref="ITextViewModel"/> used by the view shown when hovering over the scroll bar (which will have the <see cref="PredefinedTextViewRoles.PreviewTextView"/> role).
    /// </summary>
    public interface IPreviewTextViewModel : ITextViewModel
    {
        /// <summary>
        /// Pointer to the view for which this is a preview.
        /// </summary>
        ITextView SourceView { get; }
    }
}
