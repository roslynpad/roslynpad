namespace Microsoft.VisualStudio.Text.Editor.Commanding.Commands
{
    public sealed class TabKeyCommandArgs : EditorCommandArgs
    {
        public TabKeyCommandArgs(ITextView textView, ITextBuffer subjectBuffer) : base(textView, subjectBuffer)
        {
        }
    }
}
