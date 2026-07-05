namespace Microsoft.VisualStudio.Text.Editor.Commanding.Commands
{
    public sealed class ToggleCompletionListFilterCommandArgs : EditorCommandArgs
    {
        public string AccessKey { get; }

        public ToggleCompletionListFilterCommandArgs(
            ITextView textView,
            ITextBuffer subjectBuffer,
            string accessKey) : base(textView, subjectBuffer)
        {
            AccessKey = accessKey;
        }
    }
}