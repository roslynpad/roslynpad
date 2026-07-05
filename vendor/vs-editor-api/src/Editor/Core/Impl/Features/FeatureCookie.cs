using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.VisualStudio.Utilities.Features.Implementation
{
    internal class FeatureCookie : IFeatureCookie
    {
        public string FeatureName { get; private set; }

        public event EventHandler<FeatureChangedEventArgs> StateChanged;

        public bool IsEnabled
        {
            get => this.featureIsEnabled;
            private set
            {
                if (this.featureIsEnabled == value)
                    return;

                this.featureIsEnabled = value;
                StateChanged?.Invoke(this, new FeatureChangedEventArgs(FeatureName, value));
            }
        }


        private bool featureIsEnabled;
        private IEnumerable<string> aliases;
        private FeatureService service;

        internal FeatureCookie(string featureName, IEnumerable<string> aliases, FeatureService service)
        {
            FeatureName = featureName;
            this.aliases = aliases;
            this.service = service;

            IsEnabled = service.IsEnabled(FeatureName);
            this.service.StateUpdated += OnStateUpdated;
        }

        /// <summary>
        /// Recalculates <see cref="IsEnabled" /> after pertinent feature or its base feature has updated.
        /// </summary>
        private void OnStateUpdated(object sender, FeatureUpdatedEventArgs args)
        {
            if (aliases.Contains(args.FeatureName))
            {
                IsEnabled = this.service.IsEnabled(this.FeatureName);
            }
        }
    }
}
