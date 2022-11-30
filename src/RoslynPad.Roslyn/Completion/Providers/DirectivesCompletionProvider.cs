using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;

namespace RoslynPad.Roslyn.Completion.Providers;

[ExportCompletionProvider("DirectivesCompletionProvider", LanguageNames.CSharp)]
internal class DirectivesCompletionProvider : CompletionProvider
{
    private static readonly ImmutableArray<string> s_directivesName = ImmutableArray.Create("r");

    public override async Task ProvideCompletionsAsync(CompletionContext context)
    {
        var originatingDocument = context.Document;
        if (originatingDocument.SourceCodeKind != SourceCodeKind.Regular)
        {
            return;
        }

        var cancellationToken = context.CancellationToken;
        var position = context.Position;
        var semanticModel = await originatingDocument.ReuseExistingSpeculativeModelAsync(position, cancellationToken).ConfigureAwait(false);
        var service = originatingDocument.GetRequiredLanguageService<ISyntaxContextService>();
        var syntaxContext = service.CreateContext(originatingDocument, semanticModel, position, cancellationToken);
        if (!syntaxContext.IsPreProcessorExpressionContext)
        {
            return;
        }

        foreach (var name in s_directivesName)
        {
            context.AddItem(CommonCompletionItem.Create(
            name,
            displayTextSuffix: "",
            CompletionItemRules.Default,
            glyph: Microsoft.CodeAnalysis.Glyph.Keyword,
            sortText: "_0_" + name));
        }
    }
}
