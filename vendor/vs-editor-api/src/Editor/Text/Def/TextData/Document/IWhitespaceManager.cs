namespace Microsoft.VisualStudio.Text.Document
{
    /// <summary>
    /// Subscribes to buffer change events and provides access to a <see cref="NewlineState"/>
    /// object and a <see cref="LeadingWhitespaceState"/> that are kept in sync with the state
    /// of the buffer provided at creation time.
    /// </summary>
    public interface IWhitespaceManager
    {
        /// <summary>
        /// Gets an instance of <see cref="NewlineState"/> that is kept in sync with the buffer
        /// provided at creation time.
        /// </summary>
        NewlineState NewlineState { get; }

        /// <summary>
        /// Gets an instance of <see cref="LeadingWhitespaceState"/> that is kept in sync with the
        /// buffer provided at creation time.
        /// </summary>
        LeadingWhitespaceState LeadingWhitespaceState { get; }
    }
}
