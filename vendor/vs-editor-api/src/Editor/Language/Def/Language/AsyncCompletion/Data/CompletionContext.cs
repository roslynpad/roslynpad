using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data
{
    /// <summary>
    /// This type is used to transfer data from <see cref="IAsyncCompletionSource"/>
    /// to <see cref="IAsyncCompletionBroker"/> and further to <see cref="IAsyncCompletionItemManager"/>
    /// </summary>
    [DebuggerDisplay("{Items.Length} items")]
    public sealed class CompletionContext
    {
        /// <summary>
        /// Empty completion context, when <see cref="IAsyncCompletionSource"/> offers no items pertinent to given location.
        /// </summary>
        public static CompletionContext Empty { get; } = new CompletionContext(ImmutableArray<CompletionItem>.Empty, ImmutableArray<CompletionFilterWithState>.Empty);

        /// <summary>
        /// Set of completion items available at a location
        /// </summary>
        public ImmutableArray<CompletionItem> Items { get; }

        /// <summary>
        /// Set of completion items available at a location, as a list. Equivalent to <see cref="Items"/>.
        /// </summary>
        public IReadOnlyList<CompletionItem> ItemList => Items;

        /// <summary>
        /// Indicates that this context contains an incomplete set of items and completion
        /// should be re-queried as the user continues typing (LSP-style incomplete results).
        /// </summary>
        public bool IsIncomplete { get; }

        /// <summary>
        /// Additional properties attached to this context, if any.
        /// </summary>
        public PropertyCollection Properties { get; }

        /// <summary>
        /// <para>
        /// Set of completion filters available for this session.
        /// Each filter's <see cref="CompletionFilterWithState.IsSelected"/> property is used to determine initial selection.
        /// The <see cref="CompletionFilterWithState.IsAvailable"/> property is ignored.
        /// </para>
        /// <para>
        /// Typically, this is used to select <see cref="CompletionExpander"/>s that correspond to provided <see cref="CompletionItem"/>s,
        /// in scenarios when the completion source provides expanded items by default.
        /// </para>
        /// </summary>
        /// <remarks>
        /// When the value is uninitialized, then <see cref="Items"/> need to be enumerated to find the filters.
        /// </remarks>
        public ImmutableArray<CompletionFilterWithState> Filters { get; }

        /// <summary>
        /// Recommends the initial selection method for the completion list.
        /// When <see cref="SuggestionItemOptions"/> is defined, "soft selection" will be used without a need to set this property.
        /// </summary>
        public InitialSelectionHint SelectionHint { get; }

        /// <summary>
        /// When defined, uses suggestion mode with options specified in this object.
        /// When null, this context does not activate the suggestion mode.
        /// Suggestion mode puts selection in "soft selection" mode without need to set <see cref="SelectionHint"/>
        /// </summary>
        public SuggestionItemOptions SuggestionItemOptions { get; }

        /// <summary>
        /// [Deprecated] Constructs <see cref="CompletionContext"/> with specified <see cref="CompletionItem"/>s,
        /// with recommendation to not use suggestion mode and to use use regular selection.
        /// Note: completion will iterate through all items to determine filters.
        /// For better performance, use the overload which accepts <see cref="ImmutableArray{CompletionFilterWithState}"/>
        /// </summary>
        /// <param name="items">Available completion items. If none are available, use <c>CompletionContext.Default</c></param>
        public CompletionContext(ImmutableArray<CompletionItem> items)
            : this(items,
                suggestionItemOptions: null,
                selectionHint: InitialSelectionHint.RegularSelection,
                filters: default)
        {
        }

        /// <summary>
        /// Constructs <see cref="CompletionContext"/> with specified <see cref="CompletionItem"/>s and <see cref="CompletionFilterWithState"/>s
        /// with recommendation to not use suggestion mode and to use use regular selection.
        /// </summary>
        /// <param name="items">Available completion items. If none are available, use <c>CompletionContext.Default</c></param>
        /// <param name="filters">Available completion filters. Each filter's <see cref="CompletionFilterWithState.IsSelected"/> property is used to determine initial selection.
        /// The <see cref="CompletionFilterWithState.IsAvailable"/> property is ignored.</param>
        public CompletionContext(ImmutableArray<CompletionItem> items, ImmutableArray<CompletionFilterWithState> filters)
            : this(items,
                suggestionItemOptions: null,
                selectionHint: InitialSelectionHint.RegularSelection,
                filters: filters)
        {
        }

        /// <summary>
        /// Constructs <see cref="CompletionContext"/> with specified <see cref="CompletionItem"/>s,
        /// with recommendation to use suggestion mode and to use regular selection.
        /// </summary>
        /// <param name="items">Available completion items</param>
        /// <param name="suggestionItemOptions">Suggestion item options, or null to not use suggestion mode. Default is <c>null</c></param>
        public CompletionContext(
            ImmutableArray<CompletionItem> items,
            SuggestionItemOptions suggestionItemOptions)
            : this(items,
                suggestionItemOptions,
                selectionHint: InitialSelectionHint.RegularSelection,
                filters: default)
        {
        }

        /// <summary>
        /// Constructs <see cref="CompletionContext"/> with specified <see cref="CompletionItem"/>s,
        /// with recommendation to use suggestion mode item and to use a specific selection mode.
        /// </summary>
        /// <param name="items">Available completion items</param>
        /// <param name="suggestionItemOptions">Suggestion mode options, or null to not use suggestion mode. Default is <c>null</c></param>
        /// <param name="selectionHint">Recommended selection mode. Suggestion mode automatically sets soft selection Default is <c>InitialSelectionHint.RegularSelection</c></param>
        public CompletionContext(
            ImmutableArray<CompletionItem> items,
            SuggestionItemOptions suggestionItemOptions,
            InitialSelectionHint selectionHint)
        : this (items,
              suggestionItemOptions,
              selectionHint,
              filters: default)
        { }

        /// <summary>
        /// Constructs <see cref="CompletionContext"/> with specified <see cref="CompletionItem"/>s,
        /// with recommendation to use suggestion mode item and to use a specific selection mode.
        /// </summary>
        /// <param name="items">Available completion items</param>
        /// <param name="suggestionItemOptions">Suggestion mode options, or null to not use suggestion mode. Default is <c>null</c></param>
        /// <param name="selectionHint">Recommended selection mode. Suggestion mode automatically sets soft selection Default is <c>InitialSelectionHint.RegularSelection</c></param>
        /// <param name="filters">Available completion filters. Each filter's <see cref="CompletionFilterWithState.IsSelected"/> property is used to determine initial selection.
        /// The <see cref="CompletionFilterWithState.IsAvailable"/> property is ignored.</param>
        public CompletionContext(
            ImmutableArray<CompletionItem> items,
            SuggestionItemOptions suggestionItemOptions,
            InitialSelectionHint selectionHint,
            ImmutableArray<CompletionFilterWithState> filters)
        {
            if (items.IsDefault)
                throw new ArgumentException("Array must be initialized", nameof(items));
            Items = items;
            SelectionHint = selectionHint;
            SuggestionItemOptions = suggestionItemOptions;
            Filters = filters;
        }

        /// <summary>
        /// Constructs <see cref="CompletionContext"/> from a <see cref="CompletionList{T}"/> of items,
        /// optionally marking the result set as incomplete.
        /// </summary>
        /// <param name="itemList">Available completion items</param>
        /// <param name="suggestionItemOptions">Suggestion mode options, or null to not use suggestion mode</param>
        /// <param name="selectionHint">Recommended selection mode</param>
        /// <param name="filters">Available completion filters</param>
        /// <param name="isIncomplete">Whether this item set is incomplete and should be re-queried while typing</param>
        /// <param name="properties">Additional properties attached to this context, or null</param>
        public CompletionContext(
            CompletionList<CompletionItem> itemList,
            SuggestionItemOptions suggestionItemOptions,
            InitialSelectionHint selectionHint,
            ImmutableArray<CompletionFilterWithState> filters,
            bool isIncomplete,
            PropertyCollection properties)
        {
            if (itemList == null)
                throw new ArgumentNullException(nameof(itemList));
            Items = itemList.ToImmutableArray();
            SelectionHint = selectionHint;
            SuggestionItemOptions = suggestionItemOptions;
            Filters = filters;
            IsIncomplete = isIncomplete;
            Properties = properties;
        }
    }
}
