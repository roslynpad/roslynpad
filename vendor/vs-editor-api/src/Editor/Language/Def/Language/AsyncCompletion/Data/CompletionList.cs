using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data
{
    /// <summary>
    /// An immutable list of completion items, produced by <see cref="IAsyncCompletionItemManager2"/>
    /// implementations via <see cref="AsyncCompletionSessionExtensions.CreateCompletionList{T}"/>.
    /// </summary>
    public sealed class CompletionList<T> : IReadOnlyList<T>
    {
        private readonly ImmutableArray<T> _items;

        public CompletionList(IEnumerable<T> items)
        {
            _items = items.ToImmutableArray();
        }

        internal CompletionList(ImmutableArray<T> items)
        {
            _items = items;
        }

        public T this[int index] => _items[index];

        public int Count => _items.Length;

        /// <summary>
        /// Returns the underlying immutable array without copying.
        /// </summary>
        public ImmutableArray<T> ToImmutableArray() => _items;

        public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)_items).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
