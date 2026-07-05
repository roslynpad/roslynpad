//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain internal APIs that are subject to change without notice.
// Use at your own risk.
//
namespace Microsoft.VisualStudio.Language.Intellisense.Test
{
    /// <summary>
    /// Test contract for ICompletionSession.
    /// </summary>
    public interface ICompletionSession_Test
    {
        /// <summary>
        /// Triggers the session match.
        /// </summary>
        void TriggerSessionMatch();
    }
}
