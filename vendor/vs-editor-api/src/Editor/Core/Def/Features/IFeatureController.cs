using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.VisualStudio.Utilities
{
    /// <summary>
    /// Keeps track of requests to disable a feature using <see cref="IFeatureService"/>.
    /// Each <see cref="IFeatureController"/> may re-enable a feature it disabled,
    /// but may not re-enable a feature disabled by another <see cref="IFeatureController"/>.
    /// </summary>
    public interface IFeatureController
    {
    }
}
