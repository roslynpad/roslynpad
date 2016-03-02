using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace RoslynPad.Roslyn.SignatureHelp
{
    internal sealed class SignatureHelperProvider : ISignatureHelpProvider
    {
        private readonly Microsoft.CodeAnalysis.Editor.ISignatureHelpProvider _inner;

        internal SignatureHelperProvider(Microsoft.CodeAnalysis.Editor.ISignatureHelpProvider inner)
        {
            _inner = inner;
        }

        public bool IsTriggerCharacter(char ch)
        {
            return _inner.IsTriggerCharacter(ch);
        }

        public bool IsRetriggerCharacter(char ch)
        {
            return _inner.IsRetriggerCharacter(ch);
        }

        public async Task<SignatureHelpItems> GetItemsAsync(Document document, int position, SignatureHelpTriggerInfo triggerInfo,
            CancellationToken cancellationToken)
        {
            var result = await _inner.GetItemsAsync(document, position, triggerInfo.Inner, cancellationToken).ConfigureAwait(false);
            return result == null ? null : new SignatureHelpItems(result);
        }
    }
}