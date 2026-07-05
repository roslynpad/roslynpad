//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//

using System;

namespace Microsoft.VisualStudio.Utilities
{
    /// <summary>
    /// Operations that guard calls to extensions code, track performance and log errors.
    /// </summary>
    /// <remarks>This class supports the Visual Studio 
    /// infrastructure and in general is not intended to be used directly from your code.</remarks>
    public interface IGuardedOperations2 : IGuardedOperations
    {
        /// <summary>
        /// Logs an exception silently, without notifying the user.
        /// </summary>
        /// <param name="errorSource">Reference to the extension object or event handler that threw the exception</param>
        /// <param name="e">Exception to log</param>
        /// <remarks>This method can be invoked from any thread.</remarks>
        void LogException(object errorSource, Exception e);
    }
}
