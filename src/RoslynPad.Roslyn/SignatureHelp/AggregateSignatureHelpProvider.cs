using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;

namespace RoslynPad.Roslyn.SignatureHelp
{
    [Export(typeof(ISignatureHelpProvider)), Shared]
    internal sealed class AggregateSignatureHelpProvider : ISignatureHelpProvider
    {
        private ImmutableArray<Microsoft.CodeAnalysis.SignatureHelp.ISignatureHelpProvider> _providers;

        [ImportingConstructor]
        public AggregateSignatureHelpProvider([ImportMany] IEnumerable<Lazy<Microsoft.CodeAnalysis.SignatureHelp.ISignatureHelpProvider, OrderableLanguageMetadata>> providers)
        {
            _providers = providers.Where(x => x.Metadata.Language == LanguageNames.CSharp)
                .Select(x => x.Value).ToImmutableArray();
        }
     
        public bool IsTriggerCharacter(char ch)
        {
            return _providers.Any(p => p.IsTriggerCharacter(ch));
        }

        public bool IsRetriggerCharacter(char ch)
        {
            return _providers.Any(p => p.IsRetriggerCharacter(ch));
        }
        
        public async Task<SignatureHelpItems> GetItemsAsync(Document document, int position, SignatureHelpTriggerInfo trigger, CancellationToken cancellationToken)
        {
            foreach (var provider in _providers)
            {
                var items = await provider.GetItemsAsync(document, position, trigger.Inner, CancellationToken.None)
                    .ConfigureAwait(false);
                if (items != null)
                {
                    return new SignatureHelpItems(items);
                }
            }
            return null;
        }
    }
}