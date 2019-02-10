using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace RoslynPad.Roslyn.SignatureHelp
{
    public interface ISignatureHelpProvider
    {
        bool IsTriggerCharacter(char ch);

        bool IsRetriggerCharacter(char ch);

        Task<SignatureHelpItems?> GetItemsAsync(Document document, int position, SignatureHelpTriggerInfo triggerInfo, CancellationToken cancellationToken = default);
    }
}