namespace Microsoft.VisualStudio.Utilities
{
    /// <summary>
    /// Represents an object that provides a localized display name
    /// to be used when it's being represented to the user, for
    /// example when blaming for delays.
    /// </summary>
    public interface INamed
    {
        /// <summary>
        /// Gets display name of an instance used to represent it to the user, for
        /// example when blaming it for delays.
        /// </summary>
        string DisplayName { get; }
    }
}

