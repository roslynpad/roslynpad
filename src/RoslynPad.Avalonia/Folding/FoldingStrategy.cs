using AvaloniaEdit.Document;
using AvaloniaEdit.Folding;

namespace RoslynPad.Folding
{
    /// <summary>
    /// Base class for folding strategies.
    /// </summary>
    public abstract class FoldingStrategy
    {
        /// <summary>
        /// Create <see cref="NewFolding" />s for the specified document and updates the folding
        /// manager with them.
        /// </summary>
        /// <param name="manager">The <see cref="FoldingManager"/>.</param>
        /// <param name="document">The document.</param>
        public void UpdateFoldings(FoldingManager manager, TextDocument document)
        {
            var foldings = CreateNewFoldings(document, out var firstErrorOffset);
            manager.UpdateFoldings(foldings, firstErrorOffset);
        }


        /// <summary>
        /// Create <see cref="NewFolding" />s for the specified document.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <param name="firstErrorOffset">The offset of the first error.</param>
        /// <returns>The sequence of new foldings.</returns>
        protected abstract IEnumerable<NewFolding> CreateNewFoldings(TextDocument document, out int firstErrorOffset);
    }
}
