using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Rendering;
using Microsoft.CodeAnalysis;
using RoslynPad.Roslyn;
using TextDocument = ICSharpCode.AvalonEdit.Document.TextDocument;

namespace RoslynPad.Editor.Windows
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

        protected override IHighlighter CreateHighlighter(TextView textView, TextDocument document)
        {
            return new RoslynSemanticHighlighter(document, _documentId, _roslynHost, _highlightColors);
        }
    }
}