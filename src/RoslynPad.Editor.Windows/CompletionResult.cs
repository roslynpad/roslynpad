using System.Collections.Generic;

namespace RoslynPad.Editor.Windows
{
    public sealed class CompletionResult
    {
        public CompletionResult(IList<ICompletionDataEx> completionData, IOverloadProviderEx overloadProvider)
        {
            CompletionData = completionData;
            OverloadProvider = overloadProvider;
        }

        public IList<ICompletionDataEx> CompletionData { get; private set; }

        public IOverloadProviderEx OverloadProvider { get; private set; }
    }
}