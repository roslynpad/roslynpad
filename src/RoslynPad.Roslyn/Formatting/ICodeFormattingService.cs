using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace RoslynPad.Roslyn.Formatting;

public interface ICodeFormattingService : ILanguageService
{
    bool ShouldFormatOnTypedCharacter(Document document, char typedChar, int caretPosition, CancellationToken cancellationToken);

    ImmutableArray<TextChange> GetFormattingChangesOnTypedCharacter(Document document, int caretPosition, CancellationToken cancellationToken);
}
