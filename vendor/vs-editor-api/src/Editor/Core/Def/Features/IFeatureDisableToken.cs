using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Utilities
{
    /// <summary>
    /// Keeps track of the request to disable the feature.
    /// To restore the feature, 
    /// </summary>
    public interface IFeatureDisableToken : IDisposable
    {
    }
}
