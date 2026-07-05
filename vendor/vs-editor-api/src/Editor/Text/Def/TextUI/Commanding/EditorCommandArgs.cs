using System;
using Microsoft.VisualStudio.Commanding;

namespace Microsoft.VisualStudio.Text.Editor.Commanding
{
    /// <summary>
    /// A base class for all editor command arguments.
    /// </summary>
    public abstract class EditorCommandArgs : CommandArgs
    {
        /// <summary>
        /// A subject buffer to execute a command on.
        /// </summary>
        public ITextBuffer SubjectBuffer { get; }

        /// <summary>
        /// An <see cref="ITextView"/> to execute a command on.
        /// </summary>
        public ITextView TextView { get; }

        /// <summary>
        /// Creates new instance of the <see cref="EditorCommandArgs"/> with given
        /// <see cref="ITextView"/> and <see cref="ITextBuffer"/>.
        /// </summary>
        /// <param name="textView">A <see cref="ITextView"/> to execute a command on.</param>
        /// <param name="subjectBuffer">A <see cref="ITextBuffer"/> to execute command on.</param>
        public EditorCommandArgs(ITextView textView, ITextBuffer subjectBuffer)
        {
            this.TextView = textView ?? throw new ArgumentNullException(nameof(textView));
            this.SubjectBuffer = subjectBuffer ?? throw new ArgumentNullException(nameof(subjectBuffer));
        }
    }
}
