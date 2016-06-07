using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;

namespace RoslynPad.Roslyn.Completion
{
    public sealed class CompletionHelper
    {
        private readonly Microsoft.CodeAnalysis.Editor.CompletionHelper _inner;

        private CompletionHelper(Microsoft.CodeAnalysis.Editor.CompletionHelper inner)
        {
            _inner = inner;
        }

        public static CompletionHelper GetHelper(Document document, CompletionService service)
        {
            return new CompletionHelper(Microsoft.CodeAnalysis.Editor.CompletionHelper.GetHelper(document, service));
        }

        public bool MatchesFilterText(CompletionItem item, string filterText, CompletionTrigger trigger)
        {
            return _inner.MatchesFilterText(item, filterText, trigger, CompletionFilterReason.TypeChar);
        }
    }
}