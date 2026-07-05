namespace Microsoft.VisualStudio.Text.Utilities
{
    /// <summary>
    /// An enum to define the result from user task or operation.
    /// </summary>
    public enum TelemetryResult : int
    {
        /// <summary>
        /// Used for unknown or unavailable result.
        /// </summary>
        None = 0,

        /// <summary>
        /// A result without any failure from product or user.
        /// </summary>
        Success = 1,

        /// <summary>
        /// A result to indicate the action/operation failed because of product issue (not user faults)
        /// Consider using FaultEvent to provide more details about the failure.
        /// </summary>
        Failure = 2,

        /// <summary>
        /// A result to indicate the action/operation failed because of user fault (e.g., invalid input).
        /// Consider using FaultEvent to provide more details.
        /// </summary>
        UserFault = 3,

        /// <summary>
        /// A result to indicate the action/operation is cancelled by user.
        /// </summary>
        UserCancel = 4
    }
}
