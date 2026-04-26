using Microsoft.CodeAnalysis;
using RoslynPad.Roslyn;
using RoslynPad.Roslyn.Indentation;

namespace RoslynPad.Editor;

internal class RoslynIndentationStrategy :
#if AVALONIA
    AvaloniaEdit.Indentation.IIndentationStrategy
#else
    ICSharpCode.AvalonEdit.Indentation.IIndentationStrategy
#endif
{
    private readonly IRoslynHost _roslynHost;
    private readonly DocumentId _documentId;

    public RoslynIndentationStrategy(IRoslynHost roslynHost, DocumentId documentId)
    {
        _roslynHost = roslynHost;
        _documentId = documentId;
    }

    public void IndentLine(TextDocument textDocument, DocumentLine line)
    {
        var document = _roslynHost.GetDocument(_documentId);
        if (document == null)
        {
            return;
        }

        var indentationService = document.GetLanguageService<IIndentationService>();
        if (indentationService == null)
        {
            return;
        }

        // AvalonEdit uses 1-based line numbers, Roslyn uses 0-based
        var lineNumber = line.LineNumber - 1;

        try
        {
            var parsedDocument = ParsedDocument.CreateSynchronously(document);
            var result = indentationService.GetIndentation(parsedDocument, lineNumber, CancellationToken.None);

            var linePosition = parsedDocument.Text.Lines.GetLinePosition(result.BasePosition);
            var desiredIndentation = linePosition.Character + result.Offset;

            if (desiredIndentation >= 0)
            {
                var indentationString = desiredIndentation > 0 ? new string(' ', desiredIndentation) : string.Empty;
                var currentIndentLength = GetIndentLength(textDocument, line);
                var currentIndentation = textDocument.GetText(line.Offset, currentIndentLength);
                if (currentIndentation != indentationString)
                {
                    textDocument.Replace(line.Offset, currentIndentLength, indentationString);
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    public void IndentLines(TextDocument textDocument, int begin, int end)
    {
        for (var i = begin; i <= end; i++)
        {
            var line = textDocument.GetLineByNumber(i);
            IndentLine(textDocument, line);
        }
    }

    private static int GetIndentLength(TextDocument document, DocumentLine line)
    {
        var lineText = document.GetText(line.Offset, line.Length);
        var indentLength = 0;
        foreach (var ch in lineText)
        {
            if (ch is ' ' or '\t')
            {
                indentLength++;
            }
            else
            {
                break;
            }
        }
        return indentLength;
    }
}
