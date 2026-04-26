using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace RoslynPad.Roslyn.Formatting;

public interface ICodeFormattingService : ILanguageService
{
    bool ShouldFormatOnTypedCharacter(ParsedDocument document, char typedChar, int caretPosition, CancellationToken cancellationToken);

    ImmutableArray<TextChange> GetFormattingChangesOnTypedCharacter(ParsedDocument document, int caretPosition, CancellationToken cancellationToken);
}
