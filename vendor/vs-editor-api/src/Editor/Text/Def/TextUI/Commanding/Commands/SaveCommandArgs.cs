namespace Microsoft.VisualStudio.Text.Editor.Commanding.Commands
{
    public sealed class SaveCommandArgs : EditorCommandArgs
    {
        public SaveCommandArgs(ITextView textView, ITextBuffer subjectBuffer) : base(textView, subjectBuffer)
        {
        }
    }
}
