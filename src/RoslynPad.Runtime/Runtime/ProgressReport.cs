namespace RoslynPad.Runtime
{
    /// <summary>
    /// Reports progress to RoslynPad GUI
    /// </summary>
    public static class ProgressReport
    {
        /// <summary>
        /// Reports progress to display to GUI, progress parameter must be between 0.0 and 1.0 or null to hide progress.
        /// </summary>
        /// <param name="progress">Progress percentage (between 0.0 and 1.0) or null to hide progress</param>
        public static void Report(double? progress) => Helpers.ReportProgress(progress);
    }
}
