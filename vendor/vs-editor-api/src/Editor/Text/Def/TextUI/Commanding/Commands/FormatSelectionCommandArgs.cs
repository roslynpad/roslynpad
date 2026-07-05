namespace Microsoft.VisualStudio.Text.Editor.Commanding.Commands
{
    public sealed class FormatSelectionCommandArgs : EditorCommandArgs
    {
        public FormatSelectionCommandArgs(ITextView textView, ITextBuffer subjectBuffer) : base(textView, subjectBuffer)
        {
        }
    }
}
