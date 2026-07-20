#nullable enable

namespace Microsoft.VisualStudio.Text.Editor.Implementation;

using System.Composition;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;

/// <summary>
/// Word-structure navigation for plain text, recreated to VS semantics (the VS
/// natural-language navigator was never open-sourced; the vendored
/// <c>DefaultTextNavigator</c> is a degenerate per-character fallback). A word is a maximal
/// run of one character class: identifier characters (letters, digits, underscore),
/// punctuation/symbols, or whitespace (insignificant). Enclosing spans grow word → line →
/// document. Exported for "any", not "text": language navigators (e.g. Roslyn's) request
/// their natural-language fallback for comment/string interiors with the "any" content
/// type, which only providers declared on "any" itself can satisfy.
/// </summary>
[Export(typeof(ITextStructureNavigatorProvider))]
[ContentType("any")]
public sealed class TextStructureNavigatorProvider : ITextStructureNavigatorProvider
{
    public ITextStructureNavigator CreateTextStructureNavigator(ITextBuffer textBuffer)
    {
        ArgumentNullException.ThrowIfNull(textBuffer);
        return textBuffer.Properties.GetOrCreateSingletonProperty(() => new TextStructureNavigator(textBuffer));
    }

    private sealed class TextStructureNavigator : ITextStructureNavigator
    {
        private enum CharacterClass
        {
            Whitespace,
            Identifier,
            Punctuation,
        }

        private readonly ITextBuffer _textBuffer;

        public TextStructureNavigator(ITextBuffer textBuffer)
        {
            _textBuffer = textBuffer;
        }

        public IContentType ContentType => _textBuffer.ContentType;

        public TextExtent GetExtentOfWord(SnapshotPoint currentPosition)
        {
            var snapshot = ValidatePosition(currentPosition);
            if (snapshot.Length == 0)
            {
                return new TextExtent(new SnapshotSpan(snapshot, 0, 0), false);
            }

            var line = currentPosition.GetContainingLine();
            if (currentPosition >= line.End)
            {
                // On the line break (or buffer end): an insignificant extent covering it.
                return new TextExtent(new SnapshotSpan(line.End, line.EndIncludingLineBreak), false);
            }

            int position = currentPosition.Position;
            var characterClass = Classify(snapshot[position]);
            int start = position;
            while (start > line.Start.Position && Classify(snapshot[start - 1]) == characterClass)
            {
                start--;
            }

            int end = position + 1;
            while (end < line.End.Position && Classify(snapshot[end]) == characterClass)
            {
                end++;
            }

            return new TextExtent(
                new SnapshotSpan(snapshot, start, end - start),
                isSignificant: characterClass != CharacterClass.Whitespace);
        }

        public SnapshotSpan GetSpanOfEnclosing(SnapshotSpan activeSpan)
        {
            var snapshot = ValidateSpan(activeSpan);

            // Word → line (without break) → line (with break) → document.
            var word = GetExtentOfWord(activeSpan.Start);
            if (word.IsSignificant && word.Span.Contains(activeSpan) && word.Span != activeSpan)
            {
                return word.Span;
            }

            var line = activeSpan.Start.GetContainingLine();
            if (line.Extent.Contains(activeSpan) && line.Extent != activeSpan)
            {
                return line.Extent;
            }

            if (line.ExtentIncludingLineBreak.Contains(activeSpan) && line.ExtentIncludingLineBreak != activeSpan)
            {
                return line.ExtentIncludingLineBreak;
            }

            return new SnapshotSpan(snapshot, 0, snapshot.Length);
        }

        public SnapshotSpan GetSpanOfFirstChild(SnapshotSpan activeSpan)
        {
            ValidateSpan(activeSpan);
            var word = GetExtentOfWord(activeSpan.Start);
            return word.Span.Length < activeSpan.Length ? word.Span : activeSpan;
        }

        public SnapshotSpan GetSpanOfNextSibling(SnapshotSpan activeSpan)
        {
            var snapshot = ValidateSpan(activeSpan);
            if (activeSpan.End.Position >= snapshot.Length)
            {
                return activeSpan;
            }

            var next = GetExtentOfWord(activeSpan.End);
            if (!next.IsSignificant && next.Span.End.Position < snapshot.Length)
            {
                next = GetExtentOfWord(next.Span.End);
            }

            return next.Span;
        }

        public SnapshotSpan GetSpanOfPreviousSibling(SnapshotSpan activeSpan)
        {
            ValidateSpan(activeSpan);
            if (activeSpan.Start.Position == 0)
            {
                return activeSpan;
            }

            var previous = GetExtentOfWord(activeSpan.Start - 1);
            if (!previous.IsSignificant && previous.Span.Start.Position > 0)
            {
                previous = GetExtentOfWord(previous.Span.Start - 1);
            }

            return previous.Span;
        }

        private static CharacterClass Classify(char character)
        {
            if (char.IsWhiteSpace(character))
            {
                return CharacterClass.Whitespace;
            }

            return char.IsLetterOrDigit(character) || character == '_'
                ? CharacterClass.Identifier
                : CharacterClass.Punctuation;
        }

        private ITextSnapshot ValidatePosition(SnapshotPoint position)
        {
            if (position.Snapshot is null || position.Snapshot.TextBuffer != _textBuffer)
            {
                throw new ArgumentException("The position belongs to a different buffer.");
            }

            return position.Snapshot;
        }

        private ITextSnapshot ValidateSpan(SnapshotSpan span)
        {
            if (span.Snapshot is null || span.Snapshot.TextBuffer != _textBuffer)
            {
                throw new ArgumentException("The span belongs to a different buffer.");
            }

            return span.Snapshot;
        }
    }
}
