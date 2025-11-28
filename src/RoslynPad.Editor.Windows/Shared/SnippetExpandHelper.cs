using Microsoft.CodeAnalysis;
using RoslynPad.Roslyn;
using RoslynPad.Roslyn.Completion;

namespace RoslynPad.Editor;

/// <summary>
/// Helper methods for snippet text extraction and manipulation.
/// </summary>
internal static class SnippetExpandHelper
{
    /// <summary>
    /// Checks if a character is a valid snippet name character.
    /// </summary>
    public static bool IsSnippetChar(char c)
    {
        return char.IsLetterOrDigit(c) || c == '_' || c == '~' || c == '#' || c == '-' || c == '!';
    }

    /// <summary>
    /// Checks if a character is a special prefix character that can start a snippet name.
    /// </summary>
    public static bool IsSnippetPrefixChar(char c)
    {
        return c == '#' || c == '~' || c == '!' || c == '-';
    }

    /// <summary>
    /// Extracts the snippet text at the specified position, including any special prefix characters.
    /// Returns the text and the actual start offset.
    /// </summary>
    /// <param name="document">The document to extract from</param>
    /// <param name="initialOffset">The initial offset (end of the word)</param>
    /// <param name="initialLength">The initial length of the word</param>
    /// <returns>A tuple containing (actualOffset, actualLength, snippetText)</returns>
    public static (int offset, int length, string text) ExtractSnippetText(
        IDocument document, 
        int initialOffset, 
        int initialLength)
    {
        var offset = initialOffset;
        var length = initialLength;
        
        // Look backwards to include special characters that are part of snippet names
        // (like # in #if, #region, ~ in destructor)
        while (offset > 0)
        {
            var prevChar = document.GetCharAt(offset - 1);
            if (IsSnippetPrefixChar(prevChar))
            {
                offset--;
                length++;
            }
            else
            {
                break;
            }
        }
        
        var text = document.GetText(offset, length);
        return (offset, length, text);
    }

    /// <summary>
    /// Extracts the snippet text from a line of text by looking backwards from the end position.
    /// Returns the word start position (relative to line start) and the snippet text.
    /// </summary>
    /// <param name="lineText">The line of text to extract from</param>
    /// <param name="endPosition">The end position (relative to line start)</param>
    /// <returns>A tuple containing (wordStart, snippetText) or null if no valid snippet found</returns>
    public static (int wordStart, string text)? ExtractSnippetTextFromLine(
        string lineText, 
        int endPosition)
    {
        // Find the last word (snippet keyword)
        var wordStart = endPosition;
        while (wordStart > 0)
        {
            var c = lineText[wordStart - 1];
            if (IsSnippetChar(c))
            {
                wordStart--;
            }
            else
            {
                break;
            }
        }

        if (wordStart == endPosition)
        {
            return null; // No word found
        }

        var text = lineText.Substring(wordStart, endPosition - wordStart);
        return (wordStart, text);
    }

    /// <summary>
    /// Expands a snippet at the specified location in the document.
    /// </summary>
    /// <param name="snippetManager">The snippet manager to find snippets</param>
    /// <param name="textArea">The text area to insert into</param>
    /// <param name="document">The document being edited</param>
    /// <param name="initialOffset">The initial offset where the snippet keyword ends</param>
    /// <param name="initialLength">The initial length of the snippet keyword</param>
    /// <param name="roslynDocument">Optional Roslyn document for getting class name context</param>
    /// <returns>True if snippet was expanded, false otherwise</returns>
    public static async Task<bool> ExpandSnippetAsync(
        SnippetManager snippetManager,
        TextArea textArea,
        int initialOffset,
        int initialLength,
        Document? roslynDocument = null)
    {
        var document = textArea.Document;
        // Extract the actual snippet text including special prefix characters
        var (offset, length, snippetText) = ExtractSnippetText(document, initialOffset, initialLength);
        
        var snippet = snippetManager.FindSnippet(snippetText);
        if (snippet == null)
        {
            return false;
        }

        // Get the class name if we have a Roslyn document
        string? className = null;
        if (roslynDocument != null)
        {
            className = await GetCurrentClassNameAsync(roslynDocument, offset).ConfigureAwait(false);
        }

        var avalonEditSnippet = snippet.CreateAvalonEditSnippet(className);
        using (document.RunUpdate())
        {
            document.Remove(offset, length);
            textArea.Caret.Offset = offset;
            avalonEditSnippet.Insert(textArea);
        }

        return true;
    }

    /// <summary>
    /// Gets the current class name at the specified position using Roslyn semantic model.
    /// </summary>
    /// <param name="document">The Roslyn document</param>
    /// <param name="position">The position to check</param>
    /// <returns>The class name if found, null otherwise</returns>
    public static async Task<string?> GetCurrentClassNameAsync(Document document, int position)
    {
        try
        {
            var semanticModel = await document.GetSemanticModelAsync().ConfigureAwait(false);
            if (semanticModel == null)
            {
                return null;
            }

            var root = await document.GetSyntaxRootAsync().ConfigureAwait(false);
            if (root == null)
            {
                return null;
            }

            var node = root.FindToken(position).Parent;
            
            while (node != null)
            {
                var symbol = semanticModel.GetDeclaredSymbol(node);
                if (symbol is INamedTypeSymbol namedType)
                {
                    return namedType.Name;
                }
                node = node.Parent;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GetCurrentClassNameAsync error: {ex}");
        }

        return null;
    }
}
