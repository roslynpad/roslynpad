using System;
using System.Collections.Immutable;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Implementation
{
    /// <summary>
    /// Represents an immutable snapshot of state of the async completion feature.
    /// </summary>
    internal sealed class CompletionModel
    {
        /// <summary>
        /// All items, as provided by completion item sources.
        /// </summary>
        public readonly ImmutableArray<CompletionItem> InitialItems;

        /// <summary>
        /// Sorted array of all items, as provided by the completion service.
        /// </summary>
        public readonly ImmutableArray<CompletionItem> SortedItems;

        /// <summary>
        /// Snapshot pertinent to this completion model.
        /// </summary>
        public readonly ITextSnapshot Snapshot;

        /// <summary>
        /// Filters involved in this completion model, including their availability and selection state.
        /// </summary>
        public readonly ImmutableArray<CompletionFilterWithState> Filters;

        /// <summary>
        /// Invoked expansions involved in this completion model.
        /// </summary>
        public readonly ImmutableArray<CompletionExpander> PrimedExpanders;

        /// <summary>
        /// Items to be displayed in the UI.
        /// </summary>
        public readonly ImmutableArray<CompletionItemWithHighlight> PresentedItems;

        /// <summary>
        /// Index of item to select. Use -1 to select nothing, when suggestion mode item should be selected.
        /// </summary>
        public readonly int SelectedIndex;

        /// <summary>
        /// Whether selection should be displayed as soft selection.
        /// </summary>
        public readonly bool UseSoftSelection;

        /// <summary>
        /// Whether suggestion mode item should be visible.
        /// </summary>
        public readonly bool DisplaySuggestionItem;

        /// <summary>
        /// Whether suggestion mode item should be selected.
        /// </summary>
        public readonly bool SelectSuggestionItem;

        /// <summary>
        /// <see cref="CompletionItem"/> which contains user-entered text.
        /// Used to display and commit the suggestion mode item
        /// </summary>
        public readonly CompletionItem SuggestionItem;

        /// <summary>
        /// <see cref="CompletionItem"/> which overrides regular unique item selection.
        /// When this is null, the single item from <see cref="PresentedItems"/> is used as unique item.
        /// </summary>
        public readonly CompletionItem UniqueItem;

        /// <summary>
        /// This flags prevents <see cref="IAsyncCompletionSession"/> from dismissing when it initially becomes empty.
        /// We dismiss when this flag is set (span is empty) and user attempts to remove characters.
        /// </summary>
        public readonly bool ApplicableToSpanWasEmpty;

        /// <summary>
        /// Indicates the state where the model received no items from the completion sources.
        /// We keep the session (and its model) around to attempt getting items at the next keystroke, while preventing race conditions. 
        /// </summary>
        public readonly bool Uninitialized;

        /// <summary>
        /// Creates an instance of <see cref="CompletionModel"/> for session
        /// that did not receive any <see cref="CompletionItem"/>s yet, but may receive them soon thereafter.
        /// </summary>
        public static CompletionModel GetUninitializedModel(ITextSnapshot snapshot)
        {
            return new CompletionModel(default, default, snapshot, default, default, default, default, default, default, default, default, default, default, uninitialized: true);
        }

        /// <summary>
        /// Constructor for the initial model
        /// </summary>
        public CompletionModel(ImmutableArray<CompletionItem> initialItems, ImmutableArray<CompletionItem> sortedItems,
            ITextSnapshot snapshot, ImmutableArray<CompletionFilterWithState> filters, ImmutableArray<CompletionExpander> primedExpanders, bool useSoftSelection,
            bool displaySuggestionItem, bool selectSuggestionItem, CompletionItem suggestionItem)
        {
            InitialItems = initialItems;
            SortedItems = sortedItems;
            Snapshot = snapshot;
            Filters = filters;
            PrimedExpanders = primedExpanders;
            SelectedIndex = 0;
            UseSoftSelection = useSoftSelection;
            DisplaySuggestionItem = displaySuggestionItem;
            SelectSuggestionItem = selectSuggestionItem;
            SuggestionItem = suggestionItem;
            UniqueItem = null;
            ApplicableToSpanWasEmpty = false;
            Uninitialized = false;
        }

        /// <summary>
        /// Private constructor for the With* methods
        /// </summary>
        private CompletionModel(ImmutableArray<CompletionItem> initialItems, ImmutableArray<CompletionItem> sortedItems,
            ITextSnapshot snapshot, ImmutableArray<CompletionFilterWithState> filters, ImmutableArray<CompletionExpander> primedExpanders, ImmutableArray<CompletionItemWithHighlight> presentedItems,
            bool useSoftSelection, bool displaySuggestionItem, int selectedIndex, bool selectSuggestionItem, CompletionItem suggestionItem,
            CompletionItem uniqueItem, bool applicableToSpanWasEmpty, bool uninitialized)
        {
            InitialItems = initialItems;
            SortedItems = sortedItems;
            Snapshot = snapshot;
            Filters = filters;
            PrimedExpanders = primedExpanders;
            PresentedItems = presentedItems;
            SelectedIndex = selectedIndex;
            UseSoftSelection = useSoftSelection;
            DisplaySuggestionItem = displaySuggestionItem;
            SelectSuggestionItem = selectSuggestionItem;
            SuggestionItem = suggestionItem;
            UniqueItem = uniqueItem;
            ApplicableToSpanWasEmpty = applicableToSpanWasEmpty;
            Uninitialized = uninitialized;
        }

        public CompletionModel WithPresentedItems(ImmutableArray<CompletionItemWithHighlight> newPresentedItems, int newSelectedIndex)
        {
            return new CompletionModel(
                initialItems: InitialItems,
                sortedItems: SortedItems,
                snapshot: Snapshot,
                filters: Filters,
                primedExpanders: PrimedExpanders,
                presentedItems: newPresentedItems, // Updated
                useSoftSelection: UseSoftSelection,
                displaySuggestionItem: DisplaySuggestionItem,
                selectedIndex: newSelectedIndex, // Updated
                selectSuggestionItem: SelectSuggestionItem,
                suggestionItem: SuggestionItem,
                uniqueItem: UniqueItem,
                applicableToSpanWasEmpty: ApplicableToSpanWasEmpty,
                uninitialized: Uninitialized
            );
        }

        public CompletionModel WithSnapshot(ITextSnapshot newSnapshot)
        {
            return new CompletionModel(
                initialItems: InitialItems,
                sortedItems: SortedItems,
                snapshot: newSnapshot, // Updated
                filters: Filters,
                primedExpanders: PrimedExpanders,
                presentedItems: PresentedItems,
                useSoftSelection: UseSoftSelection,
                displaySuggestionItem: DisplaySuggestionItem,
                selectedIndex: SelectedIndex,
                selectSuggestionItem: SelectSuggestionItem,
                suggestionItem: SuggestionItem,
                uniqueItem: UniqueItem,
                applicableToSpanWasEmpty: ApplicableToSpanWasEmpty,
                uninitialized: Uninitialized
            );
        }

        public CompletionModel WithFilters(ImmutableArray<CompletionFilterWithState> newFilters)
        {
            return new CompletionModel(
                initialItems: InitialItems,
                sortedItems: SortedItems,
                snapshot: Snapshot,
                filters: newFilters, // Updated
                primedExpanders: PrimedExpanders,
                presentedItems: PresentedItems,
                useSoftSelection: UseSoftSelection,
                displaySuggestionItem: DisplaySuggestionItem,
                selectedIndex: SelectedIndex,
                selectSuggestionItem: SelectSuggestionItem,
                suggestionItem: SuggestionItem,
                uniqueItem: UniqueItem,
                applicableToSpanWasEmpty: ApplicableToSpanWasEmpty,
                uninitialized: Uninitialized
            );
        }

        public CompletionModel WithSelectedIndex(int newIndex, bool preserveSoftSelection = false)
        {
            return new CompletionModel(
                initialItems: InitialItems,
                sortedItems: SortedItems,
                snapshot: Snapshot,
                filters: Filters,
                primedExpanders: PrimedExpanders,
                presentedItems: PresentedItems,
                useSoftSelection: preserveSoftSelection ? UseSoftSelection : false, // Updated conditionally
                displaySuggestionItem: DisplaySuggestionItem,
                selectedIndex: newIndex, // Updated
                selectSuggestionItem: false, // Explicit selection of regular item
                suggestionItem: SuggestionItem,
                uniqueItem: UniqueItem,
                applicableToSpanWasEmpty: ApplicableToSpanWasEmpty,
                uninitialized: Uninitialized
            );
        }

        public CompletionModel WithSuggestionItemSelected()
        {
            return new CompletionModel(
                initialItems: InitialItems,
                sortedItems: SortedItems,
                snapshot: Snapshot,
                filters: Filters,
                primedExpanders: PrimedExpanders,
                presentedItems: PresentedItems,
                useSoftSelection: false, // Explicit selection and soft selection are mutually exclusive
                displaySuggestionItem: DisplaySuggestionItem,
                selectedIndex: -1, // Deselect regular item
                selectSuggestionItem: true, // Explicit selection of suggestion item
                suggestionItem: SuggestionItem,
                uniqueItem: UniqueItem,
                applicableToSpanWasEmpty: ApplicableToSpanWasEmpty,
                uninitialized: Uninitialized
            );
        }

        public CompletionModel WithSuggestionItemVisibility(bool newDisplaySuggestionItem)
        {
            return new CompletionModel(
                initialItems: InitialItems,
                sortedItems: SortedItems,
                snapshot: Snapshot,
                filters: Filters,
                primedExpanders: PrimedExpanders,
                presentedItems: PresentedItems,
                useSoftSelection: UseSoftSelection | newDisplaySuggestionItem, // Enabling suggestion mode also enables soft selection
                displaySuggestionItem: newDisplaySuggestionItem, // Updated
                selectedIndex: SelectedIndex,
                selectSuggestionItem: SelectSuggestionItem,
                suggestionItem: SuggestionItem,
                uniqueItem: UniqueItem,
                applicableToSpanWasEmpty: ApplicableToSpanWasEmpty,
                uninitialized: Uninitialized
            );
        }

        /// <summary>
        /// </summary>
        /// <param name="newUniqueItem">Overrides typical unique item selection.
        /// Pass in null to use regular behavior: treating single <see cref="PresentedItems"/> item as the unique item.</param>
        internal CompletionModel WithUniqueItem(CompletionItem newUniqueItem)
        {
            return new CompletionModel(
                initialItems: InitialItems,
                sortedItems: SortedItems,
                snapshot: Snapshot,
                filters: Filters,
                primedExpanders: PrimedExpanders,
                presentedItems: PresentedItems,
                useSoftSelection: UseSoftSelection,
                displaySuggestionItem: DisplaySuggestionItem,
                selectedIndex: SelectedIndex,
                selectSuggestionItem: SelectSuggestionItem,
                suggestionItem: SuggestionItem,
                uniqueItem: newUniqueItem,
                applicableToSpanWasEmpty: ApplicableToSpanWasEmpty,
                uninitialized: Uninitialized
            );
        }

        internal CompletionModel WithSoftSelection(bool newSoftSelection)
        {
            return new CompletionModel(
                initialItems: InitialItems,
                sortedItems: SortedItems,
                snapshot: Snapshot,
                filters: Filters,
                primedExpanders: PrimedExpanders,
                presentedItems: PresentedItems,
                useSoftSelection: newSoftSelection, // Updated
                displaySuggestionItem: DisplaySuggestionItem,
                selectedIndex: SelectedIndex,
                selectSuggestionItem: SelectSuggestionItem,
                suggestionItem: SuggestionItem,
                uniqueItem: UniqueItem,
                applicableToSpanWasEmpty: ApplicableToSpanWasEmpty,
                uninitialized: Uninitialized
            );
        }

        internal CompletionModel WithSnapshotItemsAndFilters(ITextSnapshot snapshot, ImmutableArray<CompletionItemWithHighlight> presentedItems,
            CompletionItem uniqueItem, CompletionItem suggestionItem, ImmutableArray<CompletionFilterWithState> filters)
        {
            return new CompletionModel(
                initialItems: InitialItems,
                sortedItems: SortedItems,
                snapshot: snapshot, // Updated
                filters: filters, // Updated
                primedExpanders: PrimedExpanders,
                presentedItems: presentedItems, // Updated
                useSoftSelection: UseSoftSelection,
                displaySuggestionItem: DisplaySuggestionItem,
                selectedIndex: SelectedIndex,
                selectSuggestionItem: SelectSuggestionItem,
                suggestionItem: suggestionItem, // Updated
                uniqueItem: uniqueItem, // Updated
                applicableToSpanWasEmpty: ApplicableToSpanWasEmpty,
                uninitialized: Uninitialized
            );
        }

        internal CompletionModel WithApplicableToSpanStatus(bool applicableToSpanIsEmpty)
        {
            return new CompletionModel(
                initialItems: InitialItems,
                sortedItems: SortedItems,
                snapshot: Snapshot,
                filters: Filters,
                primedExpanders: PrimedExpanders,
                presentedItems: PresentedItems,
                useSoftSelection: UseSoftSelection,
                displaySuggestionItem: DisplaySuggestionItem,
                selectedIndex: SelectedIndex,
                selectSuggestionItem: SelectSuggestionItem,
                suggestionItem: SuggestionItem,
                uniqueItem: UniqueItem,
                applicableToSpanWasEmpty: applicableToSpanIsEmpty, // Updated
                uninitialized: Uninitialized
            );
        }

        internal CompletionModel WithExpansion(
            ImmutableArray<CompletionItem> expandedItems,
            ImmutableArray<CompletionItem> sortedItems,
            ImmutableArray<CompletionFilterWithState> filters,
            ImmutableArray<CompletionExpander> primedExpanders)
        {
            return new CompletionModel(
                initialItems: expandedItems, // updated
                sortedItems: sortedItems, // updated
                snapshot: Snapshot,
                filters: filters, // updated
                primedExpanders: primedExpanders, // Updated
                presentedItems: PresentedItems,
                useSoftSelection: UseSoftSelection,
                displaySuggestionItem: DisplaySuggestionItem,
                selectedIndex: SelectedIndex,
                selectSuggestionItem: SelectSuggestionItem,
                suggestionItem: SuggestionItem,
                uniqueItem: UniqueItem,
                applicableToSpanWasEmpty: ApplicableToSpanWasEmpty,
                uninitialized: Uninitialized
            );
        }
    }
}
