namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Defines the possible <see cref="IAsyncQuickInfoSession"/> states.
    /// </summary>
    public enum QuickInfoSessionState
    {
        /// <summary>
        /// Session has been created but is not yet active.
        /// </summary>
        Created,

        /// <summary>
        /// Session is currently computing Quick Info content.
        /// </summary>
        Calculating,

        /// <summary>
        /// Session has been dismissed and is no longer active.
        /// </summary>
        Dismissed,

        /// <summary>
        /// Computation is complete and session is visible.
        /// </summary>
        Visible
    }
}
