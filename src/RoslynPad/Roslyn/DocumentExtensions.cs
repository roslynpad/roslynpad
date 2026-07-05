using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace RoslynPad.Roslyn;

public static class DocumentExtensions
{
    public static TLanguageService GetLanguageService<TLanguageService>(this Document document)
        where TLanguageService : class, ILanguageService
    {
        return document.Project.Services.GetRequiredService<TLanguageService>();
    }

    public static async Task<SyntaxToken> GetTouchingWordAsync(this Document document, int position, CancellationToken cancellationToken, bool findInsideTrivia = false)
    {
        var syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
        if (syntaxTree == null)
        {
            return default;
        }

        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var syntaxFactsService = document.GetLanguageService<ISyntaxFactsService>();
        return await syntaxTree.GetTouchingTokenAsync(semanticModel, position, (_, token) => syntaxFactsService.IsWord(token), cancellationToken, findInsideTrivia).ConfigureAwait(false);
    }
}
