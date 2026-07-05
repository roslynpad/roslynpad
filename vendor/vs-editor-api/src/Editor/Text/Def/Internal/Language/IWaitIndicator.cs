//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain internal APIs that are subject to change without notice.
// Use at your own risk.
//
using System;

namespace Microsoft.VisualStudio.Language.Intellisense.Utilities
{
    public interface IWaitIndicator
    {
        /// <summary>
        /// Schedule the action on the caller's thread and wait for the task to complete.
        /// </summary>
        WaitIndicatorResult Wait(string title, string message, bool allowCancel, Action<IWaitContext> action);
        IWaitContext StartWait(string title, string message, bool allowCancel);
    }
}