//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain internal APIs that are subject to change without notice.
// Use at your own risk.
//
using System;
using System.Threading;

namespace Microsoft.VisualStudio.Language.Intellisense.Utilities
{
    public enum WaitIndicatorResult
    {
        Completed,
        Canceled,
    }

    public interface IWaitContext : IDisposable
    {
        CancellationToken CancellationToken { get; }

        bool AllowCancel { get; set; }
        string Message { get; set; }

        void UpdateProgress();
    }
}
