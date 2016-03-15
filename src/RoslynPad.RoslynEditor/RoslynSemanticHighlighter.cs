// Copyright (c) 2014 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Text;
using RoslynPad.Editor;
using RoslynPad.Roslyn;

namespace RoslynPad.RoslynEditor
{
    internal sealed class RoslynSemanticHighlighter : IHighlighter
    {
        private readonly IDocument _document;
        private readonly RoslynHost _roslynHost;
        private readonly List<CachedLine> _cachedLines;

        private bool _inHighlightingGroup;
        private int _lineNumber;
        private HighlightedLine _line;

        public RoslynSemanticHighlighter(IDocument document, RoslynHost roslynHost)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));
            _document = document;
            _roslynHost = roslynHost;

            if (document is TextDocument)
            {
                // Use the cache only for the live AvalonEdit document
                // Highlighting in read-only documents (e.g. search results) does
                // not need the cache as it does not need to highlight the same line multiple times
                _cachedLines = new List<CachedLine>();
            }
        }

        public void Dispose()
        {
        }

        public event HighlightingStateChangedEventHandler HighlightingStateChanged;

        // ReSharper disable once UnusedMember.Local
        private void OnHighlightingStateChanged(int fromLineNumber, int toLineNumber)
        {
            HighlightingStateChanged?.Invoke(fromLineNumber, toLineNumber);
        }

        IDocument IHighlighter.Document => _document;

        IEnumerable<HighlightingColor> IHighlighter.GetColorStack(int lineNumber)
        {
            return null;
        }

        void IHighlighter.UpdateHighlightingState(int lineNumber)
        {
        }

        public HighlightedLine HighlightLine(int lineNumber)
        {
            IDocumentLine documentLine = _document.GetLineByNumber(lineNumber);
            ITextSourceVersion newVersion = _document.Version;
            CachedLine cachedLine = null;
            if (_cachedLines != null)
            {
                for (int i = 0; i < _cachedLines.Count; i++)
                {
                    if (_cachedLines[i].DocumentLine == documentLine)
                    {
                        if (newVersion == null || !newVersion.BelongsToSameDocumentAs(_cachedLines[i].OldVersion))
                        {
                            // cannot list changes from old to new: we can't update the cache, so we'll remove it
                            _cachedLines.RemoveAt(i);
                        }
                        else {
                            cachedLine = _cachedLines[i];
                        }
                        break;
                    }
                }

                if (cachedLine != null && cachedLine.IsValid && newVersion.CompareAge(cachedLine.OldVersion) == 0)
                {
                    // the file hasn't changed since the cache was created, so just reuse the old highlighted line
                    return cachedLine.HighlightedLine;
                }
            }

            bool wasInHighlightingGroup = _inHighlightingGroup;
            if (!_inHighlightingGroup)
            {
                BeginHighlighting();
            }
            try
            {
                return DoHighlightLine(lineNumber, documentLine).GetAwaiter().GetResult();
            }
            finally
            {
                _line = null;
                if (!wasInHighlightingGroup)
                    EndHighlighting();
            }
        }

        private async Task<HighlightedLine> DoHighlightLine(int lineNumber, IDocumentLine documentLine)
        {
            _line = new HighlightedLine(_document, documentLine);

            var spans = await Classifier.GetClassifiedSpansAsync(_roslynHost.CurrentDocument, 
                new TextSpan(documentLine.Offset, documentLine.TotalLength), CancellationToken.None).ConfigureAwait(true);

            foreach (var classifiedSpan in spans)
            {
                if (classifiedSpan.TextSpan.Start > documentLine.EndOffset ||
                    classifiedSpan.TextSpan.End > documentLine.EndOffset)
                {
                    // TODO: this shouldn't happen, but the Roslyn document and AvalonEdit's somehow get out of sync
                    continue;
                }
                _line.Sections.Add(new HighlightedSection
                {
                    Color = ClassificationHighlightColors.GetColor(classifiedSpan.ClassificationType),
                    Offset = classifiedSpan.TextSpan.Start,
                    Length = classifiedSpan.TextSpan.Length
                });
            }

            _lineNumber = lineNumber;

            if (_cachedLines != null && _document.Version != null)
            {
                _cachedLines.Add(new CachedLine(_line, _document.Version));
            }
            return _line;
        }

        internal void Colorize(TextLocation start, TextLocation end, HighlightingColor color)
        {
            if (color == null)
                return;
            if (start.Line <= _lineNumber && end.Line >= _lineNumber)
            {
                int lineStartOffset = _line.DocumentLine.Offset;
                int lineEndOffset = lineStartOffset + _line.DocumentLine.Length;
                int startOffset = lineStartOffset + (start.Line == _lineNumber ? start.Column - 1 : 0);
                int endOffset = lineStartOffset + (end.Line == _lineNumber ? end.Column - 1 : _line.DocumentLine.Length);
                // For some parser errors, the mcs parser produces grossly wrong locations (e.g. miscounting the number of newlines),
                // so we need to coerce the offsets to valid values within the line
                startOffset = startOffset.CoerceValue(lineStartOffset, lineEndOffset);
                endOffset = endOffset.CoerceValue(lineStartOffset, lineEndOffset);
                if (_line.Sections.Count > 0)
                {
                    HighlightedSection prevSection = _line.Sections.Last();
                    if (startOffset < prevSection.Offset + prevSection.Length)
                    {
                        // The mcs parser sometimes creates strange ASTs with duplicate nodes
                        // when there are syntax errors (e.g. "int A() public static void Main() {}"),
                        // so we'll silently ignore duplicate colorization.
                        return;
                        //throw new InvalidOperationException("Cannot create unordered highlighting section");
                    }
                }
                _line.Sections.Add(new HighlightedSection
                {
                    Offset = startOffset,
                    Length = endOffset - startOffset,
                    Color = color
                });
            }
        }

        HighlightingColor IHighlighter.DefaultTextColor => ClassificationHighlightColors.DefaultColor;

        public void BeginHighlighting()
        {
            if (_inHighlightingGroup)
                throw new InvalidOperationException();
            _inHighlightingGroup = true;
        }

        public void EndHighlighting()
        {
            _inHighlightingGroup = false;
            // TODO use this to remove cached lines which are no longer visible
            //			var visibleDocumentLines = new HashSet<IDocumentLine>(syntaxHighlighter.GetVisibleDocumentLines());
            //			cachedLines.RemoveAll(c => !visibleDocumentLines.Contains(c.DocumentLine));
        }

        public HighlightingColor GetNamedColor(string name) => null;

        #region Caching
        // If a line gets edited and we need to display it while no parse information is ready for the
        // changed file, the line would flicker (semantic highlightings disappear temporarily).
        // We avoid this issue by storing the semantic highlightings and updating them on document changes
        // (using anchor movement)
        private class CachedLine
        {
            public readonly HighlightedLine HighlightedLine;
            public readonly ITextSourceVersion OldVersion;

            /// <summary>
            /// Gets whether the cache line is valid (no document changes since it was created).
            /// This field gets set to false when Update() is called.
            /// </summary>
            public readonly bool IsValid;

            public IDocumentLine DocumentLine => HighlightedLine.DocumentLine;

            public CachedLine(HighlightedLine highlightedLine, ITextSourceVersion fileVersion)
            {
                if (highlightedLine == null)
                    throw new ArgumentNullException(nameof(highlightedLine));
                if (fileVersion == null)
                    throw new ArgumentNullException(nameof(fileVersion));

                HighlightedLine = highlightedLine;
                OldVersion = fileVersion;
                IsValid = true;
            }
        }
        
        #endregion
    }
}