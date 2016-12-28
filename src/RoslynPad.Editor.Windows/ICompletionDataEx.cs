using ICSharpCode.AvalonEdit.CodeCompletion;

namespace RoslynPad.Editor.Windows
{
    public interface ICompletionDataEx : ICompletionData
    {
        bool IsSelected { get; }

        string SortText { get; }
    }

    public interface IOverloadProviderEx : IOverloadProvider
    {
        void Refresh();
    }
}