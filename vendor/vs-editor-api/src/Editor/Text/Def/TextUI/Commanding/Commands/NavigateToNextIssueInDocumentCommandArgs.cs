namespace Microsoft.VisualStudio.Text.Editor.Commanding.Commands
{
    using System.Collections.Generic;
    using Microsoft.VisualStudio.Text.Adornments;
    using Microsoft.VisualStudio.Text.Tagging;

    /// <summary>
    /// <see cref="EditorCommandArgs"/> for next error command.
    /// </summary>
    public sealed class NavigateToNextIssueInDocumentCommandArgs : ErrorCommandArgsBase
    {
        /// <summary>
        /// Creates an instance of <see cref="NavigateToNextIssueInDocumentCommandArgs"/> with a list of error types.
        /// </summary>
        /// <param name="textView">The <see cref="ITextView"/> upon which to invoke the command.</param>
        /// <param name="subjectBuffer">The <see cref="ITextBuffer"/> upon which to invoke the command.</param>
        public NavigateToNextIssueInDocumentCommandArgs(ITextView textView, ITextBuffer subjectBuffer) : this(textView, subjectBuffer, null)
        {
        }

        /// <summary>
        /// Creates an instance of <see cref="NavigateToNextIssueInDocumentCommandArgs"/> with a list of error types.
        /// </summary>
        /// <param name="textView">The <see cref="ITextView"/> upon which to invoke the command.</param>
        /// <param name="subjectBuffer">The <see cref="ITextBuffer"/> upon which to invoke the command.</param>
        /// <param name="errorTypeNames">A list of error type names to include.</param>
        /// <remarks>
        /// <paramref name="errorTypeNames"/> defaults to the set of all defined error types if not specified.
        /// </remarks>
        public NavigateToNextIssueInDocumentCommandArgs(ITextView textView, ITextBuffer subjectBuffer, IEnumerable<string> errorTypeNames)
            : base(textView, subjectBuffer, errorTypeNames)
        {
        }
    }
}
