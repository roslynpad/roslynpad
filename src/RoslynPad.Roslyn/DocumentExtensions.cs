using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using System.Threading;
using System.Threading.Tasks;

namespace RoslynPad.Roslyn
{
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

            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            return await syntaxTree.GetTouchingTokenAsync(position, syntaxFacts.IsWord, cancellationToken, findInsideTrivia).ConfigureAwait(false);
        }

        public static Document WithFrozenPartialSemantics(this Document document, CancellationToken cancellationToken = default)
        {
            return document.WithFrozenPartialSemantics(cancellationToken);
        }
    }
}
