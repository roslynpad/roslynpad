namespace Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data
{
    /// <summary>
    /// Contains additional properties of thie <see cref="ICompletionPresenter"/> that may be accessed
    /// prior to initializing an instance of <see cref="ICompletionPresenter"/>
    /// </summary>
    public sealed class CompletionPresenterOptions
    {
        /// <summary>
        /// Declares the length of the jump when user presses PageUp and PageDown keys.
        /// </summary>
        /// <remarks>This value needs to be known before the UI is created, hence it is defined in this class instead of <see cref="ICompletionPresenter"/>.
        /// Note that <see cref="IAsyncCompletionSession"/> handles keyboard scrolling, including using PageUp and PageDown keys.</remarks>
        public int ResultsPerPage { get; }

        /// <summary>
        /// Constructs instance of <see cref="CompletionPresenterOptions"/>
        /// </summary>
        /// <param name="resultsPerPage">Declares the length of the jump when user presses PageUp and PageDown keys</param>
        public CompletionPresenterOptions(int resultsPerPage)
        {
            ResultsPerPage = resultsPerPage;
        }
    }
}
