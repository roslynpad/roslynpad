namespace Microsoft.VisualStudio.Text.Editor.Commanding.Commands
{
    public sealed class NavigateToPreviousHighlightedReferenceCommandArgs : EditorCommandArgs
    {
        public NavigateToPreviousHighlightedReferenceCommandArgs(ITextView textView, ITextBuffer subjectBuffer) : base(textView, subjectBuffer)
        {
        }
    }
}
