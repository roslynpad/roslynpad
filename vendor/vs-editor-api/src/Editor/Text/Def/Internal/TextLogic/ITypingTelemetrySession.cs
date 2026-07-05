//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain internal APIs that are subject to change without notice.
// Use at your own risk.
//

using System;

namespace Microsoft.VisualStudio.Text.Utilities
{
    public interface ITypingTelemetrySession : IDisposable
    {
        void AfterKeyProcessed();
        void BeforeKeyProcessed();
    }
}
