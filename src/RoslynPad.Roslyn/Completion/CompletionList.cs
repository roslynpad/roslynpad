using System.Collections.Immutable;
using System.Linq;

namespace RoslynPad.Roslyn.Completion
{
    public class CompletionList
    {
        private readonly Microsoft.CodeAnalysis.Completion.CompletionList _inner;
        public bool IsExclusive => _inner.IsExclusive;

        public ImmutableArray<CompletionItem> Items { get; }

        internal CompletionList(Microsoft.CodeAnalysis.Completion.CompletionList inner)
        {
            _inner = inner;
            Items = ImmutableArray.CreateRange(inner.Items.Select(x => new CompletionItem(x)));
        }
    }
}