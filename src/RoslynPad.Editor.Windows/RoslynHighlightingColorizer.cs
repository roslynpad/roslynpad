using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Rendering;
using Microsoft.CodeAnalysis;
using RoslynPad.Roslyn;
using TextDocument = ICSharpCode.AvalonEdit.Document.TextDocument;

namespace RoslynPad.Editor.Windows
{
    internal sealed class RoslynHighlightingColorizer : HighlightingColorizer
    {
        private readonly DocumentId _documentId;
        private readonly RoslynHost _roslynHost;

        public RoslynHighlightingColorizer(DocumentId documentId, RoslynHost roslynHost)
        {
            _documentId = documentId;
            _roslynHost = roslynHost;
        }

        protected override IHighlighter CreateHighlighter(TextView textView, TextDocument document)
        {
            return new RoslynSemanticHighlighter(document, _documentId, _roslynHost);
        }
    }
}