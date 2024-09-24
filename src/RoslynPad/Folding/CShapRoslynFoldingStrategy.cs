using System.Text.RegularExpressions;
using ICSharpCode.AvalonEdit.Folding;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using TextDocument = ICSharpCode.AvalonEdit.Document.TextDocument;

namespace RoslynPad.Folding;

/// <summary>
/// Folding strategy based on <see cref="Microsoft.CodeAnalysis.CSharp.FoldingStrategy"></see> Roslyn lexer
/// </summary>
public partial class CShapRoslynFoldingStrategy : FoldingStrategy
{
    readonly List<NewFolding> _foldings = [];
    readonly string _defaultFoldingText = "...";
    bool _startFolded;

    /// <summary>
    /// Folding SyntaxKind types <see cref="Microsoft.CodeAnalysis.CSharp.SyntaxKind"></see>
    /// </summary>
    readonly Dictionary<SyntaxKind, SyntaxKind> _pairTypes = new()
    {
        {SyntaxKind.OpenBraceToken,  SyntaxKind.CloseBraceToken},
        {SyntaxKind.OpenBracketToken,  SyntaxKind.CloseBracketToken},
        {SyntaxKind.OpenParenToken,  SyntaxKind.CloseParenToken},
        {SyntaxKind.RegionDirectiveTrivia,  SyntaxKind.EndRegionDirectiveTrivia},
        {SyntaxKind.MultiLineCommentTrivia, SyntaxKind.MultiLineCommentTrivia},
        {SyntaxKind.SingleLineDocumentationCommentTrivia, SyntaxKind.SingleLineDocumentationCommentTrivia},
        {SyntaxKind.UsingDirective, SyntaxKind.UsingDirective}
    };

    protected override IEnumerable<NewFolding> CreateNewFoldings(TextDocument document, out int firstErrorOffset)
    {
        firstErrorOffset = -1;

        _foldings.Clear();

        _startFolded = false;
        var tree = CSharpSyntaxTree.ParseText(document.Text);
        var root = tree.GetCompilationUnitRoot();
        var text = tree.GetText();

        _pairTypes.AsParallel().ForAll(x =>
        {
            if ($"{x.Key}".Contains("Token"))
                AddTokenFoldings(root, text, x.Key);
            else if ($"{x.Key}".Contains("Using"))
                AddUsingFoldings(root, text, x.Key);
            else
                AddTriviaFoldings(root, text, x.Key);
        });

        _foldings.Sort((a, b) => a.StartOffset.CompareTo(b.StartOffset));

        return _foldings;
    }

    /// <summary>
    /// Add folding to using directives 
    /// </summary>
    /// <param name="root"></param>
    /// <param name="text"></param>
    /// <param name="openType"></param>
    private void AddUsingFoldings(CompilationUnitSyntax root, SourceText text, SyntaxKind openType)
    {
        var itens = root.DescendantNodes().OfType<UsingDirectiveSyntax>().ToList();

        if (itens.Count <= 0) return;

        var start = itens.OrderBy(s => s.FullSpan.Start).First();
        var end = itens.OrderByDescending(s => s.FullSpan.End).First();

        if (start == null || end == null) return;

        var lineStart = text.Lines.GetLinePosition(start.SpanStart).Line;
        var lineEnd = text.Lines.GetLinePosition(end.Span.End).Line;
        if (lineStart == lineEnd) return;

        _foldings.Add(new NewFolding(start.SpanStart, end.Span.End)
        {
            Name = "using",
            IsDefinition = true,
            DefaultClosed = _startFolded,
        });
    }

