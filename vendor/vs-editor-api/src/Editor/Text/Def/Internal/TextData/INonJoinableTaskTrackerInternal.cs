//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
using System;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Text
{
    /// <summary>
    /// Internal tracker for non-joinable tasks. Used to ensure that all pending tasks
    /// have completed on editor host shutdown.
    /// </summary>
    /// <remarks>Methods of this interface can be called on any thread.</remarks>
    public interface INonJoinableTaskTrackerInternal
    {
        void Register(Task task);
    }
}
