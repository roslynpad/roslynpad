namespace Microsoft.VisualStudio.Text.Editor.Commanding.Commands
{
    public sealed class SelectAllCommandArgs : EditorCommandArgs
    {
        public SelectAllCommandArgs(ITextView textView, ITextBuffer subjectBuffer) : base(textView, subjectBuffer)
        {
        }
    }
}
