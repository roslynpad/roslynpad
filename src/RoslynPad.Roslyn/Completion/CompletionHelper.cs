using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using System.Globalization;

namespace RoslynPad.Roslyn.Completion
{
    public sealed class CompletionHelper
    {
        private readonly Microsoft.CodeAnalysis.Completion.CompletionHelper _inner;

        private CompletionHelper(Microsoft.CodeAnalysis.Completion.CompletionHelper inner)
        {
            _inner = inner;
        }

        public static CompletionHelper GetHelper(Document document, CompletionService service)
        {
            return new CompletionHelper(Microsoft.CodeAnalysis.Completion.CompletionHelper.GetHelper(document));
        }

        public bool MatchesFilterText(CompletionItem item, string filterText)
        {
            return _inner.MatchesPattern(item.FilterText, filterText, CultureInfo.InvariantCulture);
        }
    }
}