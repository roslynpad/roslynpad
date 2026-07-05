
namespace Microsoft.VisualStudio.Text.Editor.Commanding.Commands
{
    using System.Collections.Generic;
    using Microsoft.VisualStudio.Text.Tagging;

    /// <summary>
    /// Base <see cref="EditorCommandArgs"/> type for navigation between errors.
    /// </summary>
    public abstract class ErrorCommandArgsBase : EditorCommandArgs
    {
        /// <summary>
        /// The list of <see cref="IErrorTag.ErrorType"/> name strings of tags applicable to this command invocation.
        /// </summary>
        /// <remarks>
        /// Can be <c>null</c>, indicating that this command applies to all types of errors.
        /// </remarks>
        public IEnumerable<string> ErrorTagTypeNames { get; }

        /// <summary>
        /// Creates an instance of <see cref="ErrorCommandArgsBase"/> with a list of matching error types.
        /// </summary>
        /// <param name="textView">The <see cref="ITextView"/> upon which to invoke the command.</param>
        /// <param name="subjectBuffer">The <see cref="ITextBuffer"/> upon which to invoke the command.</param>
        /// <param name="errorTypeNames">A list of error type names to include.</param>
        /// <remarks>
        /// <paramref name="errorTypeNames"/> defaults to the set of all defined error types if not specified.
        /// </remarks>
        protected ErrorCommandArgsBase(ITextView textView, ITextBuffer subjectBuffer, IEnumerable<string> errorTypeNames)
            : base(textView, subjectBuffer)
        {
            this.ErrorTagTypeNames = errorTypeNames;
        }
    }
}
