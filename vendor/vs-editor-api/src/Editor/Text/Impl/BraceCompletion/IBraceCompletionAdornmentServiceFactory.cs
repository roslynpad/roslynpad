//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
namespace Microsoft.VisualStudio.Text.BraceCompletion.Implementation
{
    using Microsoft.VisualStudio.Text.Editor;

    public interface IBraceCompletionAdornmentServiceFactory
    {
        /// <summary>
        /// Creates an IBraceCompletionAdornmentService for the given text view.
        /// </summary>
        /// <remarks>Only one IBraceCompletionAdornmentService will exist per view.</remarks>
        IBraceCompletionAdornmentService GetOrCreateService(ITextView textView);
    }
}
