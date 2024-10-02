using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Structure;

namespace RoslynPad.Roslyn.Folding;


public interface IBlockStructureService : ILanguageService
{
    Task<IEnumerable<BlockSpan>> GetBlockStructureAsync(Document document, CancellationToken cancellationToken);
}
