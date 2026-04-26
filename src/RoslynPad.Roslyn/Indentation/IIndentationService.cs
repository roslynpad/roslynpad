using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;

namespace RoslynPad.Roslyn.Indentation;

public interface IIndentationService : ILanguageService
{
    IndentationResult GetIndentation(Document document, int lineNumber, CancellationToken cancellationToken);
}
