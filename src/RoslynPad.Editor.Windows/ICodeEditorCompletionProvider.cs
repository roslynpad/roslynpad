using System.Threading.Tasks;

namespace RoslynPad.Editor.Windows
{
    public interface ICodeEditorCompletionProvider
    {
        Task<CompletionResult> GetCompletionData(int position, char? triggerChar, bool useSignatureHelp);
    }
}