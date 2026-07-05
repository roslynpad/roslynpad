namespace Microsoft.VisualStudio.Text.Editor.Commanding.Commands
{
    public sealed class LeftKeyCommandArgs : EditorCommandArgs
    {
        public LeftKeyCommandArgs(ITextView textView, ITextBuffer subjectBuffer) : base(textView, subjectBuffer)
        {
        }
    }
}
