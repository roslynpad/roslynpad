using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Text.Document
{
    /// <summary>
    /// Creates Whitespace Managers
    /// </summary>
    /// <remarks>
    /// This is a MEF component part, and should be imported as follows:
    /// [Import]
    /// IEditingStateFactory factory = null;
    /// </remarks>
    public interface IWhitespaceManagerFactory
    {
        /// <summary>
        /// Gets or creates an instance if <see cref="IWhitespaceManager"/> from the provided parameters.
        /// There will be at most one manager created per buffer. Subsequent calls will use the existing
        /// newline state, and that parameter will be ignored.
        /// </summary>
        /// <param name="buffer">The buffer associated with the whitespace manager.</param>
        /// <param name="newlineState">
        /// A seed newline state that can be pre-filled with counts of the newlines in a document. The buffer
        /// will be tracked forward through edits to update the newline state, so accurate starting values are critical.
        /// </param>
        /// <returns>A whitespace manager that has been subscribed to track edits in the given buffer.</returns>
        IWhitespaceManager GetOrCreateWhitespaceManager(
            ITextBuffer buffer,
            NewlineState initialNewlineState,
            LeadingWhitespaceState initialLeadingWhitespaceState);

        /// <summary>
        /// Tries to get a whitespace manager that already exists for the given buffer.
        /// </summary>
        /// <param name="buffer">The buffer on which to search for an associated whitespace manager.</param>
        /// <param name="manager">The instance of the manager if successfully found.</param>
        /// <returns>True if successfully found a manager, false otherwise.</returns>
        bool TryGetExistingWhitespaceManager(ITextBuffer buffer, out IWhitespaceManager manager);
    }
}
