using System.Collections.Generic;
using Microsoft.VisualStudio.Text.Data.Utilities;
using Microsoft.VisualStudio.Text.Utilities;

namespace Microsoft.VisualStudio.Text.Document
{
    internal class WhitespaceManager : IWhitespaceManager
    {
        public WhitespaceManager(ITextBuffer documentBuffer, NewlineState newlineState, LeadingWhitespaceState leadingWhitespaceState)
        {
            documentBuffer.Changed += this.OnDocumentBufferChanged;
            NewlineState = newlineState;
            LeadingWhitespaceState = leadingWhitespaceState;
        }

        public NewlineState NewlineState { get; private set; }
        public LeadingWhitespaceState LeadingWhitespaceState { get; private set; }

        private void OnDocumentBufferChanged(object sender, TextContentChangedEventArgs e)
        {
            FrugalList<Span> oldLineBreakLines = null;        // Note: these are all spans of line numbers, not character positions.
            FrugalList<Span> newLineBreakLines = null;

            FrugalList<Span> oldWhitespaceLines = null;
            FrugalList<Span> newWhitespaceLines = null;

            for (int i = 0; i < e.Changes.Count; i++)
            {
                var change = e.Changes[i];
                AddLineBreakLines(ref oldLineBreakLines, e.Before, change.OldSpan);
                AddLineBreakLines(ref newLineBreakLines, e.After, change.NewSpan);

                AddWhitespaceLines(ref oldWhitespaceLines, e.Before, change.OldSpan);
                AddWhitespaceLines(ref newWhitespaceLines, e.After, change.NewSpan);
            }

            this.UpdateNewLines(e.Before, oldLineBreakLines, -1);
            this.UpdateNewLines(e.After, newLineBreakLines, 1);

            this.UpdateWhitespace(e.Before, oldWhitespaceLines, -1);
            this.UpdateWhitespace(e.After, newWhitespaceLines, 1);
        }

        // Add the range of line numbers on snapshot whose line endings might be affected by a change to span.
        private static void AddLineBreakLines(ref FrugalList<Span> lineBreakLines, ITextSnapshot snapshot, Span span)
        {
            var startLine = snapshot.GetLineFromPosition(span.Start);
            var endLine = (span.End < startLine.EndIncludingLineBreak) ? startLine : snapshot.GetLineFromPosition(span.End);

            // Extend the range if the span starts at the start of a line (since it could affect the line break of the previous line)
            // or touches the line break at the end of the line).
            int startLineNumber = ((span.Start == startLine.Start) && (span.Start != 0)) ? (startLine.LineNumber - 1) : startLine.LineNumber;
            int endLineNumber = (span.End < endLine.End) ? endLine.LineNumber : (endLine.LineNumber + 1);

            AddSpanToLines(ref lineBreakLines, startLineNumber, endLineNumber);
        }

        // Add the range of line numbers on snapshot whose leading whitespace might be affected by a change to span.
        private static void AddWhitespaceLines(ref FrugalList<Span> whitespaceLines, ITextSnapshot snapshot, Span span)
        {
            var startLine = snapshot.GetLineFromPosition(span.Start);
            var endLine = (span.End < startLine.EndIncludingLineBreak) ? startLine : snapshot.GetLineFromPosition(span.End);

            // Changes that don't start at the beginning of a line can't affect the starting character of that line.
            int startLineNumber = (span.Start == startLine.Start) ? startLine.LineNumber : (startLine.LineNumber + 1);
            int endLineNumber = endLine.LineNumber + 1;

            AddSpanToLines(ref whitespaceLines, startLineNumber, endLineNumber);
        }

        private static void AddSpanToLines(ref FrugalList<Span> lines, int startLineNumber, int endLineNumber)
        {
            if (startLineNumber != endLineNumber)
            {
                if (lines == null)
                    lines = new FrugalList<Span>();

                lines.Add(Span.FromBounds(startLineNumber, endLineNumber));
            }
        }

        private void UpdateNewLines(ITextSnapshot snapshot, FrugalList<Span> lineSpans, int delta)
        {
            if (lineSpans != null)
            {
                var collection = (lineSpans.Count == 1) ? ((IReadOnlyList<Span>)lineSpans) : new NormalizedSpanCollection(lineSpans);
                for (int i = 0; (i < collection.Count); ++i)
                {
                    Span lineSpan = collection[i];
                    for (int line = lineSpan.Start; (line < lineSpan.End); ++line)
                    {
                        ITextSnapshotLine snapshotLine = snapshot.GetLineFromLineNumber(line);
                        var state = snapshotLine.GetLineEnding();
                        if (state.HasValue)
                            this.NewlineState.Increment(state.Value, delta);
                    }
                }
            }
        }

        private void UpdateWhitespace(ITextSnapshot snapshot, FrugalList<Span> lineSpans, int delta)
        {
            if (lineSpans != null)
            {
                var collection = (lineSpans.Count == 1) ? ((IReadOnlyList<Span>)lineSpans) : new NormalizedSpanCollection(lineSpans);
                for (int i = 0; (i < collection.Count); ++i)
                {
                    Span lineSpan = collection[i];
                    for (int line = lineSpan.Start; (line < lineSpan.End); ++line)
                    {
                        ITextSnapshotLine snapshotLine = snapshot.GetLineFromLineNumber(line);
                        var state = snapshotLine.GetLeadingCharacter();
                        this.LeadingWhitespaceState.Increment(state, delta);
                    }
                }
            }
        }
    }
}
