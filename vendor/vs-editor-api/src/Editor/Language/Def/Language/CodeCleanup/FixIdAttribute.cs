using Microsoft;
using System;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.Language.CodeCleanUp
{
    /// <summary>
    /// Associates an identifying code with a fixer code part.
    /// </summary>
    public sealed class FixIdAttribute : SingletonBaseMetadataAttribute
    {
        /// <summary>
        /// Constructs a new instance of the attribute.
        /// </summary>
        /// <param name="fixId">The fix id</param>
        /// <exception cref="ArgumentNullException"><paramref name="fixId"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="fixId"/> is an empty string.</exception>
        public FixIdAttribute(string fixId)
        {
            Requires.NotNullOrWhiteSpace(fixId, nameof(fixId));
            this.FixId = fixId;
        }

        /// <summary>
        /// The fixer Id
        /// </summary>
        public string FixId { get; }
    }
}

