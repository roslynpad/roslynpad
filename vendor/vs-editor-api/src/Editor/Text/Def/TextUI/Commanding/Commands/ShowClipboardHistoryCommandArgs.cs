namespace Microsoft.VisualStudio.Text.Editor.Commanding.Commands
{
    public sealed class ShowClipboardHistoryCommandArgs: EditorCommandArgs
    {
        public ShowClipboardHistoryCommandArgs(ITextView textView, ITextBuffer subjectBuffer) : base(textView, subjectBuffer)
        {
        }
    }
}
