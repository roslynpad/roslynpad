namespace Microsoft.VisualStudio.Text.Editor.Commanding.Commands
{
    public sealed class PasteCommandArgs : EditorCommandArgs
    {
        public PasteCommandArgs(ITextView textView, ITextBuffer subjectBuffer) : base(textView, subjectBuffer)
        {
        }
    }
}
