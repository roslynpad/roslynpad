namespace Microsoft.VisualStudio.Text.Editor.Commanding.Commands
{
    public sealed class ToggleOutliningExpansionCommandArgs : EditorCommandArgs
    {
        public ToggleOutliningExpansionCommandArgs(ITextView textView, ITextBuffer subjectBuffer) : base(textView, subjectBuffer)
        {
        }
    }

    public sealed class ToggleAllOutliningCommandArgs : EditorCommandArgs
    {
        public ToggleAllOutliningCommandArgs(ITextView textView, ITextBuffer subjectBuffer) : base(textView, subjectBuffer)
        {
        }
    }

    public sealed class ToggleOutliningDefinitionsCommandArgs : EditorCommandArgs
    {
        public ToggleOutliningDefinitionsCommandArgs(ITextView textView, ITextBuffer subjectBuffer) : base(textView, subjectBuffer)
        {
        }
    }

    public sealed class ToggleOutliningEnabledCommandArgs : EditorCommandArgs
    {
        public ToggleOutliningEnabledCommandArgs(ITextView textView, ITextBuffer subjectBuffer) : base(textView, subjectBuffer)
        {
        }
    }
}