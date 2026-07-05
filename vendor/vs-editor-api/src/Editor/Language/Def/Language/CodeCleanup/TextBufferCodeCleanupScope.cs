using Microsoft;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.Language.CodeCleanUp
{
    /// <summary>
    /// Context for a text buffer
    /// </summary>
    public class TextBufferCodeCleanUpScope : ICodeCleanUpScope
    {
        public TextBufferCodeCleanUpScope(ITextBuffer subjectBuffer)
        {
            Requires.NotNull(subjectBuffer, nameof(subjectBuffer));
            this.SubjectBuffer = subjectBuffer;
        }

        /// <summary>
        /// Gets the text buffer to apply code clean up change to.
        /// Note, this is a live buffer rather than a snapshot.
        /// </summary>
        public ITextBuffer SubjectBuffer { get; }
    }
}
