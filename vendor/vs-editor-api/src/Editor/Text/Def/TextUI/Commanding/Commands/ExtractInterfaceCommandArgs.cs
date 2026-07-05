namespace Microsoft.VisualStudio.Text.Editor.Commanding.Commands
{
    public sealed class ExtractInterfaceCommandArgs : EditorCommandArgs
    {
        public ExtractInterfaceCommandArgs(ITextView textView, ITextBuffer subjectBuffer) : base(textView, subjectBuffer)
        {
        }
    }
}
