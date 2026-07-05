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
using System.Threading;

namespace Microsoft.VisualStudio.Text.Implementation
{
    internal class StringRebuilderForString : UnaryStringRebuilder
    {
        #region Private
        internal readonly string _content;

        internal static StringRebuilder Create(string source, int length, ILineBreaks lineBreaks)
        {
            return StringRebuilderForString.Create(source, lineBreaks, 0, length, 0, lineBreaks.Length);
        }

        internal static StringRebuilder Create(string source, ILineBreaks lineBreaks, int start, int length, int lineBreaksStart, int lineBreaksLength)
        {
            Debug.Assert(length != 0);

            if (lineBreaksLength == 0)
                return new StringRebuilderForString(source, LineBreakManager.Empty, start, length, 0, 0, source[start], source[start + length - 1]);
            else
                return new StringRebuilderForString(source, lineBreaks, start, length, lineBreaksStart, lineBreaksLength, source[start], source[start + length - 1]);
        }

        internal StringRebuilderForString()
            : base(LineBreakManager.Empty, 0, 0, 0, 0, '\0', '\0')
        {
            _content = String.Empty;
        }

        private StringRebuilderForString(string source, ILineBreaks lineBreaks, int start, int length, int lineBreaksStart, int lineBreaksLength, char first, char last)
            : base(lineBreaks, start, length, lineBreaksStart, lineBreaksLength, first, last)
        {
            _content = source;
        }
        #endregion

        public override string ToString()
        {
            return _content.Substring(_textSpanStart, this.Length);
        }

        #region StringRebuilder Members

        public override char this[int index]
        {
            get
            {
                if ((index < 0) || (index >= this.Length))
                    throw new ArgumentOutOfRangeException(nameof(index));

                #if DEBUG
                Interlocked.Increment(ref _totalCharactersReturned);
                #endif

                return _content[index + _textSpanStart];
            }
        }

        public override string GetText(Span span)
        {
            #if DEBUG
            Interlocked.Add(ref _totalCharactersReturned, span.Length);
            #endif

            return _content.Substring(span.Start + _textSpanStart, span.Length);
        }

        public override void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count)
        {
            #if DEBUG
            Interlocked.Add(ref _totalCharactersCopied, count);
            #endif

            _content.CopyTo(sourceIndex + _textSpanStart, destination, destinationIndex, count);
        }

        public override void Write(TextWriter writer, Span span)
        {
            if (writer == null)
                throw new ArgumentNullException(nameof(writer));

            writer.Write(this.GetText(span));
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
                return StringRebuilderForString.Create(_content, _lineBreaks, span.Start + _textSpanStart, span.Length, firstLineNumber, lastLineNumber - firstLineNumber);
            }
        }
        #endregion
    }
}
