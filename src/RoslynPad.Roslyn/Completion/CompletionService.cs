using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Options;
using InnerCompletionService = Microsoft.CodeAnalysis.Completion.CompletionService;

namespace RoslynPad.Roslyn.Completion
{
    public static class CompletionService
    {
        public static CompletionRules GetCompletionRules(Document document)
        {
            var rules = InnerCompletionService.GetCompletionRules(document);
            return new CompletionRules(rules);
        }

        public static async Task<CompletionList> GetCompletionListAsync(Document document, int position,
            CompletionTriggerInfo triggerInfo, OptionSet options = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var list = await InnerCompletionService.GetCompletionListAsync(document, position, triggerInfo.Inner, options, null,
                cancellationToken).ConfigureAwait(false);
            return list == null ? null : new CompletionList(list);
        }

        public static Task<bool> IsCompletionTriggerCharacterAsync(Document document, int characterPosition,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return InnerCompletionService.IsCompletionTriggerCharacterAsync(document, characterPosition, null, cancellationToken);
        }
    }
}