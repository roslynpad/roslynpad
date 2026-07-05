namespace Microsoft.VisualStudio.Text.Editor.Commanding.Commands
{
    public sealed class WordDeleteToStartCommandArgs : EditorCommandArgs
    {
        public WordDeleteToStartCommandArgs(ITextView textView, ITextBuffer subjectBuffer) : base(textView, subjectBuffer)
        {
        }
    }
}
