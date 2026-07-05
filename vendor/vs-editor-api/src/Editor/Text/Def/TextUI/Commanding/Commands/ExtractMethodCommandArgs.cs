namespace Microsoft.VisualStudio.Text.Editor.Commanding.Commands
{
    public sealed class ExtractMethodCommandArgs : EditorCommandArgs
    {
        public ExtractMethodCommandArgs(ITextView textView, ITextBuffer subjectBuffer) : base(textView, subjectBuffer)
        {
        }
    }
}
