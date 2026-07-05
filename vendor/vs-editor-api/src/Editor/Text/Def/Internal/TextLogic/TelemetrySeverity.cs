namespace Microsoft.VisualStudio.Text.Utilities
{
    //
    // Summary:
    //     An enum to define the severity of the telemetry event. It is used for any data
    //     consumer who wants to categorize data based on severity.
    public enum TelemetrySeverity : int
    {
        //
        // Summary:
        //     indicates telemetry event with verbose information.
        Low = -10,
        //
        // Summary:
        //     indicates a regular telemetry event.
        Normal = 0,
        //
        // Summary:
        //     indicates telemetry event with high value or require attention (e.g., fault).
        High = 10
    }
}
