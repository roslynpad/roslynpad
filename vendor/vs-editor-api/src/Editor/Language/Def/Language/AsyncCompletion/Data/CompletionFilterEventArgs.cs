using System;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data
{
    /// <summary>
    /// This class is used to notify completion's logic of selection change in the filter UI
    /// </summary>
    public sealed class CompletionFilterChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Current state of the filters
        /// </summary>
        public ImmutableArray<CompletionFilterWithState> Filters { get; }

        /// <summary>
        /// Constructs instance of <see cref="CompletionFilterChangedEventArgs"/>.
        /// </summary>
        /// <param name="filters">Current state of the filters</param>
        public CompletionFilterChangedEventArgs(
            ImmutableArray<CompletionFilterWithState> filters)
        {
            if (filters.IsDefault)
                throw new ArgumentException("Array must be initialized", nameof(filters));
            this.Filters = filters;
        }
    }
}
