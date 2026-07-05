namespace Microsoft.VisualStudio.Text.Editor.Commanding.Commands
{
    public sealed class StartAutomaticOutliningCommandArgs : EditorCommandArgs
    {
        public StartAutomaticOutliningCommandArgs(ITextView textView, ITextBuffer subjectBuffer) : base(textView, subjectBuffer)
        {
        }
    }
}
