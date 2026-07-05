using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using Microsoft.VisualStudio.Text.Utilities;

namespace Microsoft.VisualStudio.Utilities.Features.Implementation
{
    internal class FeatureService : IFeatureService
    {
        /// <summary>
        /// Maintains list of currently active requests to disable a feature.
        /// </summary>
        IDictionary<string, FrugalList<IFeatureController>> Annulations { get; set; }

        /// <summary>
        /// Reference to the feature's scope.
        /// Currently used only to distinguish service pertinent to (local) scope from global service, where this value is null
        /// </summary>
        internal IFeatureService Parent { get; }

        /// <summary>
        /// Reference to the <see cref="FeatureServiceFactory"/>.
        /// </summary>
        internal FeatureServiceFactory Factory { get; }

        public event EventHandler<FeatureUpdatedEventArgs> StateUpdated;

        // Telemetry:
        const string TelemetryDisableEventName = "VS/Editor/FeatureService/Disable";
        const string TelemetryRestoreEventName = "VS/Editor/FeatureService/Restore";
        const string TelemetryFeatureNameKey = "Property.FeatureName";
        const string TelemetryControllerKey = "Property.Controller";
        const string TelemetryGlobalScopeKey = "Property.IsGlobalScope";

        /// <summary>
        /// Creates an instance of FeatureService
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="factory"></param>
        internal FeatureService(IFeatureService parent, FeatureServiceFactory factory)
        {
            this.Parent = parent;
            this.Factory = factory;

            Annulations = new Dictionary<string, FrugalList<IFeatureController>>(factory.AllDefinitions.Count());
            foreach (var featureDefinition in factory.AllDefinitions)
            {
                // TODO: I don't think we need to initialize all lists.
                Annulations[featureDefinition.Metadata.Name] = new FrugalList<IFeatureController>();
            }

            if (Parent != null)
            {
                // subscribe to events of the parent service
                Parent.StateUpdated += OnParentServiceStateUpdated;
            }
        }

        public bool IsEnabled(string featureName)
        {
            if (string.IsNullOrEmpty(featureName))
                throw new ArgumentNullException(nameof(featureName));
            if (!Factory.RelatedDefinitions.ContainsKey(featureName))
                throw new ArgumentOutOfRangeException(nameof(featureName), $"Feature {featureName} is not registered");

            foreach (var definition in Factory.RelatedDefinitions[featureName])
            {
                if (Annulations[definition].Count > 0)
                    return false;
            }

            if (Parent != null)
            {
                // Also check the parent service
                return Parent.IsEnabled(featureName);
            }
            else
            {
                // This is the global service. Looks like the feature is enabled.
                return true;
            }
        }

        public IFeatureDisableToken Disable(string featureName, IFeatureController controller)
        {
            if (string.IsNullOrEmpty(featureName))
                throw new ArgumentNullException(nameof(featureName));
            if (controller == null)
                throw new ArgumentNullException(nameof(controller));
            if (!Factory.RelatedDefinitions.ContainsKey(featureName))
                throw new ArgumentOutOfRangeException(nameof(featureName), $"Feature {featureName} is not registered");

            var token = new FeatureDisableToken(this, featureName, controller);

            var annulations = Annulations[featureName];
            if (annulations.Contains(controller))
                return token; // This controller already disables this feature
            annulations.Add(controller);

            if (annulations.Count == 1) // Notify of update
                Factory.GuardedOperations.RaiseEvent(this, StateUpdated, new FeatureUpdatedEventArgs(featureName));

            Factory.Telemetry?.PostEvent(
                TelemetryEventType.Operation,
                TelemetryDisableEventName,
                TelemetryResult.Success,
                (TelemetryFeatureNameKey, featureName),
                (TelemetryControllerKey, controller.GetType().ToString()),
                (TelemetryGlobalScopeKey, Parent == null)
            );

            return token;
        }

        /// <summary>
        /// Cancels the request to disable a feature.
        /// If another <see cref="IFeatureController"/> disabled this feature or its group, the feature remains disabled.
        /// This method is internal, and called from <see cref="FeatureDisableToken"/>
        /// </summary>
        /// <remarks>
        /// While this service does have a thread affinity, its implementation does not guarantee thread safety.
        /// It is advised to change feature state from UI thread, otherwise simultaneous changes may result in race conditions.
        /// </remarks>
        /// <param name="featureName">Name of previously disabled feature</param>
        /// <param name="controller">Object that uniquely identifies the entity that disables and restores the feature.</param>
        internal void Restore(string featureName, IFeatureController controller)
        {
            if (string.IsNullOrEmpty(featureName))
                throw new ArgumentNullException(nameof(featureName));
            if (controller == null)
                throw new ArgumentNullException(nameof(controller));
            if (!Factory.RelatedDefinitions.ContainsKey(featureName))
                throw new ArgumentOutOfRangeException(nameof(featureName), $"Feature {featureName} is not registered");

            var annulations = Annulations[featureName];
            if (!annulations.Contains(controller))
                return; // This controller is not disabling this feature
            annulations.Remove(controller);

            if (annulations.Count == 0) // Notify of update
                Factory.GuardedOperations.RaiseEvent(this, StateUpdated, new FeatureUpdatedEventArgs(featureName));

            Factory.Telemetry?.PostEvent(
                TelemetryEventType.Operation,
                TelemetryRestoreEventName,
                TelemetryResult.Success,
                (TelemetryFeatureNameKey, featureName),
                (TelemetryControllerKey, controller.GetType().ToString()),
                (TelemetryGlobalScopeKey, Parent == null)
            );
        }

        Dictionary<string, IFeatureCookie> CookieCache = new Dictionary<string, IFeatureCookie>();

        public IFeatureCookie GetCookie(string featureName)
        {
            if (string.IsNullOrEmpty(featureName))
                throw new ArgumentNullException(nameof(featureName));
            if (!Factory.RelatedDefinitions.ContainsKey(featureName))
                throw new ArgumentOutOfRangeException(nameof(featureName), $"Feature {featureName} is not registered");

            if (!CookieCache.ContainsKey(featureName))
                CookieCache[featureName] = new FeatureCookie(featureName, Factory.RelatedDefinitions[featureName], this);
            return CookieCache[featureName];
        }

        /// <summary>
        /// Event handler that listens to updates in <see cref="IFeatureService" /> of parent scope,
        /// and propagates it further. The intent of this event is to update <see cref="IFeatureCookie"/>
        /// </summary>
        /// <param name="sender"><see cref="IFeatureService" /> that updated a feature</param>
        /// <param name="e">Instace of <see cref="FeatureUpdatedEventArgs"/></param>
        private void OnParentServiceStateUpdated(object sender, FeatureUpdatedEventArgs e)
        {
            Factory.GuardedOperations.RaiseEvent(sender, StateUpdated, e);
        }
    }
}
