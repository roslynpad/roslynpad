namespace Microsoft.VisualStudio.Text.Editor.Commanding.Commands
{
    public sealed class DocumentStartCommandArgs : EditorCommandArgs
    {
        public DocumentStartCommandArgs(ITextView textView, ITextBuffer subjectBuffer) : base(textView, subjectBuffer)
        {
        }
    }
}
