namespace Microsoft.VisualStudio.Text.Editor.Commanding.Commands
{
    public sealed class ReorderParametersCommandArgs : EditorCommandArgs
    {
        public ReorderParametersCommandArgs(ITextView textView, ITextBuffer subjectBuffer) : base(textView, subjectBuffer)
        {
        }
    }
}
