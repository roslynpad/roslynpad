using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data
{
    /// <summary>
    /// This class, returned from <see cref="IAsyncCompletionItemManager"/>,
    /// contains completion items to display in the UI, recommended item to display, selection mode and available filters.
    /// </summary>
    public sealed class FilteredCompletionModel
    {
        /// <summary>
        /// Items to display in the completion UI.
        /// </summary>
        public ImmutableArray<CompletionItemWithHighlight> Items { get; }

        /// <summary>
        /// Recommended item index to select. -1 selects suggestion item.
        /// </summary>
        public int SelectedItemIndex { get; }

        /// <summary>
        /// Completion filters with their availability and selection state.
        /// </summary>
        public ImmutableArray<CompletionFilterWithState> Filters { get; }

        /// <summary>
        /// Controls the selection mode of the selected item.
        /// </summary>
        public UpdateSelectionHint SelectionHint { get; }

        /// <summary>
        /// Whether selected item should be displayed in the center of the list. Usually, this is true
        /// </summary>
        public bool CenterSelection { get; }

        /// <summary>
        /// Optionally, provides an item that should be committed using the "commit if unique" command.
        /// </summary>
        public CompletionItem UniqueItem { get; }

        /// <summary>
        /// Constructs <see cref="FilteredCompletionModel"/> without completion filters.
        /// </summary>
        /// <param name="items">Items to display in the completion UI.</param>
        /// <param name="selectedItemIndex">Recommended item index to select. -1 selects suggestion item.</param>
        public FilteredCompletionModel(ImmutableArray<CompletionItemWithHighlight> items, int selectedItemIndex)
            : this(items, selectedItemIndex, ImmutableArray<CompletionFilterWithState>.Empty, selectionHint: UpdateSelectionHint.NoChange, centerSelection: true, uniqueItem: null)
        {
        }

        /// <summary>
        /// Constructs <see cref="FilteredCompletionModel"/> with completion filters.
        /// </summary>
        /// <param name="items">Items to display in the completion UI.</param>
        /// <param name="selectedItemIndex">Recommended item index to select. -1 selects suggestion item.</param>
        /// <param name="filters">Completion filters with their availability and selection state. Default is empty array.</param>
        public FilteredCompletionModel(ImmutableArray<CompletionItemWithHighlight> items, int selectedItemIndex, ImmutableArray<CompletionFilterWithState> filters)
            : this(items, selectedItemIndex, filters, selectionHint: UpdateSelectionHint.NoChange, centerSelection: true, uniqueItem: null)
        {
        }

        /// <summary>
        /// Constructs <see cref="FilteredCompletionModel"/> with completion filters, indication regarding selection mode and the unique item
        /// </summary>
        /// <param name="items">Items to display in the completion UI.</param>
        /// <param name="selectedItemIndex">Recommended item index to select. -1 selects suggestion item.</param>
        /// <param name="filters">Completion filters with their availability and selection state. Default is empty array.</param>
        /// <param name="selectionHint">Allows <see cref="IAsyncCompletionItemManager"/> to influence the selection mode. Default is <see cref="UpdateSelectionHint.NoChange" /></param>
        /// <param name="uniqueItem">Provides <see cref="CompletionItem"/> to commit using "commit if unique" command despite displaying more than one item. Default is <c>null</c></param>
        /// <summary>
        /// Constructs <see cref="FilteredCompletionModel"/> from a <see cref="CompletionList{T}"/> of highlighted items.
        /// </summary>
        public FilteredCompletionModel(
            CompletionList<CompletionItemWithHighlight> items,
            int selectedItemIndex,
            ImmutableArray<CompletionFilterWithState> filters,
            UpdateSelectionHint selectionHint,
            bool centerSelection,
            CompletionItem uniqueItem)
            : this(items?.ToImmutableArray() ?? throw new ArgumentNullException(nameof(items)),
                  selectedItemIndex, filters, selectionHint, centerSelection, uniqueItem)
        {
        }

        public FilteredCompletionModel(
            ImmutableArray<CompletionItemWithHighlight> items,
            int selectedItemIndex,
            ImmutableArray<CompletionFilterWithState> filters,
            UpdateSelectionHint selectionHint,
            bool centerSelection,
            CompletionItem uniqueItem)
        {
            if (selectedItemIndex < -1)
                throw new ArgumentOutOfRangeException(nameof(selectedItemIndex), "Selected index value must be greater than or equal to 0, or -1 to indicate selection of the suggestion item");
            if (items.IsDefault)
                throw new ArgumentException("Array must be initialized", nameof(items));
            if (filters.IsDefault)
                throw new ArgumentException("Array must be initialized", nameof(filters));

            Items = items;
            SelectedItemIndex = selectedItemIndex;
            Filters = filters;
            SelectionHint = selectionHint;
            CenterSelection = centerSelection;
            UniqueItem = uniqueItem;
        }
    }
}
