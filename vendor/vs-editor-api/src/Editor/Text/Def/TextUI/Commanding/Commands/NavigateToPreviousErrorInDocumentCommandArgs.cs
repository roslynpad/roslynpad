namespace Microsoft.VisualStudio.Text.Editor.Commanding.Commands
{
    using Microsoft.VisualStudio.Text.Adornments;

    /// <summary>
    /// <see cref="EditorCommandArgs"/> for next error command.
    /// </summary>
    public sealed class NavigateToPreviousErrorInDocumentCommandArgs : ErrorCommandArgsBase
    {
        /// <summary>
        /// Creates an instance of <see cref="NavigateToPreviousErrorInDocumentCommandArgs"/> with a list
        /// that contains only errors (exlucdes hinted suggestions and warnings). 
        /// </summary>
        /// <param name="textView">The <see cref="ITextView"/> upon which to invoke the command.</param>
        /// <param name="subjectBuffer">The <see cref="ITextBuffer"/> upon which to invoke the command.</param>
        public NavigateToPreviousErrorInDocumentCommandArgs(ITextView textView, ITextBuffer subjectBuffer)
            : base(textView, subjectBuffer, new [] { PredefinedErrorTypeNames.CompilerError, PredefinedErrorTypeNames.OtherError, PredefinedErrorTypeNames.SyntaxError })
        {
        }
    }
}