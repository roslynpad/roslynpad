using AvaloniaEdit.CodeCompletion;

namespace RoslynPad.Editor;

public static class AvaloniaEditExtensions
{
    public static bool IsOpen(this CompletionWindowBase window) => window?.IsEffectivelyVisible == true;
}
