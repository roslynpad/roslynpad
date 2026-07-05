namespace Microsoft.VisualStudio.Text.Editor.Commanding.Commands
{
    public sealed class DownKeyCommandArgs : EditorCommandArgs
    {
        public DownKeyCommandArgs(ITextView textView, ITextBuffer subjectBuffer) : base(textView, subjectBuffer)
        {
        }
    }
}
