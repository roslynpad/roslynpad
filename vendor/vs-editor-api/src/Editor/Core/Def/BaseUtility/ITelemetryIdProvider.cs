//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Utilities
{
    /// <summary>
    /// Represents an object that can provide a unique ID for telemetry purposes.
    /// <typeparam name="TId">Type of the telemetry ID.</typeparam>
    /// </summary>
    public interface ITelemetryIdProvider<TId>
    {
        /// <summary>
        /// Tries to get a unique ID for telemetry purposes.
        /// </summary>
        /// <returns><c>true</c> if a unique telemetry ID was returned, <c>false</c> if this object refuses to participate in telemetry logging.</returns>
        bool TryGetTelemetryId(out TId telemetryId);
    }
}
