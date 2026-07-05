namespace Microsoft.VisualStudio.Text.Editor.Commanding.Commands
{
    public sealed class UpKeyCommandArgs : EditorCommandArgs
    {
        public UpKeyCommandArgs(ITextView textView, ITextBuffer subjectBuffer) : base(textView, subjectBuffer)
        {
        }
    }
}
