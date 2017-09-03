using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RoslynPad.Roslyn
{
    public static class DocumentExtensions
    {
        public static TLanguageService GetLanguageService<TLanguageService>(this Document document)
            where TLanguageService : class, ILanguageService
        {
            return document?.Project?.LanguageServices?.GetService<TLanguageService>();
        }

        public static async Task<SyntaxToken> GetTouchingWordAsync(this Document document, int position, CancellationToken cancellationToken, bool findInsideTrivia = false)
        {
            var syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            return await syntaxTree.GetTouchingTokenAsync(position, syntaxFacts.IsWord, cancellationToken, findInsideTrivia).ConfigureAwait(false);
        }

        public static async Task<ImmutableArray<string>> GetReferencesDirectivesAsync(this Document document)
        {
            var syntaxRoot = await document.GetSyntaxRootAsync().ConfigureAwait(false);

            return (syntaxRoot as CompilationUnitSyntax)?.GetReferenceDirectives()
                   .Select(x => x.File.ValueText).ToImmutableArray() ?? ImmutableArray<string>.Empty;
        }

    }
}