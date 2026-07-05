namespace Microsoft.VisualStudio.Text.Utilities
{
    /// <summary>
    /// Service for querying the status of A/B experiments.
    /// </summary>
    public interface IExperimentationServiceInternal
    {
        /// <summary>
        /// Checks whether or not the flight is enabled for this user.
        /// </summary>
        /// <param name="flightName">A name of a flight, up to 16 characters long.</param>
        /// <returns>True if this user has the specific flight enabled.</returns>
        /// <remarks>
        /// This method uses cached flighting results, meaning, that this method does not
        /// block to download flight membership data, but rather, returns false if the data
        /// is not yet available.
        /// </remarks>
        bool IsCachedFlightEnabled(string flightName);
    }
}
