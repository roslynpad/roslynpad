namespace Microsoft.VisualStudio.Text.Editor.Commanding.Commands
{
    public sealed class PageUpKeyCommandArgs : EditorCommandArgs
    {
        public PageUpKeyCommandArgs(ITextView textView, ITextBuffer subjectBuffer) : base(textView, subjectBuffer)
        {
        }
    }
}
