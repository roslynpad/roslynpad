namespace Microsoft.VisualStudio.Language.Intellisense.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.Language.Intellisense;
    using Microsoft.VisualStudio.Language.Utilities;
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Adornments;
    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.Threading;
    using Microsoft.VisualStudio.Utilities;

    internal sealed class AsyncQuickInfoPresentationSession : AsyncQuickInfoSession,
        IAsyncQuickInfoSession2
    {
        private readonly IGuardedOperations guardedOperations;
        private readonly IToolTipService toolTipService;

        #region Reable/Writeable via UI Thread Only

        IToolTipPresenter uiThreadOnlyPresenter;
        IToolTipPresenter2 uiThreadOnlyPresenterV2;
        internal IToolTipPresenter UIThreadOnlyPresenter
        {
            get => uiThreadOnlyPresenter;
            set {
                uiThreadOnlyPresenter = value;
                uiThreadOnlyPresenterV2 = value as IToolTipPresenter2;
            }
        }

        #endregion

        public AsyncQuickInfoPresentationSession(
            IEnumerable<Lazy<IAsyncQuickInfoSourceProvider, Microsoft.VisualStudio.Text.Utilities.OrderableContentTypeMetadata>> orderedSourceProviders,
            IGuardedOperations guardedOperations,
            JoinableTaskContext joinableTaskContext,
            IToolTipService toolTipService,
            ITextView textView,
            ITrackingPoint triggerPoint,
            QuickInfoSessionOptions options,
            PropertyCollection propertyCollection) : base(
                orderedSourceProviders,
                joinableTaskContext,
                textView,
                triggerPoint,
                options,
                propertyCollection)
        {
            this.guardedOperations = guardedOperations ?? throw new ArgumentNullException(nameof(guardedOperations));
            this.toolTipService = toolTipService ?? throw new ArgumentNullException(nameof(toolTipService));
        }

        public override bool IsMouseOverAggregated
            => uiThreadOnlyPresenterV2 != null && uiThreadOnlyPresenterV2.IsMouseOverAggregated;

        public override async Task DismissAsync()
        {
            // Ensure that we have the UI thread. To avoid races, the rest of this method must be sync.
            await this.JoinableTaskContext.Factory.SwitchToMainThreadAsync();

            var currentState = this.State;
            if (currentState != QuickInfoSessionState.Dismissed)
            {
                // Dismiss presenter.
                var presenter = this.UIThreadOnlyPresenter;
                if (presenter != null)
                {
                    presenter.Dismissed -= this.OnDismissed;
                    UIThreadOnlyPresenter.Dismiss();
                    UIThreadOnlyPresenter = null;
                }
            }

            await base.DismissAsync().ConfigureAwait(false);
        }

        internal override async Task UpdateAsync(bool allowUpdate, CancellationToken cancellationToken)
        {
            // Ensure we have the UI thread.
            await this.JoinableTaskContext.Factory.SwitchToMainThreadAsync();

            try
            {
                await base.UpdateAsync(allowUpdate, cancellationToken).ConfigureAwait(true);
                await this.UpdatePresenterAsync().ConfigureAwait(false);
            }
            catch (AggregateException ex)
            {
                // Catch all exceptions and post them here on the UI thread.
                Debug.Assert(this.JoinableTaskContext.IsOnMainThread);
                this.guardedOperations.HandleException(this, ex);
            }
        }

        private void OnDismissed(object sender, EventArgs e)
        {
            IntellisenseUtilities.ThrowIfNotOnMainThread(this.JoinableTaskContext);

            this.JoinableTaskContext.Factory.RunAsync(async delegate
            {
                await this.DismissAsync().ConfigureAwait(false);
            });
        }

        private async Task UpdatePresenterAsync()
        {
            await this.JoinableTaskContext.Factory.SwitchToMainThreadAsync();

            // Ensure that the session wasn't dismissed.
            if (this.State == QuickInfoSessionState.Dismissed)
            {
                return;
            }

            // Configure presenter behavior.
            var parameters = new ToolTipParameters(
                this.Options.HasFlag(QuickInfoSessionOptions.TrackMouse),
                keepOpenFunc: this.ContentRequestsKeepOpen);

            // Create presenter if necessary.
            if (UIThreadOnlyPresenter == null)
            {
                UIThreadOnlyPresenter = this.toolTipService.CreatePresenter(this.TextView, parameters);
                UIThreadOnlyPresenter.Dismissed += this.OnDismissed;
            }

            // Update presenter content.
            UIThreadOnlyPresenter.StartOrUpdate(this.ApplicableToSpan, this.Content);

            // Ensure that the presenter didn't dismiss the session.
            if (this.State != QuickInfoSessionState.Dismissed)
            {
                // Update state and alert subscribers on the UI thread.
                this.TransitionTo(QuickInfoSessionState.Visible);
            }
        }

        private bool ContentRequestsKeepOpen()
        {
            IntellisenseUtilities.ThrowIfNotOnMainThread(this.JoinableTaskContext);

            if (this.HasInteractiveContent)
            {
                foreach (var content in this.Content)
                {
                    if ((content is IInteractiveQuickInfoContent interactiveContent)
                        && ((interactiveContent.KeepQuickInfoOpen || interactiveContent.IsMouseOverAggregated)))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
