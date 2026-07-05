namespace Microsoft.VisualStudio.Text.Editor.Commanding.Commands
{
    public sealed class FormatDocumentCommandArgs : EditorCommandArgs
    {
        public FormatDocumentCommandArgs(ITextView textView, ITextBuffer subjectBuffer) : base(textView, subjectBuffer)
        {
        }
    }
}
