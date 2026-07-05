namespace Microsoft.VisualStudio.Language.Intellisense.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Composition;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.Language.Utilities;
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Adornments;
    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.Threading;
    using Microsoft.VisualStudio.Utilities;
    using IOrderableContentTypeMetadata = Microsoft.VisualStudio.Text.Utilities.OrderableContentTypeMetadata;

    [Export(typeof(IAsyncQuickInfoBroker))]
    [Shared]
    public sealed class AsyncQuickInfoBroker : IAsyncQuickInfoBroker
    {
        private readonly IEnumerable<Lazy<IAsyncQuickInfoSourceProvider, IOrderableContentTypeMetadata>> unorderedSourceProviders;
        private readonly IGuardedOperations guardedOperations;
        private readonly IToolTipService toolTipService;
        private readonly JoinableTaskContext joinableTaskContext;
        private IEnumerable<Lazy<IAsyncQuickInfoSourceProvider, IOrderableContentTypeMetadata>> orderedSourceProviders;

        [ImportingConstructor]
        public AsyncQuickInfoBroker(
            [ImportMany]IEnumerable<Lazy<IAsyncQuickInfoSourceProvider, IOrderableContentTypeMetadata>> unorderedSourceProviders,
            IGuardedOperations guardedOperations,
            IToolTipService toolTipService,
            JoinableTaskContext joinableTaskContext)
        {
            this.unorderedSourceProviders = unorderedSourceProviders ?? throw new ArgumentNullException(nameof(unorderedSourceProviders));
            this.guardedOperations = guardedOperations ?? throw new ArgumentNullException(nameof(guardedOperations));
            this.joinableTaskContext = joinableTaskContext ?? throw new ArgumentNullException(nameof(joinableTaskContext));
            this.toolTipService = toolTipService;
        }

        #region IAsyncQuickInfoBroker

        public IAsyncQuickInfoSession GetSession(ITextView textView)
        {
            if (textView == null)
            {
                throw new ArgumentNullException(nameof(textView));
            }

            if (textView.Properties.TryGetProperty(typeof(AsyncQuickInfoPresentationSession), out AsyncQuickInfoPresentationSession property))
            {
                return property;
            }

            return null;
        }

        public bool IsQuickInfoActive(ITextView textView) => GetSession(textView) != null;

        public Task<IAsyncQuickInfoSession> TriggerQuickInfoAsync(
            ITextView textView,
            ITrackingPoint triggerPoint,
            QuickInfoSessionOptions options,
            CancellationToken cancellationToken)
        {
            return this.TriggerQuickInfoAsync(
                textView,
                triggerPoint,
                options,
                null,
                cancellationToken);
        }

        public async Task<QuickInfoItemsCollection> GetQuickInfoItemsAsync(
            ITextView textView,
            ITrackingPoint triggerPoint,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            triggerPoint = await this.ResolveAndMapUpTriggerPointAsync(textView, triggerPoint, cancellationToken).ConfigureAwait(false);
            if (triggerPoint != null)
            {
                var session = new AsyncQuickInfoSession(
                                this.OrderedSourceProviders,
                                this.joinableTaskContext,
                                textView,
                                triggerPoint,
                                QuickInfoSessionOptions.None);

                var startedSession = await StartQuickInfoSessionAsync(session, cancellationToken).ConfigureAwait(false);
                if (startedSession != null)
                {
                    var results = new QuickInfoItemsCollection(startedSession.Content, startedSession.ApplicableToSpan);
                    await startedSession.DismissAsync().ConfigureAwait(false);

                    return results;
                }
            }

            return null;
        }

        #endregion

        #region Private Impl

        private async Task<IAsyncQuickInfoSession> TriggerQuickInfoAsync(
            ITextView textView,
            ITrackingPoint triggerPoint,
            QuickInfoSessionOptions options,
            PropertyCollection propertyCollection,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Dismiss any currently open session.
            var currentSession = this.GetSession(textView);
            if (currentSession != null)
            {
                await currentSession.DismissAsync().ConfigureAwait(true);
            }

            triggerPoint = await this.ResolveAndMapUpTriggerPointAsync(textView, triggerPoint, cancellationToken).ConfigureAwait(false);
            if (triggerPoint == null)
            {
                return null;
            }

            var newSession = new AsyncQuickInfoPresentationSession(
                this.OrderedSourceProviders,
                this.guardedOperations,
                this.joinableTaskContext,
                this.toolTipService,
                textView,
                triggerPoint,
                options,
                propertyCollection);

            // StartAsync() is responsible for dispatching a StateChange
            // event if canceled so no need to clean these up on cancellation.
            newSession.StateChanged += this.OnStateChanged;
            textView.Properties.AddProperty(typeof(AsyncQuickInfoPresentationSession), newSession);

            return await StartQuickInfoSessionAsync(newSession, cancellationToken).ConfigureAwait(false);
        }

        private IEnumerable<Lazy<IAsyncQuickInfoSourceProvider, IOrderableContentTypeMetadata>> OrderedSourceProviders
            => this.orderedSourceProviders ?? (this.orderedSourceProviders = Orderer.Order(this.unorderedSourceProviders));

        /// <summary>
        /// Gets a trigger point for this session on the view's buffer.
        /// </summary>
        /// <remarks>
        /// Get's the caret's tracking point, if <paramref name="trackingPoint"/> is null,
        /// and maps the chosen tracking point up to the view's buffer.
        /// </remarks>
        private async Task<ITrackingPoint> ResolveAndMapUpTriggerPointAsync(
            ITextView textView,
            ITrackingPoint trackingPoint,
            CancellationToken cancellationToken)
        {
            // Caret element requires UI thread.
            await this.joinableTaskContext.Factory.SwitchToMainThreadAsync();

            // We switched threads and there is some latency, so ensure that we're still not canceled.
            cancellationToken.ThrowIfCancellationRequested();

            if (trackingPoint == null)
            {
                // Get the trigger point from the caret if none is provided.
                SnapshotPoint caretPoint = textView.Caret.Position.BufferPosition;
                trackingPoint = caretPoint.Snapshot.CreateTrackingPoint(
                    caretPoint.Position,
                    PointTrackingMode.Negative);
            }
            else
            {
                // Map the provided trigger point to the view's buffer.
                trackingPoint = PointToViewBuffer(textView, trackingPoint);
                if (trackingPoint == null)
                {
                    return null;
                }
            }

            return trackingPoint;
        }

        private static async Task<IAsyncQuickInfoSession> StartQuickInfoSessionAsync(AsyncQuickInfoSession session, CancellationToken cancellationToken)
        {
            try
            {
                await session.UpdateAsync(allowUpdate: false, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                // Don't throw OperationCanceledException unless the caller canceled us.
                // This can happen if computation was canceled by a quick info source
                // dismissing the session during computation, which we want to consider
                // more of a 'N/A' than an error.
                return null;
            }

            return session.State == QuickInfoSessionState.Dismissed ? null : session;
        }

        // Listens for the session being dismissed so that we can remove it from the view's property bag.
        private void OnStateChanged(object sender, QuickInfoSessionStateChangedEventArgs e)
        {
            IntellisenseUtilities.ThrowIfNotOnMainThread(this.joinableTaskContext);

            if (e.NewState == QuickInfoSessionState.Dismissed)
            {
                if (sender is AsyncQuickInfoPresentationSession session)
                {
                    session.TextView.Properties.RemoveProperty(typeof(AsyncQuickInfoPresentationSession));
                    session.StateChanged -= this.OnStateChanged;
                    return;
                }

                Debug.Fail("Unexpected sender type");
            }
        }

        private ITrackingPoint PointToViewBuffer(ITextView textView, ITrackingPoint trackingPoint)
        {
            // Requires UI thread for BufferGraph.
            IntellisenseUtilities.ThrowIfNotOnMainThread(this.joinableTaskContext);

            if ((trackingPoint == null) || (textView.TextBuffer == trackingPoint.TextBuffer))
            {
                return trackingPoint;
            }

            var targetSnapshot = textView.TextSnapshot;
            var point = trackingPoint.GetPoint(trackingPoint.TextBuffer.CurrentSnapshot);
            var viewBufferPoint = textView.BufferGraph.MapUpToSnapshot(
                point,
                trackingPoint.TrackingMode,
                PositionAffinity.Predecessor,
                targetSnapshot);

            if (viewBufferPoint == null)
            {
                return null;
            }

            return targetSnapshot.CreateTrackingPoint(
                viewBufferPoint.Value.Position,
                trackingPoint.TrackingMode);
        }

        #endregion
    }
}
