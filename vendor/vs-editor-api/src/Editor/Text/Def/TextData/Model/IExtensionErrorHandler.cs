//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
using System;

namespace Microsoft.VisualStudio.Text
{
    /// <summary>
    /// Allows editor hosts to detect exceptions that get captured at extension points.
    /// </summary>
    /// <remarks>This is a MEF component part, and should be imported as follows:
    /// [Import]
    /// IExtensionErrorHandler errorHandler = null;
    /// </remarks>
    public interface IExtensionErrorHandler
    {
        /// <summary>
        /// Logs an exception to ActivityLogs and the telemetry, and notifies that an exception has occured.
        /// </summary>
        /// <param name="sender">The extension object or event handler that threw the exception.</param>
        /// <param name="exception">The exception that was thrown.</param>
        void HandleError(object sender, Exception exception);
    }
}
