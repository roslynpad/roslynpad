namespace Microsoft.VisualStudio.Text.Editor.Commanding.Commands
{
    public sealed class RedoCommandArgs : EditorCommandArgs
    {
#pragma warning disable CA1051 // Do not declare visible instance fields
        public readonly int Count;
#pragma warning restore CA1051 // Do not declare visible instance fields

        public RedoCommandArgs(ITextView textView, ITextBuffer subjectBuffer, int count = 1) : base(textView, subjectBuffer)
        {
            this.Count = count;
        }
    }
}
