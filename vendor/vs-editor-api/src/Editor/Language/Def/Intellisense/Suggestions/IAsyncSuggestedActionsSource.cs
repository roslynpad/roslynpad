// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Language.Intellisense
{
    using System;
    using System.Collections.Immutable;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.Text;

    /// <summary>
    /// A collector that receives <see cref="SuggestedActionSet"/>s for a particular priority class
    /// as they are computed.
    /// </summary>
    [CLSCompliant(false)]
    public interface ISuggestedActionSetCollector
    {
        /// <summary>
        /// The priority ordering this collector gathers action sets for (see
        /// <see cref="SuggestedActionPriorityAttribute"/> and <see cref="Utilities.DefaultOrderings"/>).
        /// </summary>
        string Priority { get; }

        /// <summary>
        /// Adds a computed action set to this collector.
        /// </summary>
        void Add(SuggestedActionSet set);

        /// <summary>
        /// Signals that no more sets will be added to this collector.
        /// </summary>
        void Complete();
    }

    /// <summary>
    /// An <see cref="ISuggestedActionsSource"/> that computes actions asynchronously, streaming
    /// results into per-priority <see cref="ISuggestedActionSetCollector"/>s.
    /// </summary>
    [CLSCompliant(false)]
    public interface IAsyncSuggestedActionsSource : ISuggestedActionsSource
    {
        /// <summary>
        /// Computes suggested actions for the given span, adding them to the provided collectors
        /// (ordered from highest to lowest priority).
        /// </summary>
        Task GetSuggestedActionsAsync(
            ISuggestedActionCategorySet requestedActionCategories,
            SnapshotSpan range,
            ImmutableArray<ISuggestedActionSetCollector> collectors,
            CancellationToken cancellationToken);
    }
}
