using System.Collections.Generic;

namespace RoslynPad.Editor;

public sealed class CompletionResult(IList<ICompletionDataEx>? completionData, IOverloadProviderEx? overloadProvider, bool useHardSelection)
{
    public bool UseHardSelection { get; } = useHardSelection;

    public IList<ICompletionDataEx>? CompletionData { get; } = completionData;

    public IOverloadProviderEx? OverloadProvider { get; } = overloadProvider;
}