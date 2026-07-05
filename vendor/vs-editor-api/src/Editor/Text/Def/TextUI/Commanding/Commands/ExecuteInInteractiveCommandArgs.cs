namespace Microsoft.VisualStudio.Text.Editor.Commanding.Commands
{
    public sealed class ExecuteInInteractiveCommandArgs : EditorCommandArgs
    {
        public ExecuteInInteractiveCommandArgs(ITextView textView, ITextBuffer subjectBuffer) : base(textView, subjectBuffer)
        {
        }
    }
}
