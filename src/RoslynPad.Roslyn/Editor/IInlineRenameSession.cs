namespace RoslynPad.Roslyn.Editor
{
    public interface IInlineRenameSession
    {
        void Cancel();

        void Commit(bool previewChanges = false);
    }
}