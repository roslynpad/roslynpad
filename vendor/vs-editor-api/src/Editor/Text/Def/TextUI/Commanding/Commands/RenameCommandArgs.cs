namespace Microsoft.VisualStudio.Text.Editor.Commanding.Commands
{
    public sealed class RenameCommandArgs : EditorCommandArgs
    {
        public RenameCommandArgs(ITextView textView, ITextBuffer subjectBuffer) : base(textView, subjectBuffer)
        {
        }
    }
}
