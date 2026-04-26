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

#if AVALONIA
    public void IndentLine(TextDocument textDocument, DocumentLine line)
#else
    public void IndentLine(TextArea textArea, DocumentLine line)
#endif
    {
#if !AVALONIA
        var textDocument = textArea.Document;
#endif
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
            var result = indentationService.GetIndentation(document, lineNumber, CancellationToken.None);

            var sourceText = document.GetTextAsync(CancellationToken.None).Result;
            var linePosition = sourceText.Lines.GetLinePosition(result.BasePosition);
            var desiredIndentation = linePosition.Character + result.Offset;

            if (desiredIndentation >= 0)
            {
                var indentationString = desiredIndentation > 0 ? new string(' ', desiredIndentation) : string.Empty;
                var currentIndentation = GetLineIndentation(textDocument, line);
                if (currentIndentation != indentationString)
                {
                    ReplaceLineIndentation(textDocument, line, indentationString);
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

#if AVALONIA
    public void IndentLines(TextDocument textDocument, int begin, int end)
#else
    public void IndentLines(TextArea textArea, int begin, int end)
#endif
    {
#if !AVALONIA
        var textDocument = textArea.Document;
#endif
        for (var i = begin; i <= end; i++)
        {
            var line = textDocument.GetLineByNumber(i);
#if AVALONIA
            IndentLine(textDocument, line);
#else
            IndentLine(textArea, line);
#endif
        }
    }

    private static string GetLineIndentation(TextDocument document, DocumentLine line)
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
        return lineText[..indentLength];
    }

    private static void ReplaceLineIndentation(TextDocument document, DocumentLine line, string newIndentation)
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
        document.Replace(line.Offset, indentLength, newIndentation);
    }
}
