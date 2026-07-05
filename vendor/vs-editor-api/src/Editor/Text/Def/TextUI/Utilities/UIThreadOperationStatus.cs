namespace Microsoft.VisualStudio.Utilities
{
#pragma warning disable CA1717 // Only FlagsAttribute enums should have plural names
    /// <summary>
    /// Represents a status of executing a potentially long running operation on the UI thread.
    /// </summary>
    public enum UIThreadOperationStatus
#pragma warning restore CA1717 // Only FlagsAttribute enums should have plural names
    {
        /// <summary>
        /// An operation was successfully completed.
        /// </summary>
        Completed,

        /// <summary>
        /// An operation was cancelled.
        /// </summary>
        Canceled,
    }
}
