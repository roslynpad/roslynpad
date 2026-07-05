using System;

namespace Microsoft.VisualStudio.Utilities
{
    /// <summary>
    /// Represents a single scope of a context of executing potentially long running operation on the UI thread.
    /// Scopes allow multiple components running within an operation to share the same context.
    /// </summary>
    public interface IUIThreadOperationScope : IDisposable
    {
        /// <summary>
        /// Gets or sets whether the operation can be cancelled.
        /// </summary>
        bool AllowCancellation { get; set; }

        /// <summary>
        /// Gets user readable operation description.
        /// </summary>
        string Description { get; set; }

        /// <summary>
        /// The <see cref="IUIThreadOperationContext" /> owning this scope instance.
        /// </summary>
        IUIThreadOperationContext Context { get; }

        /// <summary>
        /// Progress tracker instance to report operation progress.
        /// </summary>
        IProgress<ProgressInfo> Progress { get; }
    }

#pragma warning disable CA1815 // Override equals and operator equals on value types
    /// <summary>
    /// Represents an update of a progress.
    /// </summary>
    public struct ProgressInfo
#pragma warning restore CA1815 // Override equals and operator equals on value types
    {
        /// <summary>
        /// A number of already completed items.
        /// </summary>
        public int CompletedItems { get; }

        /// <summary>
        /// A total number if items.
        /// </summary>
        public int TotalItems { get; }

        /// <summary>
        /// Creates a new instance of the <see cref="ProgressInfo"/> struct.
        /// </summary>
        /// <param name="completedItems">A number of already completed items.</param>
        /// <param name="totalItems">A total number if items.</param>
        public ProgressInfo(int completedItems, int totalItems)
        {
            this.CompletedItems = completedItems;
            this.TotalItems = totalItems;
        }
    }
}
