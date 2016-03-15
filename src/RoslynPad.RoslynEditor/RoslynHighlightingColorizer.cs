using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Rendering;
using RoslynPad.Roslyn;

namespace RoslynPad.RoslynEditor
{
    internal sealed class RoslynHighlightingColorizer : HighlightingColorizer
    {
        private readonly RoslynHost _roslynHost;

        public RoslynHighlightingColorizer(RoslynHost roslynHost)
        {
            _roslynHost = roslynHost;
        }

        protected override IHighlighter CreateHighlighter(TextView textView, TextDocument document)
        {
            return new RoslynSemanticHighlighter(document, _roslynHost);
        }
    }
}