namespace Microsoft.VisualStudio.Text.Editor.Commanding.Commands
{
    public sealed class NavigateToNextHighlightedReferenceCommandArgs : EditorCommandArgs
    {
        public NavigateToNextHighlightedReferenceCommandArgs(ITextView textView, ITextBuffer subjectBuffer) : base(textView, subjectBuffer)
        {
        }
    }
}
