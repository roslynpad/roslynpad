using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Utilities;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Implementation
{
    /// <summary>
    /// Provides information whether modern completion should be enabled,
    /// based on the state of <see cref="PredefinedEditorFeatureNames.AsyncCompletion"/> in <see cref="IFeatureServiceFactory" />
    /// for the given <see cref="IContentType"/> and <see cref="ITextView"/>.
    /// </summary>
    [Export]
    [Shared]
    public class CompletionAvailabilityUtility
    {
        [Import]
        public IFeatureServiceFactory FeatureServiceFactory { get; set; }

        [Import]
        public AsyncCompletionBroker Broker { get; set; } // We're using internal method to check if relevant MEF parts exist.

        // Quick access data:
        private IFeatureCookie _globalCompletionCookie;
        private IFeatureCookie GlobalCompletionCookie =>
            _globalCompletionCookie
            ?? (_globalCompletionCookie = FeatureServiceFactory.GlobalFeatureService.GetCookie(PredefinedEditorFeatureNames.AsyncCompletion));

        /// <summary>
        /// Returns whether completion is available for the given <see cref="IContentType"/> and <see cref="ITextViewRoleSet" />.
        /// </summary>
        /// <returns>true if feature is enabled in the <see cref="ITextView" />'s scope, and broker has providers that match the supplied <see cref="IContentType" /></returns>
        internal bool IsAvailable(IContentType contentType, ITextViewRoleSet roles)
        {
            return GlobalCompletionCookie.IsEnabled
                && Broker.HasCompletionProviders(contentType, roles);
        }

        /// <summary>
        /// Returns whether completion feature is available in the given <see cref="ITextView" />.
        /// </summary>
        /// <returns>true if feature is enabled in <see cref="ITextView"/>'s scope</returns>
        internal bool IsCurrentlyAvailable(ITextView textView)
        {
            var featureService = FeatureServiceFactory.GetOrCreate(textView);
            return featureService.IsEnabled(PredefinedEditorFeatureNames.AsyncCompletion);
        }
    }
}
