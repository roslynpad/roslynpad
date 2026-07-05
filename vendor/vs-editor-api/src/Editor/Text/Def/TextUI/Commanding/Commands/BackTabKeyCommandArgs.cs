namespace Microsoft.VisualStudio.Text.Editor.Commanding.Commands
{
    public sealed class BackTabKeyCommandArgs : EditorCommandArgs
    {
        public BackTabKeyCommandArgs(ITextView textView, ITextBuffer subjectBuffer) : base(textView, subjectBuffer)
        {
        }
    }
}
