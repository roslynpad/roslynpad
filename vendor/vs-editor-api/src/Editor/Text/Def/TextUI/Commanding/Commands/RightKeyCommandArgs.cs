namespace Microsoft.VisualStudio.Text.Editor.Commanding.Commands
{
    public sealed class RightKeyCommandArgs : EditorCommandArgs
    {
        public RightKeyCommandArgs(ITextView textView, ITextBuffer subjectBuffer) : base(textView, subjectBuffer)
        {
        }
    }
}
