//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Represents an object that can provide a Diagnostic ID for telemetry purposes.
    /// </summary>
    public interface ITelemetryDiagnosticID<T>
    {
        /// <summary>
        /// Get Diagnostic ID for telemetry purposes.
        /// </summary>
        /// <returns></returns>
        T GetDiagnosticID();
    }
}

