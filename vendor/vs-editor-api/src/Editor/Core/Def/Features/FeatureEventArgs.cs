using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Utilities
{
    /// <summary>
    /// Notifies that a specific feature was updated and might have changed its state,
    /// without computing the state value.
    /// </summary>
    public class FeatureUpdatedEventArgs : EventArgs
    {
        /// <summary>
        /// Name of feature that was updated.
        /// </summary>
        public string FeatureName { get; }

        /// <summary>
        /// Creates an instance of <see cref="FeatureUpdatedEventArgs"/>.
        /// </summary>
        /// <param name="featureName">Name of feature that was updated</param>
        public FeatureUpdatedEventArgs(string featureName)
        {
            FeatureName = featureName;
        }
    }

    /// <summary>
    /// Notifies that a specific feature changed state, and provides the new state value.
    /// </summary>
    public class FeatureChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Name of feature that was changed.
        /// </summary>
        public string FeatureName { get; }

        /// <summary>
        /// New value of the feature state.
        /// </summary>
        public bool IsEnabled { get; }

        /// <summary>
        /// Creates an instance of <see cref="FeatureChangedEventArgs"/>.
        /// </summary>
        /// <param name="featureName">Name of feature that was changed</param>
        /// <param name="isEnabled">New value of the feature state</param>
        public FeatureChangedEventArgs(string featureName, bool isEnabled)
        {
            FeatureName = featureName;
            IsEnabled = isEnabled;
        }
    }
}
