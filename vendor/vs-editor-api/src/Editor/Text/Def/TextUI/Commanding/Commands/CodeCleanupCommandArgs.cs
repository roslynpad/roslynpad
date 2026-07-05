namespace Microsoft.VisualStudio.Text.Editor.Commanding.Commands
{
    public sealed class CodeCleanUpCommandArgs : EditorCommandArgs
    {
        public CodeCleanUpCommandArgs(ITextView textView, ITextBuffer subjectBuffer) : base(textView, subjectBuffer)
        {
        }
    }
}
