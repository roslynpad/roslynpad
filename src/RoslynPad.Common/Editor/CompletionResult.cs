using System.Collections.Generic;
using ICSharpCode.AvalonEdit.CodeCompletion;

namespace RoslynPad.Editor
{
    public sealed class CompletionResult
    {
        public CompletionResult(IList<ICompletionDataEx> completionData, IOverloadProvider overloadProvider)
        {
            CompletionData = completionData;
            OverloadProvider = overloadProvider;
        }

        public IList<ICompletionDataEx> CompletionData { get; private set; }

        public IOverloadProvider OverloadProvider { get; private set; }
    }
}