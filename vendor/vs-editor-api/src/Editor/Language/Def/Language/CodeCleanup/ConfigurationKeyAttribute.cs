using Microsoft;
using System;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.Language.CodeCleanUp
{
    /// <summary>
    /// Associates a configuration key with a fixer code part.
    /// </summary>
    public sealed class ConfigurationKeyAttribute : SingletonBaseMetadataAttribute
    {
        /// <summary>
        /// Constructs a new instance of the attribute.
        /// </summary>
        /// <param name="configurationKey">Fixer configuration key</param>
        /// <exception cref="ArgumentNullException"><paramref name="configurationKey"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="configurationKey"/> is an empty string.</exception>
        public ConfigurationKeyAttribute(string configurationKey)
        {
            Requires.NotNullOrWhiteSpace(configurationKey, nameof(configurationKey));
            this.ConfigurationKey = configurationKey;
        }

        /// <summary>
        /// The configuration key
        /// </summary>
        public string ConfigurationKey { get; }
    }
}

