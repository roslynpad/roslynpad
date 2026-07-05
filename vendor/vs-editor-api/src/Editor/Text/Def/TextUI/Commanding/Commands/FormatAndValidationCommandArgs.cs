namespace Microsoft.VisualStudio.Text.Editor.Commanding.Commands
{
    public sealed class FormatAndValidationCommandArgs : EditorCommandArgs
    {
        public FormatAndValidationCommandArgs(ITextView textView, ITextBuffer subjectBuffer) : base(textView, subjectBuffer)
        {
        }
    }

}
