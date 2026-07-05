using System;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;

namespace Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion
{
    /// <summary>
    /// Represents a class that manages user interface for the completion feature.
    /// All methods are called on UI thread.
    /// </summary>
    /// <remarks>
    /// Instances of this class should be created by <see cref="ICompletionPresenterProvider"/>, which is a MEF part.
    /// </remarks>
    public interface ICompletionPresenter : IDisposable
    {
        /// <summary>
        /// Opens the UI and displays provided data
        /// </summary>
        /// <param name="presentation">Data to display in the UI</param>
        void Open(IAsyncCompletionSession session, CompletionPresentationViewModel presentation);

        /// <summary>
        /// Updates the UI with provided data
        /// </summary>
        /// <param name="presentation">Data to display in the UI</param>
        void Update(IAsyncCompletionSession session, CompletionPresentationViewModel presentation);

        /// <summary>
        /// Hides the completion UI
        /// </summary>
        void Close();

        /// <summary>
        /// Notifies of user changing the selection state of filters
        /// </summary>
        event EventHandler<CompletionFilterChangedEventArgs> FiltersChanged;

        /// <summary>
        /// Notifies of user selecting an item.
        /// When item is selected programmatically, firing this event may result in endless loop.
        /// </summary>
        event EventHandler<CompletionItemSelectedEventArgs> CompletionItemSelected;

        /// <summary>
        /// Notifies of user committing an item for completion
        /// </summary>
        event EventHandler<CompletionItemEventArgs> CommitRequested;

        /// <summary>
        /// Notifies of UI closing
        /// </summary>
        event EventHandler<CompletionClosedEventArgs> CompletionClosed;
    }
}
