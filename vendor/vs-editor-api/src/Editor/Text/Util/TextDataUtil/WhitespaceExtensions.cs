using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Text.Data.Utilities
{
    internal static class WhitespaceExtensions
    {
        public const string CRLF_LITERAL = "\r\n";
        public const string CR_LITERAL = "\r";
        public const string LF_LITERAL = "\n";
        public const string NEL_LITERAL = "\u0085";
        public const string LS_LITERAL = "\u2028";
        public const string PS_LITERAL = "\u2029";

        /// <summary>
        /// Given a line, returns the kind of newline that appears at the end of the line.
        /// </summary>
        /// <param name="line">The line to inspect</param>
        /// <returns>The kind of newline appearing at the end of the line, or null if this is the last line of the document.</returns>
        public static NewlineState.LineEnding? GetLineEnding(this ITextSnapshotLine line)
        {
            if (line.LineBreakLength == 0)
            {
                return null;
            }

            if (line.LineBreakLength == 2)
            {
                return NewlineState.LineEnding.CRLF;
            }

            switch (line.Snapshot[line.End])
            {
                case '\r':
                    return NewlineState.LineEnding.CR;
                case '\n':
                    return NewlineState.LineEnding.LF;
                case '\u0085':
                    return NewlineState.LineEnding.NEL;
                case '\u2028':
                    return NewlineState.LineEnding.LS;
                case '\u2029':
                    return NewlineState.LineEnding.PS;
                default:
                    throw new ArgumentException($"Unexpected newline character {line.Snapshot[line.End]}", nameof(line));
            }
        }

        public static LeadingWhitespaceState.LineLeadingCharacter GetLeadingCharacter(this ITextSnapshotLine line)
        {
            if (line.Length == 0)
            {
                return LeadingWhitespaceState.LineLeadingCharacter.Empty;
            }

            switch (line.Snapshot[line.Start])
            {
                case ' ':
                    return LeadingWhitespaceState.LineLeadingCharacter.Space;
                case '\t':
                    return LeadingWhitespaceState.LineLeadingCharacter.Tab;
                default:
                    return LeadingWhitespaceState.LineLeadingCharacter.Printable;
            }
        }

        /// <summary>
        /// Takes a string representation of a line ending and returns the corresponding line ending.
        /// </summary>
        /// <remarks>This method will involve allocating strings, if at all posibile, use LineEndingFromSnapshotLine.</remarks>
        /// <param name="lineEndingString">A string representation of a line ending.</param>
        /// <returns>The corresponding LineEnding enumeration value. Null if the string isn't a recognized line ending.</returns>
        public static NewlineState.LineEnding? LineEndingFromString(string lineEndingString)
        {
            switch (lineEndingString)
            {
                case CRLF_LITERAL:
                    return NewlineState.LineEnding.CRLF;
                case CR_LITERAL:
                    return NewlineState.LineEnding.CR;
                case LF_LITERAL:
                    return NewlineState.LineEnding.LF;
                case NEL_LITERAL:
                    return NewlineState.LineEnding.NEL;
                case LS_LITERAL:
                    return NewlineState.LineEnding.LS;
                case PS_LITERAL:
                    return NewlineState.LineEnding.PS;
                default:
                    return null;
            }
        }

        public static string StringFromLineEnding(this NewlineState.LineEnding lineEnding)
        {
            switch (lineEnding)
            {
                case NewlineState.LineEnding.CRLF:
                    return CRLF_LITERAL;
                case NewlineState.LineEnding.CR:
                    return CR_LITERAL;
                case NewlineState.LineEnding.LF:
                    return LF_LITERAL;
                case NewlineState.LineEnding.NEL:
                    return NEL_LITERAL;
                case NewlineState.LineEnding.LS:
                    return LS_LITERAL;
                case NewlineState.LineEnding.PS:
                    return PS_LITERAL;
                default:
                    // We shouldn't have any more, just return CRLF as paranoia.
                    return CRLF_LITERAL;
            }
        }

        /// <summary>
        /// Normalizes the given buffer to match the given newline string on every line
        /// </summary>
        /// <returns>True if the buffer was changed. False otherwise.</returns>
        public static bool NormalizeNewlines(this ITextBuffer buffer, string newlineString)
        {
            using (var edit = buffer.CreateEdit())
            {
                foreach (var line in edit.Snapshot.Lines)
                {
                    if (line.LineBreakLength != 0)
                    {
                        // Calling line.GetLineBreakText allocates a string. Since we only have 1 2-character newline to worry about
                        // we can do this without that allocation by comparing characters directly.
                        if (line.LineBreakLength != newlineString.Length || edit.Snapshot[line.End] != newlineString[0])
                        {
                            // Intentionally ignore failed replaces. We'll do the best effort change here.
                            edit.Replace(new Span(line.End, line.LineBreakLength), newlineString);
                        }
                    }
                }

                if (edit.HasEffectiveChanges)
                {
                    return edit.Apply() != edit.Snapshot;
                }
                else
                {
                    // We didn't have to do anything
                    return false;
                }
            }
        }

        /// <summary>
        /// Normalizes the given buffer to match the given whitespace stype.
        /// </summary>
        /// <returns>True if the buffer was changed. False otherwise.</returns>
        public static bool NormalizeLeadingWhitespace(this ITextBuffer buffer, int tabSize, bool useSpaces)
        {
            using (var edit = buffer.CreateEdit())
            {
                var whitespaceCache = new string[100];

                foreach (var line in edit.Snapshot.Lines)
                {
                    if (line.Length > 0)
                    {
                        AnalyzeWhitespace(line, tabSize, out int whitespaceCharacterLength, out int column);
                        if (column > 0)
                        {
                            var whitespace = GetWhitespace(whitespaceCache, tabSize, useSpaces, column);

                            if ((whitespace.Length != whitespaceCharacterLength) || !ComparePrefix(line, whitespace))
                            {
                                edit.Replace(new Span(line.Start, whitespaceCharacterLength), whitespace);
                            }
                        }
                    }
                }

                return edit.Apply() != edit.Snapshot;
            }
        }

        private static void AnalyzeWhitespace(ITextSnapshotLine line, int tabSize, out int whitespaceCharacterLength, out int column)
        {
            column = 0;
            whitespaceCharacterLength = 0;
            while (whitespaceCharacterLength < line.Length)
            {
                var c = (line.Start + whitespaceCharacterLength).GetChar();
                if (c == ' ')
                    ++column;
                else if (c == '\t')
                    column = ((1 + column / tabSize) * tabSize);
                else
                    break;

                ++whitespaceCharacterLength;
            }
        }

        private static string GetWhitespace(string[] whitespaceCache, int tabSize, bool useSpaces, int column)
        {
            string whitespace;
            if ((column < whitespaceCache.Length) && (whitespaceCache[column] != null))
            {
                whitespace = whitespaceCache[column];
            }
            else
            {
                if (useSpaces)
                {
                    whitespace = new string(' ', column);
                }
                else
                {
                    whitespace = new string('\t', column / tabSize);
                    var spaces = column % tabSize;
                    if (spaces != 0)
                        whitespace += new string(' ', spaces);
                }

                if (column < whitespaceCache.Length)
                    whitespaceCache[column] = whitespace;
            }

            return whitespace;
        }

        private static bool ComparePrefix(ITextSnapshotLine line, string whitespace)
        {
            for (int i = 0; (i < whitespace.Length); ++i)
                if ((line.Start + i).GetChar() != whitespace[i])
                    return false;

            return true;
        }

        /// <summary>
        /// Normalizes the given buffer to match the given newline state's line endings if they are consistent.
        /// </summary>
        /// <returns>True if the buffer was changed. False otherwise.</returns>
        public static bool NormalizeNewlines(this NewlineState newlineState, ITextBuffer buffer)
        {
            // Keep this method in sync with the other NormalizeNewlines overload below.
            if (!newlineState.HasConsistentLineEndings || !newlineState.InferredLineEnding.HasValue)
            {
                // Right now we expect people to overwhelmingly start from project templates, item templates, or cloned code,
                // which will give them at least one newline in a given document. If they don't then we're taking the easy
                // route here, to not do anything. That could be improved upon, but we're waiting for user feedback to justify
                // further work in this area.
                return false;
            }

            /* Potential optimization, check to see if text contains any newlines that would be replaced, and if not, just return text and avoid allocations */
            string newlineString = newlineState.InferredLineEnding.Value.StringFromLineEnding();

            return buffer.NormalizeNewlines(newlineString);
        }

        public static string NormalizeNewlines(this NewlineState newlineState, string text)
        {
            // Keep this method in sync with the other NormalizeNewlines overload above
            if (!newlineState.HasConsistentLineEndings || !newlineState.InferredLineEnding.HasValue)
            {
                // Right now we expect people to overwhelmingly start from project templates, item templates, or cloned code,
                // which will give them at least one newline in a given document. If they don't then we're taking the easy
                // route here, to not do anything. That could be improved upon, but we're waiting for user feedback to justify
                // further work in this area.
                return text;
            }

            /* Potential optimization, check to see if text contains any newlines that would be replaced, and if not, just return text and avoid allocations */
            string newlineString = newlineState.InferredLineEnding.Value.StringFromLineEnding();

            // In the perverse case, where we have a string full of "\n\n\n\n\n\n" and the document wants \r\n, we can only ever double the size of the string.
            var strBuilder = new StringBuilder(text.Length * 2);

            for (int i = 0; i < text.Length; i++)
            {
                switch (text[i])
                {
                    case '\r':
                        if (i < (text.Length - 1) && text[i + 1] == '\n')
                        {
                            i++;
                        }
                        goto case '\n';
                    case '\n':
                    case '\u0085':
                    case '\u2028':
                    case '\u2029':
                        strBuilder.Append(newlineString);
                        break;
                    default:
                        strBuilder.Append(text[i]);
                        break;
                }
            }

            return strBuilder.ToString();

        }
    }
}
