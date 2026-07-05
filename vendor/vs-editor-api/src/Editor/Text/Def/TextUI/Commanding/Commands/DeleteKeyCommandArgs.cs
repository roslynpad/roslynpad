namespace Microsoft.VisualStudio.Text.Editor.Commanding.Commands
{
    public sealed class DeleteKeyCommandArgs : EditorCommandArgs
    {
        public DeleteKeyCommandArgs(ITextView textView, ITextBuffer subjectBuffer) : base(textView, subjectBuffer)
        {
        }
    }
}
