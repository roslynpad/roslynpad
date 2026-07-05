namespace Microsoft.VisualStudio.Language.Intellisense
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.Text.Adornments;
    using Microsoft.VisualStudio.Threading;

    /// <summary>
    /// Source of Quick Info tooltip content item, proffered to the IDE by a <see cref="IAsyncQuickInfoSourceProvider"/>.
    /// </summary>
    /// <remarks>
    /// This class is always constructed and disposed on the UI thread and called on
    /// a non-UI thread. Callers that require the UI thread must explicitly marshal there with
    /// <see cref="JoinableTaskFactory.SwitchToMainThreadAsync(CancellationToken)"/>.
    /// Content objects are resolved into UI constructs via the <see cref="IViewElementFactoryService"/>.
    /// </remarks>
    public interface IAsyncQuickInfoSource : IDisposable
    {
        /// <summary>
        /// Gets Quick Info item and tracking span via a <see cref="QuickInfoItem"/>.
        /// </summary>
        /// <remarks>
        /// This method is always called on a background thread. Multiple elements can be
        /// be returned by a single source by wrapping them in a <see cref="ContainerElement"/>.
        /// </remarks>
        /// <param name="session">An object tracking the current state of the Quick Info.</param>
        /// <param name="cancellationToken">Cancels an in-progress computation.</param>
        /// <returns>item and a tracking span for which these item are applicable.</returns>
        Task<QuickInfoItem> GetQuickInfoItemAsync(
            IAsyncQuickInfoSession session,
            CancellationToken cancellationToken);
    }
}
