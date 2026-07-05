//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain internal APIs that are subject to change without notice.
// Use at your own risk.
//
using System;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    public interface ICompletionSession2 : ICompletionSession
    {
        /// <summary>
        /// Raised following a call to <see cref="IIntellisenseSession.Match"/>.
        /// </summary>
        event EventHandler Matched;

        /// <summary>
        /// Raised after <see cref="ICompletionSession.Filter()"/> was executed.
        /// </summary>
        event EventHandler Filtered;
    }
}