using Avalonia.Media;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Rendering;

namespace RoslynPad.Editor;

public static class AvaloniaEditExtensions
{
    public static bool IsOpen(this CompletionWindowBase window) => window?.IsEffectivelyVisible == true;
}
