using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.VisualStudio.Utilities
{
    /// <summary>
    /// Service that provides <see cref="IFeatureService" />s used to track feature availability and to request feature to be disabled.
    /// Feature may be tracked by scope, using <see cref="IFeatureServiceFactory.GetOrCreate" /> and passing <see cref="IPropertyOwner" /> e.g. a text view.
    /// or throughout the application using <see cref="IFeatureServiceFactory.GlobalFeatureService" />.
    /// 
    /// Features are implemented by exporting <see cref="FeatureDefinition"/> and grouped using <see cref="BaseDefinitionAttribute"/>.
    /// Grouping allows alike features to be disabling at once.
    /// Grouping also relieves <see cref="IFeatureController"/> from updating its code when new feature of appropriate category is introduced.
    /// Standard editor feature names are available in <see cref="PredefinedEditorFeatureNames"/>.
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
    public interface IFeatureServiceFactory
    {
        /// <summary>
        /// Gets the global <see cref="IFeatureService"/>
        /// </summary>
        IFeatureService GlobalFeatureService { get; }

        /// <summary>
        /// Gets the <see cref="IFeatureService"/> for the specified scope.
        /// </summary>
        /// <param name="scope"></param>
        /// <returns></returns>
        IFeatureService GetOrCreate(IPropertyOwner scope);
    }
}
