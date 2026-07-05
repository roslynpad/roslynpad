namespace Microsoft.VisualStudio.Text.Editor.Commanding.Commands
{
    public sealed class InsertSnippetCommandArgs : EditorCommandArgs
    {
        public InsertSnippetCommandArgs(ITextView textView, ITextBuffer subjectBuffer) : base(textView, subjectBuffer)
        {
        }
    }
}
