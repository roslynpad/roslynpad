using System;

namespace Microsoft.VisualStudio.Utilities
{
    /// <summary>
    /// A scope of work displayed by a background work indicator
    /// (see <c>Microsoft.VisualStudio.Text.Editor.IBackgroundWorkIndicator</c>).
    /// </summary>
    public abstract class BackgroundWorkOperationScope : IDisposable
    {
        /// <summary>
        /// The description of the work shown to the user.
        /// </summary>
        public abstract string Description { get; set; }

        public abstract void Dispose();
    }
}
