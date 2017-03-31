using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace RoslynPad.Roslyn.SignatureHelp
{
    public static class SignatureHelpProviderExtensions
    {
        public static async Task<bool> IsTriggerCharacter(this ISignatureHelpProvider provider, Document document, int position)
        {
            if (provider == null) throw new ArgumentNullException(nameof(provider));
            var text = await document.GetTextAsync().ConfigureAwait(false);
            var character = text.GetSubText(new TextSpan(position, 1))[0];
            return provider.IsTriggerCharacter(character);
        }
    }
}