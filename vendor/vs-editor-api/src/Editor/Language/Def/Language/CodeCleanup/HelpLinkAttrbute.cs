using Microsoft;
using System;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.Language.CodeCleanUp
{
    /// <summary>
    /// Associates an help URI with a fixer code part.
    /// </summary>
    public sealed class HelpLinkAttribute : SingletonBaseMetadataAttribute
    {
        /// <summary>
        /// Constructs a new instance of the attribute.
        /// </summary>
        /// <param name="helpLink">The fixer help uri</param>
        /// <exception cref="ArgumentNullException"><paramref name="helpLink"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="helpLink"/> is an empty string.</exception>
        public HelpLinkAttribute(string helpLink)
        {
            Requires.NotNullOrWhiteSpace(helpLink, nameof(helpLink));
            this.HelpLink = helpLink;
        }

        /// <summary>
        /// The fixer help link
        /// </summary>
        public string HelpLink { get; }
    }
}

