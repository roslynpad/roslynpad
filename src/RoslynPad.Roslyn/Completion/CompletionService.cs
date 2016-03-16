using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Options;
using InnerCompletionService = Microsoft.CodeAnalysis.Completion.CompletionService;

namespace RoslynPad.Roslyn.Completion
{
    public static class CompletionService
    {
        private static IEnumerable<CompletionListProvider> _additionalProviders;

        internal static void Initialize(CompositionContainer container)
        {
            _additionalProviders = container.GetExportedValues<CompletionListProvider>();
        }

        public static CompletionRules GetCompletionRules(Document document)
        {
            var rules = InnerCompletionService.GetCompletionRules(document);
            return new CompletionRules(rules);
        }

        public static async Task<CompletionList> GetCompletionListAsync(Document document, int position,
            CompletionTriggerInfo triggerInfo, OptionSet options = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var providers = GetCompletionListProviders(document);
            var list = await InnerCompletionService.GetCompletionListAsync(document, position, triggerInfo.Inner, options, providers,
                cancellationToken).ConfigureAwait(false);
            return list == null ? null : new CompletionList(list);
        }

        private static IEnumerable<CompletionListProvider> GetCompletionListProviders(Document document)
        {
            var languageService = document.GetLanguageService<ICompletionService>();
            IEnumerable<CompletionListProvider> providers = null;
            if (languageService != null)
            {
                providers = InnerCompletionService.GetDefaultCompletionListProviders(document);
                if (_additionalProviders != null)
                {
                    providers = providers.Concat(_additionalProviders);
                }
            }
            return providers;
        }

        public static Task<bool> IsCompletionTriggerCharacterAsync(Document document, int characterPosition,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return InnerCompletionService.IsCompletionTriggerCharacterAsync(document, characterPosition, null, cancellationToken);
        }
    }
}