    /// <summary>
    /// Add folding to trivia syntax type
    /// </summary>
    /// <param name="root"></param>
    /// <param name="text"></param>
    /// <param name="openType"></param>
    private void AddTriviaFoldings(CompilationUnitSyntax root, SourceText text, SyntaxKind openType)
    {
        var stack = new Stack<SyntaxTrivia>();
        var closeType = _pairTypes[openType];

        var items = root.DescendantTrivia().Where(s => s.IsKind(openType) || s.IsKind(closeType)).ToList();

        for (var i = 0; i < items.Count; i++)
        {
            if (openType == closeType)
            {
                _foldings.Add(new NewFolding(items[i].SpanStart - 3, items[i].Span.End - 2)
                {
                    Name = _defaultFoldingText,
                    IsDefinition = true,
                    DefaultClosed = _startFolded,
                });
            }
            else if (items[i].IsKind(openType))
            {
                stack.Push(items[i]);
            }
            else if (stack.Count > 0 && items[i].IsKind(closeType))
            {
                var match = stack.Pop();
                var lineStart = text.Lines.GetLinePosition(match.SpanStart).Line;
                var lineEnd = text.Lines.GetLinePosition(items[i].SpanStart).Line;
                if (lineStart == lineEnd) continue;

                var foldText = _defaultFoldingText;
                if (openType == SyntaxKind.RegionDirectiveTrivia)
                    foldText = RegionRegex().Replace(match.ToFullString(), "");

                _foldings.Add(new NewFolding(match.SpanStart, items[i].Span.End)
                {
                    Name = foldText,
                    IsDefinition = true,
                    DefaultClosed = _startFolded,
                });
            }
        }
    }

    /// <summary>
    /// Add folding to token syntax type
    /// </summary>
    /// <param name="root"></param>
    /// <param name="text"></param>
    /// <param name="openType"></param>
    private void AddTokenFoldings(CompilationUnitSyntax root, SourceText text, SyntaxKind openType)
    {
        var stack = new Stack<SyntaxToken>();
        var closeType = _pairTypes[openType];
        var items = root.DescendantTokens().Where(s => s.IsKind(openType) || s.IsKind(closeType)).ToList();
        for (var i = 0; i < items.Count; i++)
        {
            if (items[i].IsKind(openType))
            {
                stack.Push(items[i]);
            }
            else if (stack.Count > 0 && items[i].IsKind(closeType))
            {
                var match = stack.Pop();
                var lineStart = text.Lines.GetLinePosition(match.SpanStart).Line;
                var lineEnd = text.Lines.GetLinePosition(items[i].SpanStart).Line;
                if (lineStart == lineEnd) continue;

                var foldText = _defaultFoldingText;
                if (openType == SyntaxKind.RegionDirectiveTrivia)
                    foldText = RegionRegex().Replace(match.ToFullString(),"");

                _foldings.Add(new NewFolding(match.SpanStart, items[i].Span.End)
                {
                    Name = foldText,
                    IsDefinition = true,
                    DefaultClosed = _startFolded,
                });
            }
        }
    }

    /// <summary>
    /// Add folding to token syntax type
    /// </summary>
    /// <param name="root"></param>
    /// <param name="text"></param>
    /// <param name="openType"></param>
    private void AddFoldings(CompilationUnitSyntax root, SourceText text, SyntaxKind openType)
    {
        var stack = new Stack<SyntaxToken>();
        var closeType = _pairTypes[openType];
        var items = root.DescendantTokens().Where(s => s.IsKind(openType) || s.IsKind(closeType)).ToList();
        for (var i = 0; i < items.Count; i++)
        {
            if (items[i].IsKind(openType))
            {
                stack.Push(items[i]);
            }
            else if (stack.Count > 0 && items[i].IsKind(closeType))
            {
                var match = stack.Pop();
                var lineStart = text.Lines.GetLinePosition(match.SpanStart).Line;
                var lineEnd = text.Lines.GetLinePosition(items[i].SpanStart).Line;
                if (lineStart == lineEnd) continue;

                _foldings.Add(new NewFolding(match.SpanStart, items[i].Span.End)
                {
                    Name = _defaultFoldingText,
                    IsDefinition = true,
                    DefaultClosed = _startFolded,
                });
            }
        }
    }

    [GeneratedRegex(@"\#region\s+")]
    private static partial Regex RegionRegex();
}
