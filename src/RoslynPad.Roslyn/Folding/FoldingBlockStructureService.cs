#pragma warning disable CS8602, CS8604, CA2007, CS9113

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace RoslynPad.Roslyn.Folding;

[ExportLanguageService(typeof(IBlockStructureService), LanguageNames.CSharp), Shared]
public partial class FoldingBlockStructureService : BlockStructureService, IBlockStructureService
{
    public async Task<IEnumerable<BlockSpan>> GetBlockStructureAsync(Document document, CancellationToken cancellationToken)
    {
        return (await GetFoldingBlockStructure(document, cancellationToken)).Spans;
    }

    public override async Task<BlockStructure> GetBlockStructureAsync(Document document, BlockStructureOptions options, CancellationToken cancellationToken)
        => await GetFoldingBlockStructure(document, cancellationToken);

    Dictionary<SyntaxKind, SyntaxKind> PairTypes => new()
    {
        {SyntaxKind.OpenBraceToken,  SyntaxKind.CloseBraceToken},
        {SyntaxKind.OpenBracketToken,  SyntaxKind.CloseBracketToken},
        {SyntaxKind.OpenParenToken,  SyntaxKind.CloseParenToken},
    };

    public override string Language => "C#";

    public async Task<BlockStructure> GetFoldingBlockStructure(Document document, CancellationToken cancellationToken)
    {
        var syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
        var root = await syntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);
        var spans = ImmutableArray.CreateBuilder<BlockSpan>();
        var text = root.GetText();

        FindUsingsBlock(root, spans, text);
        FindRegionsBlocks(root, spans, text);
        FindBracesBlocks(root, spans, text);

        return new BlockStructure(spans.ToImmutable());
    }

    private bool IsOpenType(SyntaxToken token)
    {
        return token.IsKind(SyntaxKind.OpenBraceToken) ||
               token.IsKind(SyntaxKind.OpenParenToken) ||
               token.IsKind(SyntaxKind.OpenBracketToken);
    }

    private bool IsCommentType(SyntaxTrivia token)
    {
        return token.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) ||
               token.IsKind(SyntaxKind.MultiLineCommentTrivia);
    }

    private bool IsOpenRegionType(SyntaxTrivia token)
    {
        return token.IsKind(SyntaxKind.RegionDirectiveTrivia);
    }

    private bool IsRegionType(SyntaxTrivia token)
    {
        return token.IsKind(SyntaxKind.RegionDirectiveTrivia) ||
               token.IsKind(SyntaxKind.EndRegionDirectiveTrivia);
    }

    private void FindRegionsBlocks(SyntaxNode node, ImmutableArray<BlockSpan>.Builder spans, SourceText text)
    {
        var stack = new Stack<SyntaxTrivia>();
        var tokens = node.DescendantTrivia().Where(s => IsCommentType(s) || IsRegionType(s));

        foreach (var token in tokens)
        {
            if (IsOpenRegionType(token))
            {
                stack.Push(token);
            }
            else if (!IsOpenRegionType(token) && !IsCommentType(token))
            {
                var start = stack.Pop();
                var lineStart = text.Lines.GetLinePosition(start.SpanStart).Line;
                var lineEnd = text.Lines.GetLinePosition(token.SpanStart).Line;
                if (lineStart == lineEnd)
                    continue;

                spans.Add(new BlockSpan(
                         isCollapsible: true,
                         textSpan: TextSpan.FromBounds(start.SpanStart, token.Span.End),
                         hintSpan: TextSpan.FromBounds(start.SpanStart, token.Span.End),
                         type: BlockTypes.Nonstructural,
                         bannerText: AllRegexHelpers.RegionRegex().Replace(start.ToFullString(), ""),
                         autoCollapse: false));

            }
            else if (IsCommentType(token))
            {
                var bannerText = "/*..*/";
                var startAjustment = 0;

                if (token.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia))
                {
                    bannerText = "/// <summary>";
                    startAjustment = 3;
                }

                spans.Add(new BlockSpan(
                     isCollapsible: true,
                     textSpan: TextSpan.FromBounds(token.SpanStart - startAjustment, token.Span.End),
                     hintSpan: TextSpan.FromBounds(token.SpanStart - startAjustment, token.Span.End),
                     type: BlockTypes.Nonstructural,
                     bannerText: bannerText,
                     autoCollapse: false));
            }
        }
    }

    private void FindBracesBlocks(SyntaxNode node, ImmutableArray<BlockSpan>.Builder spans, SourceText text)
    {
        foreach (var child in node.ChildNodesAndTokens())
        {
            if (child.IsNode)
            {
                FindBracesBlocks(child.AsNode(), spans, text);
            }
            else if (child.IsToken)
            {
                var token = child.AsToken();
                if (!IsOpenType(token)) continue;

                var end = FindMatchingEndBrace(token, token.ContextualKind());
                if (end != null)
                {
                    var lineStart = text.Lines.GetLinePosition(token.SpanStart).Line;
                    var lineEnd = text.Lines.GetLinePosition(end.Value.SpanStart).Line;
                    if (lineStart == lineEnd)
                        continue;

                    spans.Add(new BlockSpan(
                             isCollapsible: true,
                             textSpan: TextSpan.FromBounds(token.SpanStart, end.Value.Span.End),
                             hintSpan: TextSpan.FromBounds(token.SpanStart, end.Value.Span.End),
                             type: BlockTypes.Nonstructural,
                             bannerText: "...",
                             autoCollapse: false));
                }

            }
        }
    }

    private void FindUsingsBlock(SyntaxNode root, ImmutableArray<BlockSpan>.Builder spans, SourceText text)
    {
        var itens = root.DescendantNodes().OfType<UsingDirectiveSyntax>().ToList();
        if (itens.Count <= 0) return;

        var start = itens.OrderBy(s => s.FullSpan.Start).First();
        var end = itens.OrderByDescending(s => s.FullSpan.End).First();

        if (start == null || end == null) return;

        var lineStart = text.Lines.GetLinePosition(start.SpanStart).Line;
        var lineEnd = text.Lines.GetLinePosition(end.Span.End).Line;
        if (lineStart == lineEnd) return;

        spans.Add(new BlockSpan(
            isCollapsible: true,
            textSpan: TextSpan.FromBounds(start.SpanStart, end.Span.End),
            hintSpan: TextSpan.FromBounds(start.SpanStart, end.Span.End),
            type: BlockTypes.Nonstructural,
            bannerText: "using",
            autoCollapse: false));
    }

    private SyntaxToken? FindMatchingEndBrace(SyntaxToken startRegion, SyntaxKind startKind)
    {
        var parent = startRegion.Parent;
        var siblings = parent.ChildNodesAndTokens();
        var startIndex = siblings.IndexOf(startRegion);

        for (int i = startIndex + 1; i < siblings.Count; i++)
        {
            var sibling = siblings[i];
            if (sibling.IsToken && sibling.AsToken().IsKind(PairTypes[startKind]))
            {
                return sibling.AsToken();
            }
        }

        return null;
    }
}

