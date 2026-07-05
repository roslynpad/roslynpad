using System.Composition;
using System.Text;
using Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.AsyncCompletion;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Morgania.CodeAnalysis.Editor;

/// <summary>
/// Expands the LSP snippet text carried by Roslyn's snippet completion items (foreach, if, prop, …).
/// In VS this goes through the LSP client's snippet support, which is excluded from the recompile;
/// without an export, committing a snippet item fails a null contract in Roslyn's CommitManager.
/// Roslyn's RoslynLSPSnippetConverter only emits "$0" (final caret) and "${n:placeholder}", so that
/// is all this parses. The first placeholder is selected for type-over; there is no linked editing
/// or tab-stop navigation.
/// </summary>
[Shared]
[Export(typeof(ILanguageServerSnippetExpander))]
internal sealed class LspSnippetExpander : ILanguageServerSnippetExpander
{
    public bool TryExpand(string lspSnippetText, SnapshotSpan snapshotSpan, ITextView textView)
    {
        var (text, fields, caretOffset) = Parse(lspSnippetText);

        var buffer = snapshotSpan.Snapshot.TextBuffer;
        var span = snapshotSpan.TranslateTo(buffer.CurrentSnapshot, SpanTrackingMode.EdgeInclusive);

        ITextSnapshot applied;
        using (var edit = buffer.CreateEdit())
        {
            edit.Replace(span.Span, text);
            applied = edit.Apply();
        }

        var start = span.Start.Position;
        var firstField = fields.OrderBy(field => field.Index).FirstOrDefault();
        if (firstField.Length > 0)
        {
            textView.Selection.Select(new SnapshotSpan(applied, start + firstField.Offset, firstField.Length), isReversed: false);
            textView.Caret.MoveTo(textView.Selection.ActivePoint.Position);
        }
        else
        {
            textView.Caret.MoveTo(new SnapshotPoint(applied, start + caretOffset));
        }

        textView.Caret.EnsureVisible();
        return true;
    }

    private static (string Text, List<(int Index, int Offset, int Length)> Fields, int CaretOffset) Parse(string snippet)
    {
        var text = new StringBuilder();
        var fields = new List<(int Index, int Offset, int Length)>();
        var caretOffset = -1;

        for (var i = 0; i < snippet.Length; i++)
        {
            if (snippet[i] == '$' && i + 1 < snippet.Length)
            {
                if (snippet[i + 1] == '0')
                {
                    caretOffset = text.Length;
                    i++;
                    continue;
                }

                if (snippet[i + 1] == '{')
                {
                    var colon = snippet.IndexOf(':', i + 2);
                    var close = colon < 0 ? -1 : snippet.IndexOf('}', colon + 1);
                    if (close > 0 && int.TryParse(snippet.AsSpan(i + 2, colon - i - 2), out var index))
                    {
                        var placeholder = snippet[(colon + 1)..close];
                        fields.Add((index, text.Length, placeholder.Length));
                        text.Append(placeholder);
                        i = close;
                        continue;
                    }
                }
            }

            text.Append(snippet[i]);
        }

        return (text.ToString(), fields, caretOffset < 0 ? text.Length : caretOffset);
    }
}
