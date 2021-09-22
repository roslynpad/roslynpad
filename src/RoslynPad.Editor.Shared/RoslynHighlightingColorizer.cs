#if AVALONIA
using AvaloniaEdit.Highlighting;
using AvaloniaEdit.Rendering;
using TextDocument = AvaloniaEdit.Document.TextDocument;
#else
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Rendering;
using TextDocument = ICSharpCode.AvalonEdit.Document.TextDocument;
#endif
using Microsoft.CodeAnalysis;
using RoslynPad.Roslyn;

namespace RoslynPad.Editor
{
    public sealed class RoslynHighlightingColorizer : HighlightingColorizer
    {
        private readonly DocumentId _documentId;
        private readonly IRoslynHost _roslynHost;
        private readonly IClassificationHighlightColors _highlightColors;

        public RoslynHighlightingColorizer(DocumentId documentId, IRoslynHost roslynHost, IClassificationHighlightColors highlightColors)
        {
            _documentId = documentId;
            _roslynHost = roslynHost;
            _highlightColors = highlightColors;
        }

        protected override IHighlighter CreateHighlighter(TextView textView, TextDocument document) =>
            new RoslynSemanticHighlighter(textView, document, _documentId, _roslynHost, _highlightColors);
    }
}
