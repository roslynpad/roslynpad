namespace Microsoft.VisualStudio.Text.Editor.Commanding.Commands
{
    public sealed class RemoveParametersCommandArgs : EditorCommandArgs
    {
        public RemoveParametersCommandArgs(ITextView textView, ITextBuffer subjectBuffer) : base(textView, subjectBuffer)
        {
        }
    }
}
