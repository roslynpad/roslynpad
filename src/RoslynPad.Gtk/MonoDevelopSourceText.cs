//
// MonoDevelopSourceText.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2016 Xamarin Inc. (http://xamarin.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Text;
using Microsoft.CodeAnalysis.Text;
using MonoDevelop.Core.Text;
using MonoDevelop.Ide.Editor;

namespace RoslynPad.Gtk
{
    internal sealed class MonoDevelopSourceText : SourceText
    {
        private readonly ITextSource _doc;
        private TextLineCollectionWrapper _wrapper;

        public MonoDevelopSourceText(ITextSource doc)
        {
            if (doc == null)
                throw new ArgumentNullException(nameof(doc));
            _doc = doc;
        }

        protected override TextLineCollection GetLinesCore()
        {
            var textDoc = _doc as IReadonlyTextDocument;
            if (textDoc != null)
            {
                if (_wrapper == null)
                {
                    _wrapper = new TextLineCollectionWrapper(this, textDoc);
                }
                return _wrapper;
            }
            return base.GetLinesCore();
        }

        public override void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count)
        {
            _doc.CopyTo(sourceIndex, destination, destinationIndex, count);
        }

        public override Encoding Encoding => _doc.Encoding;

        public override int Length => _doc.Length;

        public override char this[int index] => _doc.GetCharAt(index);

        private class TextLineCollectionWrapper : TextLineCollection
        {
            private readonly MonoDevelopSourceText _parent;
            private readonly IReadonlyTextDocument _textDoc;

            public TextLineCollectionWrapper(MonoDevelopSourceText parent, IReadonlyTextDocument textDoc)
            {
                _parent = parent;
                _textDoc = textDoc;
            }

            public override int Count => _textDoc.LineCount;

            public override TextLine this[int index]
            {
                get
                {
                    var line = _textDoc.GetLine(index + 1);
                    return TextLine.FromSpan(_parent, new TextSpan(line.Offset, line.Length));
                }
            }

            public override TextLine GetLineFromPosition(int position)
            {
                var line = _textDoc.GetLineByOffset(position);
                return TextLine.FromSpan(_parent, new TextSpan(line.Offset, line.Length));
            }

            public override LinePosition GetLinePosition(int position)
            {
                var loc = _textDoc.OffsetToLocation(position);
                return new LinePosition(loc.Line - 1, loc.Column - 1);
            }

            public override int IndexOf(int position)
            {
                return _textDoc.OffsetToLineNumber(position) - 1;
            }
        }
    }
}