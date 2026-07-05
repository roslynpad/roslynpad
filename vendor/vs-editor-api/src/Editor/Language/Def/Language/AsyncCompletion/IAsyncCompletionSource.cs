using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion
{
    /// <summary>
    /// Represents an object that provides <see cref="CompletionItem"/>s and other information
    /// relevant to the completion feature at a specific <see cref="SnapshotPoint"/>.
    /// </summary>
    /// <remarks>
    /// Instances of this class should be created by <see cref="IAsyncCompletionSourceProvider"/>, which is a MEF part.
    /// </remarks>
    public interface IAsyncCompletionSource
    {
        /// <summary>
        /// Called once per completion session to fetch the set of all completion items available at a given location.
        /// Called on a background thread.
        /// </summary>
        /// <param name="session">Reference to the active <see cref="IAsyncCompletionSession"/></param>
        /// <param name="trigger">What caused the completion</param>
        /// <param name="triggerLocation">Location where completion was triggered, on the subject buffer that matches this <see cref="IAsyncCompletionSource"/>'s content type</param>
        /// <param name="applicableToSpan">Location where completion will take place, on the view's data buffer: <see cref="ITextView.TextBuffer"/></param>
        /// <param name="token">Cancellation token that may interrupt this operation</param>
        /// <returns>A struct that holds completion items and applicable span</returns>
        Task<CompletionContext> GetCompletionContextAsync(
            IAsyncCompletionSession session,
            CompletionTrigger trigger,
            SnapshotPoint triggerLocation,
            SnapshotSpan applicableToSpan,
            CancellationToken token);

        /// <summary>
        /// Returns tooltip associated with provided <see cref="CompletionItem"/>.
        /// The returned object will be rendered by <see cref="IViewElementFactoryService"/>. See its documentation for default supported types.
        /// You may export a <see cref="IViewElementFactory"/> to provide a renderer for a custom type.
        /// Since this method is called on a background thread and on multiple platforms, an instance of UIElement may not be returned.
        /// </summary>
        /// <param name="session">Reference to the active <see cref="IAsyncCompletionSession"/></param>
        /// <param name="item"><see cref="CompletionItem"/> which is a subject of the tooltip</param>
        /// <param name="token">Cancellation token that may interrupt this operation</param>
        /// <returns>An object that will be passed to <see cref="IViewElementFactoryService"/>. See its documentation for supported types.</returns>
        Task<object> GetDescriptionAsync(IAsyncCompletionSession session, CompletionItem item, CancellationToken token);

        /// <summary>
        /// Provides the span applicable to the prospective session.
        /// Called on UI thread and expected to return very quickly, based on syntactic clues.
        /// This method is called as a result of user action, after the Editor makes necessary changes in direct response to user's action.
        /// The state of the Editor prior to making the text edit is captured in <see cref="CompletionTrigger.ViewSnapshotBeforeTrigger"/> of <paramref name="trigger"/>.
        /// This method is called sequentially on available <see cref="IAsyncCompletionSource"/>s until one of them returns
        /// <see cref="CompletionStartData"/> with appropriate level of <see cref="CompletionStartData.Participation"/>
        /// and one returns <see cref="CompletionStartData"/> with <see cref="CompletionStartData.ApplicableToSpan"/>
        /// If neither of the above conditions are met, no completion session will start.
        /// </summary>
        /// <remarks>
        /// If a language service does not wish to participate in completion, it should try to provide a valid <see cref="CompletionStartData.ApplicableToSpan"/>
        /// and set <see cref="CompletionStartData.Participation"/> to <c>false</c>.
        /// This will enable other extensions to provide completion in syntactically appropriate location.
        /// </remarks>
        /// <param name="trigger">What causes the completion, including the character typed and reference to <see cref="ITextView.TextSnapshot"/> prior to triggering the completion</param>
        /// <param name="triggerLocation">Location on the subject buffer that matches this <see cref="IAsyncCompletionSource"/>'s content type</param>
        /// <param name="token">Cancellation token that may interrupt this operation</param>
        /// <returns>Whether this <see cref="IAsyncCompletionSource"/> wishes to participate in completion.</returns>
        CompletionStartData InitializeCompletion(CompletionTrigger trigger, SnapshotPoint triggerLocation, CancellationToken token);
    }
}
