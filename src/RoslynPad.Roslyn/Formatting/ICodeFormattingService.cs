using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace RoslynPad.Roslyn.Formatting;

public interface ICodeFormattingService : ILanguageService
{
    bool ShouldFormatOnTypedCharacter(RoslynParsedDocument document, char typedChar, int caretPosition, CancellationToken cancellationToken);

    ImmutableArray<TextChange> GetFormattingChangesOnTypedCharacter(RoslynParsedDocument document, int caretPosition, CancellationToken cancellationToken);
}
