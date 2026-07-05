using System;
using Microsoft.VisualStudio.Commanding;

namespace Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion
{
    /// <summary>
    /// Provides names used by the Async Completion feature.
    /// </summary>
    public static class PredefinedCompletionNames
    {
        /// <summary>
        /// Name of the default <see cref="IAsyncCompletionItemManagerProvider"/>. Use to order your MEF part.
        /// </summary>
        public const string DefaultCompletionItemManager = "DefaultCompletionItemManager";

        /// <summary>
        /// Name of the default <see cref="ICompletionPresenterProvider"/>. Use to order your MEF part.
        /// </summary>
        public const string DefaultCompletionPresenter = "DefaultCompletionPresenter";

        /// <summary>
        /// Name of the completion's <see cref="ICommandHandler"/>. Use to order your MEF part.
        /// </summary>
        public const string CompletionCommandHandler = "CompletionCommandHandler";

        [Obsolete("Use Morgania.Text.Editor.DefaultOptions.NonBlockingCompletionOptionName instead")]
        /// <summary>
        /// Name of the editor option that stores user's preference for dismissing completion rather than blocking for potentially long running tasks.
        /// </summary>
        public const string NonBlockingCompletionOptionName = "NonBlockingCompletion";

        /// <summary>
        /// Name of the editor option that stores user's preference for the completion mode.
        /// </summary>
        public const string SuggestionModeInCompletionOptionName = "SuggestionModeInCompletion";

        /// <summary>
        /// Name of the editor option that stores user's preference for the completion mode during debugging.
        /// </summary>
        public const string SuggestionModeInDebuggerCompletionOptionName = "SuggestionModeInDebuggerViewCompletion";

        /// <summary>
        /// Order your MEF part of type <see cref="Data.CompletionFilter"/> relatively to this name,
        /// so that it tends to be the default expander (order before this name) or not be the default expander (order after this name).
        /// </summary>
        public const string DefaultCompletionExpander = "DefaultCompletionExpander";
    }
}
