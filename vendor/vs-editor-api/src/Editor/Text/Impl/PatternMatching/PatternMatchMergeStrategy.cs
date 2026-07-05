using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Text.PatternMatching.Implementation
{
    /// <summary>
    /// This indicates how matches should be merged together when multiple matches are found, and only one can be returned.
    /// </summary>
    internal enum PatternMatchMergeStrategy
    {
        /// <summary>
        /// Indicates that matches were found in a single string and should be merged as intelligently as possible to report best results.
        /// </summary>
        Simple,

        /// <summary>
        /// Indicates a container match, which means matches are all from distinct parts of the original string, and should be merged with
        /// a take-the-worst policy.
        /// </summary>
        Container
    }
}
