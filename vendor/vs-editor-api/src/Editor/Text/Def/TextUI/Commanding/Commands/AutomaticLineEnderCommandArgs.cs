namespace Microsoft.VisualStudio.Text.Editor.Commanding.Commands
{
    public sealed class AutomaticLineEnderCommandArgs : EditorCommandArgs
    {
        public AutomaticLineEnderCommandArgs(ITextView textView, ITextBuffer subjectBuffer) : base(textView, subjectBuffer)
        {
        }
    }
}
