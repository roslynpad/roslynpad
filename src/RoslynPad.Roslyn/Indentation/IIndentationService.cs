using Microsoft.CodeAnalysis.Host;

namespace RoslynPad.Roslyn.Indentation;

public interface IIndentationService : ILanguageService
{
    IndentationResult GetIndentation(RoslynParsedDocument document, int lineNumber, CancellationToken cancellationToken);
}
