//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
using System;
using System.Diagnostics;
using System.IO;

namespace Microsoft.VisualStudio.Text.Implementation
{
    internal class StringRebuilderForChars : UnaryStringRebuilder
    {
        #region Private
        internal readonly char[] _content;   // Contents should be treated as immutable.

        internal static StringRebuilder Create(char[] source, int length, ILineBreaks lineBreaks)
        {
            return StringRebuilderForChars.Create(source, lineBreaks, 0, length, 0, lineBreaks.Length);
        }

        internal static StringRebuilder Create(char[] source, ILineBreaks lineBreaks, int start, int length, int lineBreaksStart, int lineBreaksLength)
        {
            Debug.Assert(length > 0);

            if (lineBreaksLength == 0)
                return new StringRebuilderForChars(source, LineBreakManager.Empty, start, length, 0, 0, source[start], source[start + length - 1]);
            else
                return new StringRebuilderForChars(source, lineBreaks, start, length, lineBreaksStart, lineBreaksLength, source[start], source[start + length - 1]);
        }

        private StringRebuilderForChars(char[] source, ILineBreaks lineBreaks, int start, int length, int lineBreaksStart, int lineBreaksLength, char first, char last)
            : base(lineBreaks, start, length, lineBreaksStart, lineBreaksLength, first, last)
        {
            _content = source;
        }
        #endregion

        public override string ToString()
        {
            return new string(_content, _textSpanStart, this.Length);
        }

        #region StringRebuilder Members

        public override char this[int index]
        {
            get
            {
                return this.GetChar(_content, index);
            }
        }

        public override string GetText(Span span)
        {
            return this.GetText(_content, span);
        }

        public override void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count)
        {
            this.CopyTo(_content, sourceIndex, destination, destinationIndex, count);
        }

        public override void Write(TextWriter writer, Span span)
        {
            this.Write(_content, writer, span);
        }

        public override StringRebuilder GetSubText(Span span)
        {
            if (span.End > this.Length)
                throw new ArgumentOutOfRangeException(nameof(span));

            if (span.Length == 0)
                return StringRebuilder.Empty;
            else if (span.Length == this.Length)
                return this;
            else
            {
                int firstLineNumber;
                int lastLineNumber;
                this.FindFirstAndLastLines(span, out firstLineNumber, out lastLineNumber);
                return StringRebuilderForChars.Create(_content, _lineBreaks, span.Start + _textSpanStart, span.Length, firstLineNumber, lastLineNumber - firstLineNumber);
            }
        }
        #endregion
    }
}
