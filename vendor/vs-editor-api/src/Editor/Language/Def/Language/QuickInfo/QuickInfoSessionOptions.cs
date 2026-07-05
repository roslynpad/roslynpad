namespace Microsoft.VisualStudio.Language.Intellisense
{
    using System;

    /// <summary>
    /// Options for customization of Quick Info behavior.
    /// </summary>
    [Flags]
    public enum QuickInfoSessionOptions
    {
        /// <summary>
        /// No options.
        /// </summary>
        None                    = 0b00000000,

        /// <summary>
        /// Dismisses Quick Info when the mouse moves away.
        /// </summary>
        TrackMouse              = 0b00000001
    }
}
