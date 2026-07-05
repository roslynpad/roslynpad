using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Implementation
{
    /// <summary>
    /// Internal item source used during lifetime of the suggestion mode item.
    /// </summary>
    internal class SuggestionModeCompletionItemSource : IAsyncCompletionSource
    {
        private SuggestionItemOptions _options;

        internal SuggestionModeCompletionItemSource(SuggestionItemOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        Task<CompletionContext> IAsyncCompletionSource.GetCompletionContextAsync(IAsyncCompletionSession session, CompletionTrigger trigger, SnapshotPoint triggerLocation, SnapshotSpan applicableToSpan, CancellationToken token)
        {
            throw new NotImplementedException("This item source is not meant to be registered. It is used only to provide a tooltip.");
        }

        Task<object> IAsyncCompletionSource.GetDescriptionAsync(IAsyncCompletionSession session, CompletionItem item, CancellationToken token)
        {
            return Task.FromResult<object>(_options.ToolTipText);
        }

        public CompletionStartData InitializeCompletion(CompletionTrigger trigger, SnapshotPoint triggerLocation, CancellationToken token)
        {
            return CompletionStartData.DoesNotParticipateInCompletion;
        }
    }
}
