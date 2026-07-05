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
    internal class StringRebuilderForCompressedChars : UnaryStringRebuilder
    {
        private readonly Page _content;

        internal static StringRebuilder Create(Page content, ILineBreaks lineBreaks)
        {
            return StringRebuilderForCompressedChars.Create(content, lineBreaks, 0, content.Length, 0, lineBreaks.Length);
        }

        private static StringRebuilder Create(Page content, ILineBreaks lineBreaks, int start, int length, int lineBreaksStart, int linebreaksLength)
        {
            Debug.Assert(content.Length > 0);

            var expanded = content.Expand();

            return new StringRebuilderForCompressedChars(content, lineBreaks, start, length, lineBreaksStart, linebreaksLength, expanded[start], expanded[start + length - 1]);
        }

        private StringRebuilderForCompressedChars(Page content, ILineBreaks lineBreaks, int start, int length, int lineBreaksStart, int linebreaksLength, char first, char last)
            : base(lineBreaks, start, length, lineBreaksStart, linebreaksLength, first, last)
        {
            _content = content;
        }

        #region StringRebuilder Members
        public override char this[int index]
        {
            get
            {
                return this.GetChar(_content.Expand(), index);
            }
        }

        public override string GetText(Span span)
        {
            return this.GetText(_content.Expand(), span);
        }

        public override void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count)
        {
            this.CopyTo(_content.Expand(), sourceIndex, destination, destinationIndex, count);
        }

        public override void Write(TextWriter writer, Span span)
        {
            this.Write(_content.Expand(), writer, span);
        }

        public override StringRebuilder GetSubText(Span span)
        {
            if (span.End > this.Length)
                throw new ArgumentOutOfRangeException(nameof(span));

            if (span.Length == this.Length)
                return this;
            else if (span.Length == 0)
                return StringRebuilder.Empty;
            else
            {
                int firstLineNumber;
                int lastLineNumber;
                this.FindFirstAndLastLines(span, out firstLineNumber, out lastLineNumber);

                return StringRebuilderForCompressedChars.Create(_content, _lineBreaks, span.Start + _textSpanStart, span.Length, firstLineNumber, lastLineNumber - firstLineNumber);
            }
        }
        #endregion
    }
}
