namespace Microsoft.VisualStudio.Text.Editor.Commanding.Commands
{
    public sealed class ToggleCompletionModeCommandArgs : EditorCommandArgs
    {
        public ToggleCompletionModeCommandArgs(ITextView textView, ITextBuffer subjectBuffer) : base(textView, subjectBuffer)
        {
        }
    }
}
