using Microsoft;
using System;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.Language.CodeCleanUp
{
    /// <summary>
    /// Error code information that describes an errro code that can be fixed by one a code cleanup fixer
    /// </summary>
    internal sealed class FixerCodeInfo : IFixInformation
    {
        private readonly Lazy<FixIdDefinition, FixIdDefinitionMetadata> fixerCode;

        /// <summary>
        /// Error code information
        /// </summary>
        public FixerCodeInfo(Lazy<FixIdDefinition, FixIdDefinitionMetadata> fixerCode)
        {
            Requires.NotNull(fixerCode, nameof(fixerCode));
            this.fixerCode = fixerCode;
        }

        /// <summary>
        /// Fixer code, Example: IDE001
        /// </summary>
        public string FixerId => fixerCode.Metadata.FixId;

        /// <summary>
        /// Configuration name used for setting modification
        /// Example:ConfigKeyName: CS_Remove_Unused_Usings
        /// </summary>
        public string ConfigurationKey => fixerCode.Metadata.ConfigurationKey;

        /// <summary>
        /// Help link to give user more information about the error code.
        /// May be null
        /// </summary>
        public string HelpLink => fixerCode.Metadata.HelpLink;

        /// <summary>
        /// Gets the localized friendly name
        /// </summary>
        public string LocalizedDisplayName => fixerCode.Metadata.LocalizedName;
    }
}
