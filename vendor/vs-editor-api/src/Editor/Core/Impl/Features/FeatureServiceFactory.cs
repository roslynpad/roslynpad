using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using Microsoft.VisualStudio.Text.Utilities;

namespace Microsoft.VisualStudio.Utilities.Features.Implementation
{
    /// <inheritdoc />
    [Export(typeof(IFeatureServiceFactory))]
    [Shared]
    public class FeatureServiceFactory : IFeatureServiceFactory
    {
        /// <summary>
        /// All <see cref="FeatureDefinition"/>s imported by MEF
        /// </summary>
        [ImportMany]
        public IEnumerable<Lazy<FeatureDefinition, FeatureDefinitionMetadata>> AllDefinitions { get; set; }

        [Import]
        public IGuardedOperations GuardedOperations { get; set; }

        [Import(AllowDefault = true)]
        public ILoggingServiceInternal Telemetry { get; set; }

        /// <summary>
        /// Maps feature name to all names that may disable the feature,
        /// i.e. the name itself and names of base <see cref="FeatureDefinition"/>s
        /// </summary>
        internal IDictionary<string, SortedSet<string>> RelatedDefinitions { get; set; }

        private bool initializing = false;
        private IFeatureService _globalFeatureService;

        /// <inheritdoc />
        public IFeatureService GlobalFeatureService
        {
            get
            {
                if (this.initializing) // Protection from stack oveflow
                    throw new InvalidOperationException($"Do not access {nameof(GlobalFeatureService)} when it is being initialized");

                if (_globalFeatureService == null)
                {
                    this.initializing = true;
                    _globalFeatureService = new FeatureService(parent: null, factory: this);
                    this.initializing = false;
                }
                return _globalFeatureService;
            }
        }

        /// <inheritdoc />
        public IFeatureService GetOrCreate(IPropertyOwner scope)
        {
            if (scope == null)
                throw new ArgumentNullException(nameof(scope));

            return scope.Properties.GetOrCreateSingletonProperty(
                () => new FeatureService(GlobalFeatureService, this));
        }

        /// <summary>
        /// Does the initial setup: iterates over imported definitions and builds a mapping
        /// from base feature definitions to leaf feature definitions
        /// </summary>
        [OnImportsSatisfied]
        public void OnImportsSatisfied()
        {
            RelatedDefinitions = new Dictionary<string, SortedSet<string>>();
            foreach (var featureDefinition in AllDefinitions)
            {
                var alsoKnownAs = new SortedSet<string>();
                AddBaseDefinitionNamesToSet(featureDefinition.Metadata.Name, alsoKnownAs);
                RelatedDefinitions[featureDefinition.Metadata.Name] = alsoKnownAs;
            }
        }

        /// <summary>
        /// Recursively collects names of base <see cref="FeatureDefinition"/>s
        /// </summary>
        /// <param name="name">Feature name</param>
        /// <param name="set">Collection that stores names that may be used to disable the feature with given <paramref name="name"/></param>
        private void AddBaseDefinitionNamesToSet(string name, ISet<string> set)
        {
            foreach (var feature in AllDefinitions.Where(n => n.Metadata.Name.Equals(name, StringComparison.Ordinal)))
            {
                set.Add(feature.Metadata.Name);
                if (feature.Metadata.BaseDefinition == null)
                    continue;
                foreach (var baseDefinition in feature.Metadata.BaseDefinition)
                {
                    AddBaseDefinitionNamesToSet(baseDefinition, set);
                }
            }
        }
    }
}
