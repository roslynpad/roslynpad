using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace RoslynPad.Roslyn;

using InternalParsedDocument = Microsoft.CodeAnalysis.ParsedDocument;

/// <summary>
/// A wrapper around Roslyn's internal <c>ParsedDocument</c>, exposing document state
/// (text, syntax tree, root) without requiring callers to use internal APIs.
/// </summary>
public sealed class ParsedDocument
{
    internal InternalParsedDocument Inner { get; }

    internal ParsedDocument(InternalParsedDocument inner)
    {
        Inner = inner;
    }

    public DocumentId Id => Inner.Id;
    public SourceText Text => Inner.Text;
    public SyntaxTree SyntaxTree => Inner.SyntaxTree;
    public SyntaxNode Root => Inner.Root;

    /// <summary>
    /// Creates a <see cref="ParsedDocument"/> synchronously from a Roslyn <see cref="Document"/>.
    /// </summary>
    public static ParsedDocument CreateSynchronously(Document document, CancellationToken cancellationToken = default)
    {
        var inner = InternalParsedDocument.CreateSynchronously(document, cancellationToken);
        return new ParsedDocument(inner);
    }
}
