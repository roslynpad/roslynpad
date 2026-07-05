//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain internal APIs that are subject to change without notice.
// Use at your own risk.
//

using System.Collections.Generic;
using System;

namespace Microsoft.VisualStudio.Text.Utilities
{
    /// <summary>
    /// Allows code in VS-Platform to log events.
    /// </summary>
    /// <remarks>
    /// For example, the VS Provider of this inserts data points into the telemetry data stream.
    /// </remarks>
    public interface ILoggingServiceInternal
    {
        /// <summary>
        /// Post the event named <paramref name="key"/> to the telemetry stream. Additional properties can be appended as name/value pairs in <paramref name="namesAndProperties"/>.
        /// </summary>
        void PostEvent(string key, params object[] namesAndProperties);

        /// <summary>
        /// Post the event named <paramref name="key"/> to the telemetry stream. Additional properties can be appended as name/value pairs in <paramref name="namesAndProperties"/>.
        /// </summary>
        void PostEvent(string key, IReadOnlyList<object> namesAndProperties);

        void PostEvent(
            TelemetryEventType eventType,
            string eventName,
            TelemetryResult result = TelemetryResult.Success,
            params (string name, object property)[] namesAndProperties);

        void PostEvent(
            TelemetryEventType eventType,
            string eventName,
            TelemetryResult result,
            IReadOnlyList<(string name, object property)> namesAndProperties);

        /// <summary>
        /// Creates and posts a FaultEvent.
        /// </summary>
        /// <param name="eventName">
        /// An event name following data model schema.
        /// It requires that event name is a unique, not null or empty string.
        /// It consists of 3 parts and must follows pattern [product]/[featureName]/[entityName]. FeatureName could be a one-level feature or feature hierarchy delimited by "/".
        /// For examples,
        /// vs/platform/opensolution;
        /// vs/platform/editor/lightbulb/fixerror;
        /// </param>
        /// <param name="description">Fault description</param>
        /// <param name="exceptionObject">Exception instance</param>
        /// <param name="additionalErrorInfo">Additional information to be added to Watson's ErrorInformation.txt file.</param>
        /// <param name="isIncludedInWatsonSample">
        /// Gets or sets a value indicating whether we sample this event locally. Affects Watson only.
        /// If false, will not send to Watson: only sends the telemetry event to AI and doesn't call callback.
        /// Changing this will force the event to send to Watson. Be careful because it can have big perf impact.
        /// If unchanged, it will be set according to the default sample rate.
        /// </param>
        /// <param name="correlations">TelemetryEventCorrelations which help correlate this fault to the scope it was executing within</param>
        void PostFault(
            string eventName,
            string description,
            Exception exceptionObject,
            string additionalErrorInfo = null,
            bool? isIncludedInWatsonSample = null,
            object[] correlations = null
            );

        /// <summary>
        /// Adjust the counter associated with <paramref name="key"/> and <paramref name="name"/> by <paramref name="delta"/>.
        /// </summary>
        /// <remarks>
        /// <para>Counters start at 0.</para>
        /// <para>No information is sent over the wire until the <see cref="PostCounters"/> is called.</para>
        /// </remarks>
        void AdjustCounter(string key, string name, int delta = 1);

        /// <summary>
        /// Post all of the counters.
        /// </summary>
        /// <remarks>
        /// <para>The counters are logged as if PostEvent had been called for each key with a list counter names and values.</para>
        /// <para>The counters are cleared as a side-effect of this call.</para>
        /// </remarks>
        void PostCounters();

        object CreateTelemetryOperationEventScope(string eventName, TelemetrySeverity severity, object[] correlations, IDictionary<string, object> startingProperties);
        object GetCorrelationFromTelemetryScope(object telemetryScope);
         void EndTelemetryScope(object telemetryScope, TelemetryResult result, string summary = null);
    }
}
