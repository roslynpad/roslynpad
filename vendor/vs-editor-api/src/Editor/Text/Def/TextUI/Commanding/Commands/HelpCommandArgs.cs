namespace Microsoft.VisualStudio.Text.Editor.Commanding.Commands
{
    public sealed class HelpCommandArgs : EditorCommandArgs
    {
        public HelpCommandArgs(ITextView textView, ITextBuffer subjectBuffer) : base(textView, subjectBuffer)
        {
        }
    }
}
