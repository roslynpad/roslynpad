namespace Microsoft.VisualStudio.Text.Editor.Commanding.Commands
{
    public sealed class ShowNavigateMenuCommandArgs: EditorCommandArgs
    {
        public ShowNavigateMenuCommandArgs(ITextView textView, ITextBuffer subjectBuffer) : base(textView, subjectBuffer)
        {
        }
    }
}
