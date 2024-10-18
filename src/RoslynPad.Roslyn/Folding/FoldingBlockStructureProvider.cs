using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Structure;

namespace RoslynPad.Roslyn.Folding;

public class FoldingBlockStructureProvider()
{
    public async Task<List<ElementSpan>> GetCodeFoldingsAsync(Document document, CancellationToken cancellationToken)
    {
        var blocks = await document.GetLanguageService<IBlockStructureService>()
                           .GetBlockStructureAsync(document, cancellationToken).ConfigureAwait(false);

        return blocks?.Select(s => new ElementSpan
        {
            Text = s.BannerText,
            StartOffset = s.TextSpan.Start,
            EndOffset = s.TextSpan.End
        }).ToList() ?? [];
    }
}
