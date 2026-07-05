using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data
{
    /// <summary>
    /// This type is used to fetch completion data from available <see cref="IAsyncCompletionSource"/>s
    /// without sorting, filtering and displaying the results in the GUI.
    /// </summary>
    [DebuggerDisplay("{CompletionContext.Items.Length} items")]
    public sealed class AggregatedCompletionContext
    {
        /// <summary>
        /// Aggregate <see cref="CompletionContext"/>
        /// </summary>
        public CompletionContext CompletionContext { get; }

        /// <summary>
        /// <see cref="IAsyncCompletionSession"/> which interacted with <see cref="IAsyncCompletionSource"/>s.
        /// This session can not be retrieved from <see cref="IAsyncCompletionBroker"/>.
        /// This session does not have full capability, and its purpose is to provide data in <see cref="IAsyncCompletionSource.GetDescriptionAsync(IAsyncCompletionSession, CompletionItem, System.Threading.CancellationToken)"/>.
        /// This session must be dismissed when client no longer needs it.
        /// </summary>
        public IAsyncCompletionSession InertSession { get; }

        /// <summary>
        /// Creates <see cref="AggregatedCompletionContext"/> which carries data aggregated from
        /// <see cref="CompletionContext"/>s returned by the <see cref="IAsyncCompletionSource"/>s.
        /// This object also has a reference to the <see cref="IAsyncCompletionSession"/> which queried the <see cref="IAsyncCompletionSource"/>s.
        /// </summary>
        /// <param name="completionContext">Aggregate <see cref="CompletionContext"/></param>
        /// <param name="inertSession"><see cref="IAsyncCompletionSession"/> which interacted with <see cref="IAsyncCompletionSource"/>s.</param>
        public AggregatedCompletionContext(CompletionContext completionContext, IAsyncCompletionSession inertSession)
        {
            CompletionContext = completionContext ?? throw new ArgumentNullException(nameof(completionContext));
            InertSession = inertSession ?? throw new ArgumentNullException(nameof(inertSession));
        }

        /// <summary>
        /// Creates empty <see cref="AggregatedCompletionContext"/>
        /// </summary>
        private AggregatedCompletionContext()
        {
            CompletionContext = CompletionContext.Empty;
            InertSession = null;
        }

        /// <summary>
        /// Empty headless completion context, used when obtaining completion data was unsuccessful.
        /// </summary>
        public static AggregatedCompletionContext Empty { get; } = new AggregatedCompletionContext();
    }
}
