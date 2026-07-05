namespace Microsoft.VisualStudio.Language.Intellisense
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using Microsoft.VisualStudio.Text;

    /// <summary>
    /// An immutable collection of Quick Info items and the span to which they are applicable.
    /// </summary>
    public sealed class QuickInfoItemsCollection
    {
        /// <summary>
        /// The collection of Quick Info items.
        /// </summary>
        public IEnumerable<object> Items { get; }

        /// <summary>
        /// The span to which the Quick Info items apply.
        /// </summary>
        public ITrackingSpan ApplicableToSpan { get; }

        /// <summary>
        /// Creates a new <see cref="QuickInfoItemsCollection"/>.
        /// </summary>
        /// <param name="items">The Quick Info items.</param>
        /// <param name="applicableToSpan">The span to which the items are applicable.</param>
        public QuickInfoItemsCollection(IEnumerable<object> items, ITrackingSpan applicableToSpan)
        {
            this.Items = items.ToImmutableList() ?? throw new ArgumentNullException(nameof(items));
            this.ApplicableToSpan = applicableToSpan ?? throw new ArgumentNullException(nameof(applicableToSpan));
        }
    }
}

