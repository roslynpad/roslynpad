using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data
{
    /// <summary>
    /// Stores information on computed <see cref="CompletionItem"/>s and their selection information.
    /// </summary>
    public sealed class ComputedCompletionItems
    {
        /// <summary>
        /// Constructs instance of <see cref="ComputedCompletionItems"/> with recently computed
        /// <see cref="CompletionItem"/>s and their selection infomration.
        /// </summary>
        /// <param name="items"><see cref="CompletionItem"/>s displayed in the completion UI</param>
        /// <param name="suggestionItem">Suggestion <see cref="CompletionItem"/> displayed in the UI, or null if no suggestion is displayed</param>
        /// <param name="selectedItem">Currently selected <see cref="CompletionItem"/></param>
        /// <param name="suggestionItemSelected">Whether <see cref="SelectedItem"/> is a suggestion item</param>
        /// <param name="usesSoftSelection">Whether <see cref="SelectedItem"/> is soft selected.</param>
        public ComputedCompletionItems(
            ImmutableArray<CompletionItem> items,
            CompletionItem suggestionItem,
            CompletionItem selectedItem,
            bool suggestionItemSelected,
            bool usesSoftSelection)
        {
            _itemsWithoutHighlight = items;
            SuggestionItem = suggestionItem;
            SelectedItem = selectedItem;
            SuggestionItemSelected = suggestionItemSelected;
            UsesSoftSelection = usesSoftSelection;
        }

        /// <summary>
        /// Constructs instance of <see cref="ComputedCompletionItems"/> with recently computed
        /// <see cref="CompletionItemWithHighlight"/>s and their selection infomration.
        /// </summary>
        /// <param name="itemsWithHighlight"><see cref="CompletionItemWithHighlight"/>s displayed in the completion UI</param>
        /// <param name="suggestionItem">Suggestion <see cref="CompletionItem"/> displayed in the UI, or null if no suggestion is displayed</param>
        /// <param name="selectedItem">Currently selected <see cref="CompletionItem"/></param>
        /// <param name="suggestionItemSelected">Whether <see cref="SelectedItem"/> is a suggestion item</param>
        /// <param name="usesSoftSelection">Whether <see cref="SelectedItem"/> is soft selected.</param>
        public ComputedCompletionItems(
            ImmutableArray<CompletionItemWithHighlight> itemsWithHighlight,
            CompletionItem suggestionItem,
            CompletionItem selectedItem,
            bool suggestionItemSelected,
            bool usesSoftSelection)
        {
            _itemsWithHighlight = itemsWithHighlight;
            SuggestionItem = suggestionItem;
            SelectedItem = selectedItem;
            SuggestionItemSelected = suggestionItemSelected;
            UsesSoftSelection = usesSoftSelection;
        }

        /// <summary>
        /// Empty data structure, used when no computation was performed
        /// </summary>
        public static ComputedCompletionItems Empty { get; } = new ComputedCompletionItems(ImmutableArray<CompletionItem>.Empty, null, null, false, false);

        private ImmutableArray<CompletionItem> _itemsWithoutHighlight;
        private ImmutableArray<CompletionItemWithHighlight> _itemsWithHighlight;

        private IEnumerable<CompletionItem> _computedItems = null;

        /// <summary>
        /// <see cref="CompletionItem"/>s displayed in the completion UI
        /// </summary>
        public IEnumerable<CompletionItem> Items
        {
            get
            {
                if (_computedItems == null)
                {
                    // We were constructed with either items or itemsWithHighlight
                    if (_itemsWithoutHighlight != null)
                    {
                        _computedItems = _itemsWithoutHighlight.IsDefault
                            ? Enumerable.Empty<CompletionItem>()
                            : _itemsWithoutHighlight;
                    }
                    else
                    {
                        if (_itemsWithHighlight.IsDefault)
                        {
                            _computedItems = Enumerable.Empty<CompletionItem>();
                        }
                        else
                        {
                            var items = new List<CompletionItem>(_itemsWithHighlight.Length);
                            for (int i = 0; i < _itemsWithHighlight.Length; i++)
                            {
                                items.Add(_itemsWithHighlight[i].CompletionItem);
                            }
                            _computedItems = items;
                        }
                    }
                }
                return _computedItems;
            }
        }

        /// <summary>
        /// Suggestion <see cref="CompletionItem"/> displayed in the UI, or null if no suggestion is displayed
        /// </summary>
        public CompletionItem SuggestionItem { get; }

        /// <summary>
        /// Currently selected <see cref="CompletionItem"/>
        /// </summary>
        public CompletionItem SelectedItem { get; }

        /// <summary>
        /// Whether <see cref="SelectedItem"/> is a suggestion item
        /// </summary>
        public bool SuggestionItemSelected { get; }

        /// <summary>
        /// Whether <see cref="SelectedItem"/> is soft selected.
        /// </summary>
        public bool UsesSoftSelection { get; }
    }
}
