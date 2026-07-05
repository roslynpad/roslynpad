using System.Collections.Immutable;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;

namespace Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Implementation
{
    internal sealed class CompletionSourceConnectionResult
    {
        internal bool SourceUsesSuggestionMode { get; set; }
        internal SuggestionItemOptions RequestedSuggestionItemOptions { get; set; }
        internal InitialSelectionHint InitialSelectionHint { get; set; }
        internal ImmutableArray<CompletionItem> Items { get; set; }
        internal ImmutableArray<CompletionFilterWithState> Filters { get; set; }
        internal bool IsCanceled { get; set; }

        internal CompletionSourceConnectionResult(bool sourceUsesSuggestionMode,
            SuggestionItemOptions requestedSuggestionItemOptions,
            InitialSelectionHint initialSelectionHint,
            ImmutableArray<CompletionItem> initialCompletionItems,
            ImmutableArray<CompletionFilterWithState> initialCompletionFilters,
            bool isCanceled = false)
        {
            SourceUsesSuggestionMode = sourceUsesSuggestionMode;
            RequestedSuggestionItemOptions = requestedSuggestionItemOptions;
            InitialSelectionHint = initialSelectionHint;
            Items = initialCompletionItems;
            Filters = initialCompletionFilters;
            IsCanceled = isCanceled;
        }

        internal static CompletionSourceConnectionResult Canceled
            => new CompletionSourceConnectionResult(default, default, default, default, default, isCanceled: true);
    }
}
