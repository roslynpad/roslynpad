namespace Microsoft.VisualStudio.Text.Editor.Commanding.Commands
{
    public sealed class ContractSelectionCommandArgs: EditorCommandArgs
    {
        public ContractSelectionCommandArgs(ITextView textView, ITextBuffer subjectBuffer) : base(textView, subjectBuffer)
        {
        }
    }
}
