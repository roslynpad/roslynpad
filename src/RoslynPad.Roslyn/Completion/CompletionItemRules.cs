using Microsoft.CodeAnalysis.Text;

namespace RoslynPad.Roslyn.Completion
{
    public class CompletionItemRules
    {
        private readonly Microsoft.CodeAnalysis.Completion.CompletionItemRules _inner;

        internal CompletionItemRules(Microsoft.CodeAnalysis.Completion.CompletionItemRules inner)
        {
            _inner = inner;
        }

        public TextChange? GetTextChange(CompletionItem selectedItem, char? ch = null, string textTypedSoFar = null)
        {
            return _inner.GetTextChange(selectedItem.Inner, ch, textTypedSoFar);
        }
    }
}