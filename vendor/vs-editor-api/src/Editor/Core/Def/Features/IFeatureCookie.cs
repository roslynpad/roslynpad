using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Utilities
{
    /// <summary>
    /// Provides O(1) read only view on the state of the feature
    /// in the <see cref="IFeatureService" /> that created this <see cref="IFeatureCookie" />.
    /// Also exposes an event that provides notification when the state of the feature changes.
    /// </summary>
    public interface IFeatureCookie
    {
        /// <summary>
        /// Provides notification when <see cref="IsEnabled"/> value changes.
        /// </summary>
        event EventHandler<FeatureChangedEventArgs> StateChanged;

        /// <summary>
        /// Up to date state of the feature.
        /// </summary>
        bool IsEnabled { get; }

        /// <summary>
        /// Name of the tracked feature.
        /// </summary>
        string FeatureName { get; }
    }
}
