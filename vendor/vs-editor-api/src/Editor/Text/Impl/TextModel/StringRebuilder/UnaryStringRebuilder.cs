//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace Microsoft.VisualStudio.Text.Implementation
{
    internal abstract class UnaryStringRebuilder : StringRebuilder
    {
        internal readonly ILineBreaks _lineBreaks;

        #if DEBUG
        private static int _totalCreated = 0;
        public static int TotalCreated { get { return _totalCreated; } }
        #endif

        protected readonly int _textSpanStart;         //subspan of _storage contained in this StringRebuilderForChars
        protected readonly int _lineBreakSpanStart;    //subspan of _storage.LineBreaks that contains all line breaks in this StringRebuilderForChars
        protected int TextSpanEnd { get { return _textSpanStart + this.Length; } }
        protected int LineBreakSpanEnd { get { return _lineBreakSpanStart + this.LineBreakCount; } }

        protected UnaryStringRebuilder(ILineBreaks lineBreaks, int start, int length, int linebreaksStart, int linebreaksLength, char first, char last)
                : base(length, linebreaksLength, first, last)
        {
            #if DEBUG
            Interlocked.Increment(ref _totalCreated);
            #endif

            _lineBreaks = lineBreaks;

            _textSpanStart = start;
            _lineBreakSpanStart = linebreaksStart;
        }

        internal void FindFirstAndLastLines(Span span, out int firstLineNumber, out int lastLineNumber)
        {
            firstLineNumber = this.GetLineNumberFromPosition(span.Start) + _lineBreakSpanStart;
            lastLineNumber = this.GetLineNumberFromPosition(span.End) + _lineBreakSpanStart;

            //Handle the special case where the end position falls in the middle of a linebreak.
            if ((lastLineNumber < this.LineBreakSpanEnd) &&
                (span.End > _lineBreaks.StartOfLineBreak(lastLineNumber) - _textSpanStart))
            {
                ++lastLineNumber;
            }
        }

        #region StringRebuilder Members
        public override int GetLineNumberFromPosition(int position)
        {
            if ((position < 0) || (position > this.Length))
                throw new ArgumentOutOfRangeException(nameof(position));

            //Convert position to a position relative to the start of _text.
            if (position == this.Length)
            {
                //Handle positions at the end of the span as a special case since otherwise we
                //return the incorrect value if the last line break extends past the end of _textSpan.
                return this.LineBreakCount;
            }

            position += _textSpanStart;

            int start = _lineBreakSpanStart;
            int end = this.LineBreakSpanEnd;

            while (start < end)
            {
                int middle = (start + end) / 2;
                if (position < _lineBreaks.EndOfLineBreak(middle))
                    end = middle;
                else
                    start = middle + 1;
            }

            return start - _lineBreakSpanStart;
        }

        public override void GetLineFromLineNumber(int lineNumber, out Span extent, out int lineBreakLength)
        {
            if ((lineNumber < 0) || (lineNumber > this.LineBreakCount))
                throw new ArgumentOutOfRangeException(nameof(lineNumber));

            int absoluteLineNumber = _lineBreakSpanStart + lineNumber;

            int start = (lineNumber == 0)
                        ? 0
                        : (Math.Min(this.TextSpanEnd, _lineBreaks.EndOfLineBreak(absoluteLineNumber - 1)) - _textSpanStart);

            int end;
            if (lineNumber < this.LineBreakCount)
            {
                end = Math.Max(_textSpanStart, _lineBreaks.StartOfLineBreak(absoluteLineNumber));
                lineBreakLength = Math.Min(this.TextSpanEnd, _lineBreaks.EndOfLineBreak(absoluteLineNumber)) - end;

                end -= _textSpanStart;
            }
            else
            {
                end = this.Length;
                lineBreakLength = 0;
            }

            extent = Span.FromBounds(start, end);

        }

        public override StringRebuilder GetLeaf(int position, out int offset)
        {
            offset = 0;
            return this;
        }

        protected char GetChar(char[] content, int index)
        {
            if ((index < 0) || (index >= this.Length))
                throw new ArgumentOutOfRangeException(nameof(index));

            #if DEBUG
            Interlocked.Increment(ref _totalCharactersReturned);
            #endif

            return content[index + _textSpanStart];
        }

        protected string GetText(char[] content, Span span)
        {
            if (span.End > this.Length)
                throw new ArgumentOutOfRangeException(nameof(span));

            #if DEBUG
            Interlocked.Add(ref _totalCharactersReturned, span.Length);
            #endif

            return new string(content, span.Start + _textSpanStart, span.Length);
        }

        protected void CopyTo(char[] content, int sourceIndex, char[] destination, int destinationIndex, int count)
        {
            if (sourceIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(sourceIndex));
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));
            if (destinationIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(destinationIndex));
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            if ((sourceIndex + count > this.Length) || (sourceIndex + count < 0))
                throw new ArgumentOutOfRangeException(nameof(count));

            if ((destinationIndex + count > destination.Length) || (destinationIndex + count < 0))
                throw new ArgumentOutOfRangeException(nameof(count));

            #if DEBUG
            Interlocked.Add(ref _totalCharactersCopied, count);
            #endif

            Array.Copy(content, sourceIndex + _textSpanStart, destination, destinationIndex, count);
        }

        protected void Write(char[] content, TextWriter writer, Span span)
        {
            if (writer == null)
                throw new ArgumentNullException(nameof(writer));
            if (span.End > this.Length)
                throw new ArgumentOutOfRangeException(nameof(span));

            writer.Write(content, span.Start + _textSpanStart, span.Length);
        }

        public override StringRebuilder Child(bool rightSide)
        {
            throw new InvalidOperationException();
        }
        #endregion
    }
}
