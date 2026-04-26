using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace RoslynPad.Roslyn;

/// <summary>
/// A wrapper around Roslyn's internal <c>ParsedDocument</c>, exposing document state
/// (text, syntax tree, root) without requiring callers to use internal APIs.
/// </summary>
public sealed class RoslynParsedDocument
{
    internal ParsedDocument Inner { get; }

    internal RoslynParsedDocument(ParsedDocument inner)
    {
        Inner = inner;
    }

    public DocumentId Id => Inner.Id;
    public SourceText Text => Inner.Text;
    public SyntaxTree SyntaxTree => Inner.SyntaxTree;
    public SyntaxNode Root => Inner.Root;

    /// <summary>
    /// Creates a <see cref="RoslynParsedDocument"/> synchronously from a Roslyn <see cref="Document"/>.
    /// </summary>
    public static RoslynParsedDocument CreateSynchronously(Document document, CancellationToken cancellationToken = default)
    {
        var inner = ParsedDocument.CreateSynchronously(document, cancellationToken);
        return new RoslynParsedDocument(inner);
    }
}
