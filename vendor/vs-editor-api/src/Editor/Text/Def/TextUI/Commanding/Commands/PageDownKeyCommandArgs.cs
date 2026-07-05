namespace Microsoft.VisualStudio.Text.Editor.Commanding.Commands
{
    public sealed class PageDownKeyCommandArgs : EditorCommandArgs
    {
        public PageDownKeyCommandArgs(ITextView textView, ITextBuffer subjectBuffer) : base(textView, subjectBuffer)
        {
        }
    }
}
