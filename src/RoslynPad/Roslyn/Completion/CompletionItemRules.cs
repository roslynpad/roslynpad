using Microsoft.CodeAnalysis.Text;

namespace RoslynPad.Roslyn.Completion
{
    public class CompletionItemRules
    {
        private readonly object _inner;

        internal CompletionItemRules(object inner)
        {
            _inner = inner;
        }

        public virtual TextChange? GetTextChange(CompletionItem selectedItem, char? ch = null, string textTypedSoFar = null)
        {
            return (TextChange?)_inner.GetType().GetMethod(nameof(GetTextChange)).Invoke(_inner, new[] { selectedItem.Inner, ch, textTypedSoFar });
        }
    }
}