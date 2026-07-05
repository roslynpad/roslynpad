namespace Microsoft.VisualStudio.Text.Editor.Commanding.Commands
{
    public sealed class WordDeleteToEndCommandArgs : EditorCommandArgs
    {
        public WordDeleteToEndCommandArgs(ITextView textView, ITextBuffer subjectBuffer) : base(textView, subjectBuffer)
        {
        }
    }
}
