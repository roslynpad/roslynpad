//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain internal APIs that are subject to change without notice.
// Use at your own risk.
//

using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.Text.Utilities
{
    public interface ITypingTelemetrySessionProvider
    {
        ITypingTelemetrySession CreateTypingTelemetrySession(ITextView textView);
    }
}
