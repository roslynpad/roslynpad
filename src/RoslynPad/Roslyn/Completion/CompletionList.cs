using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using RoslynPad.Utilities;

namespace RoslynPad.Roslyn.Completion
{
    public class CompletionList
    {
        public bool IsExclusive { get; }

        public ImmutableArray<CompletionItem> Items { get; }

        public CompletionList(object inner)
        {
            IsExclusive = inner.GetPropertyValue<bool>(nameof(IsExclusive));
            Items = ImmutableArray.CreateRange(inner.GetPropertyValue<IEnumerable<object>>(nameof(Items))
                .Select(x => new CompletionItem(x)));
        }
    }
}