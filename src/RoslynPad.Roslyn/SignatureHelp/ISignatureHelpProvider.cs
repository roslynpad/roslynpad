using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace RoslynPad.Roslyn.SignatureHelp;

public interface ISignatureHelpProvider
{
    ImmutableArray<char> TriggerCharacters { get; }

    ImmutableArray<char> RetriggerCharacters { get; }

    Task<SignatureHelpItems?> GetItemsAsync(Document document, int position, SignatureHelpTriggerInfo triggerInfo, CancellationToken cancellationToken = default);
}