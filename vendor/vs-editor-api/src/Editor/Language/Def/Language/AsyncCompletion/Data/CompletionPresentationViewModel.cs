using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data
{
    /// <summary>
    /// This class contains completion items, filters and other pieces of information
    /// used by <see cref="ICompletionPresenter"/> to render the completion UI.
    /// </summary>
    public sealed class CompletionPresentationViewModel
    {
        /// <summary>
        /// Completion items to display with their highlighted spans.
        /// </summary>
        public ImmutableArray<CompletionItemWithHighlight> Items { get; }

        /// <summary>
        /// Completion filters with their available and selected state.
        /// </summary>
        public ImmutableArray<CompletionFilterWithState> Filters { get; }

        /// <summary>
        /// Span pertinent to the completion session.
        /// </summary>
        public ITrackingSpan ApplicableToSpan { get; }

        /// <summary>
        /// Controls whether selected item should be soft selected.
        /// </summary>
        public bool UseSoftSelection { get; }

        /// <summary>
        /// Controls whether suggestion item is visible.
        /// </summary>
        public bool DisplaySuggestionItem { get; }

        /// <summary>
        /// Controls whether suggestion item is selected.
        /// </summary>
        public bool SelectSuggestionItem { get; }

        /// <summary>
        /// Controls which item is selected. Use -1 in suggestion mode.
        /// </summary>
        public int SelectedItemIndex { get; }

        /// <summary>
        /// Suggestion item to display when <see cref="DisplaySuggestionItem"/> is set.
        /// </summary>
        public CompletionItem SuggestionItem { get; }

        /// <summary>
        /// How to display the <see cref="SuggestionItem"/>.
        /// </summary>
        public SuggestionItemOptions SuggestionItemOptions { get; }

        /// <summary>
        /// Constructs <see cref="CompletionPresentationViewModel"/>
        /// </summary>
        /// <param name="items">Completion items to display with their highlighted spans</param>
        /// <param name="filters">Completion filters with their available and selected state</param>
        /// <param name="selectedItemIndex">Controls which item is selected. Use -1 in suggestion mode</param>
        /// <param name="applicableToSpan">Span pertinent to the completion session</param>
        /// <param name="useSoftSelection">Controls whether selected item should be soft selected. Default is <c>false</c></param>
        /// <param name="displaySuggestionItem">Controls whether suggestion mode item is visible. Default is <c>false</c></param>
        /// <param name="selectSuggestionItem">Controls whether suggestion mode item is selected. Default is <c>false</c></param>
        /// <param name="suggestionItem">Suggestion mode item to display. Default is <c>null</c></param>
        /// <param name="suggestionItemOptions">How to present the suggestion mode item. This is required because completion may be in suggestion mode even if there is no explicit suggestion mode item</param>
        public CompletionPresentationViewModel(
            ImmutableArray<CompletionItemWithHighlight> items,
            ImmutableArray<CompletionFilterWithState> filters,
            int selectedItemIndex,
            ITrackingSpan applicableToSpan,
            bool useSoftSelection,
            bool displaySuggestionItem,
            bool selectSuggestionItem,
            CompletionItem suggestionItem,
            SuggestionItemOptions suggestionItemOptions)
        {
            if (selectedItemIndex < -1)
                throw new ArgumentOutOfRangeException(nameof(selectedItemIndex), "Selected index value must be greater than or equal to 0, or -1 to indicate no selection");
            if (items.IsDefault)
                throw new ArgumentException("Array must be initialized", nameof(items));
            if (filters.IsDefault)
                throw new ArgumentException("Array must be initialized", nameof(filters));

            Items = items;
            Filters = filters;
            ApplicableToSpan = applicableToSpan ?? throw new NullReferenceException(nameof(applicableToSpan));
            UseSoftSelection = useSoftSelection;
            DisplaySuggestionItem = displaySuggestionItem;
            SelectSuggestionItem = selectSuggestionItem;
            SelectedItemIndex = selectedItemIndex;
            SuggestionItem = suggestionItem;
            SuggestionItemOptions = suggestionItemOptions ?? throw new NullReferenceException(nameof(suggestionItemOptions));
        }
    }
}
