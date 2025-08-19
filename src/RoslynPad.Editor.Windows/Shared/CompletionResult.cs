namespace RoslynPad.Editor;

public sealed class CompletionResult(IReadOnlyList<ICompletionDataEx>? completionData, IOverloadProviderEx? overloadProvider, bool useHardSelection)
{
    public bool UseHardSelection { get; } = useHardSelection;

    public IReadOnlyList<ICompletionDataEx>? CompletionData { get; } = completionData;

    public IOverloadProviderEx? OverloadProvider { get; } = overloadProvider;
}
