using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.VisualStudio.Utilities
{
    /// <summary>
    /// Service that keeps track of <see cref="IFeatureController"/>'s requests to disable a feature in given scope.
    /// When multiple <see cref="IFeatureController"/>s disable a feature and one <see cref="IFeatureController"/>
    /// enables it back, it will not interfere with other disable requests, and feature will ultimately remain disabled.
    ///
    /// While this service does have a thread affinity, its implementation does not guarantee thread safety.
    /// It is advised to change feature state from UI thread, otherwise simultaneous changes may result in race conditions.
    /// </summary>
    /// <example>
    /// // In an exported MEF part:
    /// [Import]
    /// IFeatureServiceFactory FeatureServiceFactory;
    ///
    /// IFeatureService globalService = FeatureServiceFactory.GlobalFeatureService;
    /// IFeatureService localService = FeatureServiceFactory.GetOrCreate(scope); // scope is an IPropertyOwner
    ///
    /// // Also have a reference to <see cref="IFeatureController"/>:
    /// IFeatureController MyFeatureController;
    /// // Interact with the <see cref="IFeatureService"/>:
    /// globalService.Disable(PredefinedEditorFeatureNames.Popup, MyFeatureController);
    /// localService.IsEnabled(PredefinedEditorFeatureNames.Completion); // returns false, because Popup is a base definition of Completion and because global scope is a superset of local scope.
    /// </example>
    public interface IFeatureService
    {
        /// <summary>
        /// Checks if feature is enabled. By default, every feature is enabled.
        /// </summary>
        /// <param name="featureName">Name of the feature</param>
        /// <returns>False if there are any disable requests. True otherwise.</returns>
        bool IsEnabled(string featureName);

        /// <summary>
        /// Disables a feature.
        /// </summary>
        /// <param name="featureName">Name of the feature to disable</param>
        /// <param name="controller">Object that uniquely identifies the caller.</param>
        IFeatureDisableToken Disable(string featureName, IFeatureController controller);

        /// <summary>
        /// Provides a notification when this feature or its base feature was updated.
        /// We use FeatureUpdatedEventArgs and not FeatureChangedEventArgs
        /// because there are base features and disable requests from parent scopes that affect the factual state of given feature.
        /// We use this event to let the interested parties (<see cref="IFeatureCookie"/>) recalculate the actual state of the feature.
        /// </summary>
        event EventHandler<FeatureUpdatedEventArgs> StateUpdated;

        /// <summary>
        /// Creates a new <see cref="IFeatureCookie"/> that provides O(1) access to the feature's value, in this service's scope.
        /// The <see cref="IFeatureCookie" /> is updated when the feature or its base is updated in this scope or in the global scope.
        /// </summary>
        /// <param name="featureName">Name of the feature</param>
        /// <returns>New instance of <see cref="IFeatureCookie"/></returns>
        IFeatureCookie GetCookie(string featureName);
    }
}
