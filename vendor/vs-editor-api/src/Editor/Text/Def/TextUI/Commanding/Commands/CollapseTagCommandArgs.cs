namespace Microsoft.VisualStudio.Text.Editor.Commanding.Commands
{
    public sealed class CollapseTagCommandArgs : EditorCommandArgs
    {
        public CollapseTagCommandArgs(ITextView textView, ITextBuffer subjectBuffer) : base(textView, subjectBuffer)
        {
        }
    }
}
