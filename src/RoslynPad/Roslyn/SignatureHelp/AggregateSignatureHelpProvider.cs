using System.Collections.Immutable;
using System.ComponentModel.Composition.Hosting;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using RoslynPad.Utilities;

namespace RoslynPad.Roslyn.SignatureHelp
{
    [Export(typeof(ISignatureHelpProvider)), Shared]
    internal sealed class AggregateSignatureHelpProvider : ISignatureHelpProvider
    {
        private ImmutableArray<ISignatureHelpProvider> _providers;

        internal void Initialize(CompositionContainer container)
        {
            _providers = container.GetExportedValues(SignatureHelperProvider.InterfaceType)
                .Select(x => (ISignatureHelpProvider)new SignatureHelperProvider(x))
                .ToImmutableArray();
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
                var items = await provider.GetItemsAsync(document, position, trigger, CancellationToken.None)
                    .ConfigureAwait(false);
                if (items != null)
                {
                    return items;
                }
            }
            return null;
        }
    }
}