namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Tracks state of a visible or calculating Quick Info session.
    /// </summary>
    public interface IAsyncQuickInfoSession2 : IAsyncQuickInfoSession
    {
        /// <summary>
        /// Check if mouse is over the Quick Info tip, to prevent triggering a new session for
        /// a span that appears below the tip.
        /// </summary>
        bool IsMouseOverAggregated { get; }
    }
}
