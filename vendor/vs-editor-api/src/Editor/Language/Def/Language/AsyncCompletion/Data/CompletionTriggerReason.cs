namespace Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data
{
    /// <summary>
    /// Describes the kind of action that initially triggered completion to open.
    /// </summary>
    public enum CompletionTriggerReason
    {
        /// <summary>
        /// Completion was triggered by a direct invocation of the completion feature
        /// using the Edit.ListMember command.
        /// </summary>
        Invoke = 0,

        /// <summary>
        /// Completion was triggered with a request to commit if a single item would be selected
        /// using the Edit.CompleteWord command.
        /// </summary>
        InvokeAndCommitIfUnique = 1,

        /// <summary>
        /// Completion was triggered with a request to display items of matching type
        /// </summary>
        InvokeMatchingType = 2,

        /// <summary>
        /// Completion was triggered or updated via an action inserting a character into the buffer.
        /// </summary>
        Insertion = 3,

        /// <summary>
        /// Completion was triggered or updated by removing a character from the buffer using Delete.
        /// </summary>
        Deletion = 4,

        /// <summary>
        /// Completion was triggered or updated by removing a character from the buffer using Backspace.
        /// </summary>
        Backspace = 5,

        /// <summary>
        /// Completion was updated by changing filters
        /// </summary>
        FilterChange = 6,

        /// <summary>
        /// Completion was triggered by Roslyn's Snippets mode
        /// </summary>
        SnippetsMode = 7
    }
}
