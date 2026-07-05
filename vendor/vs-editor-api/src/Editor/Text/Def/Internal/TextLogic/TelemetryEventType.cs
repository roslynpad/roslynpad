namespace Microsoft.VisualStudio.Text.Utilities
{
    /// <summary>
    /// Supported telemetry event types.
    /// </summary>
    public enum TelemetryEventType : int
    {
        /// <summary>
        /// User task event
        /// </summary>
        UserTask = 0,

        /// <summary>
        /// Trace event
        /// </summary>
        Trace = 1,

        /// <summary>
        /// Operation event
        /// </summary>
        Operation = 2,

        /// <summary>
        /// Fault event
        /// </summary>
        Fault = 3,

        /// <summary>
        /// Asset event
        /// </summary>
        Asset = 4,
    }
}
