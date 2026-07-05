namespace Microsoft.VisualStudio.Text.Editor.Commanding.Commands
{
    public sealed class DocumentEndCommandArgs : EditorCommandArgs
    {
        public DocumentEndCommandArgs(ITextView textView, ITextBuffer subjectBuffer) : base(textView, subjectBuffer)
        {
        }
    }
}
