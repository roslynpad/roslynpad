using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Language.CodeCleanUp
{
    /// <summary>
    /// Represents MEF metadata view corresponding to the <see cref="FixIdDefinition"/>s.
    /// </summary>
    public interface IFixIdDefinitionMetadata
    {
        /// <summary>
        /// Fixer Id for example "IDE001"
        /// </summary>
        string FixId { get; }

        /// <summary>
        /// Key for use in the .editorconfig file
        /// </summary>
        string ConfigurationKey { get; }

        /// <summary>
        /// Optional help link to provide more information about the fixer code
        /// </summary>
        [DefaultValue(null)]
        string HelpLink { get; }

        /// <summary>
        /// Localized display name
        /// </summary>
        string LocalizedName { get; }
    }

    /// <summary>
    /// Concrete metadata view for <see cref="IFixIdDefinitionMetadata"/>; System.Composition cannot
    /// proxy interface views, so imports use this class (PLAN §5.2 rule 2).
    /// </summary>
    public sealed class FixIdDefinitionMetadata : IFixIdDefinitionMetadata
    {
        public FixIdDefinitionMetadata(System.Collections.Generic.IDictionary<string, object> data)
        {
            this.FixId = Microsoft.VisualStudio.Utilities.MetadataValue.Get<string>(data, nameof(FixId));
            this.ConfigurationKey = Microsoft.VisualStudio.Utilities.MetadataValue.Get<string>(data, nameof(ConfigurationKey));
            this.HelpLink = Microsoft.VisualStudio.Utilities.MetadataValue.Get<string>(data, nameof(HelpLink));
            this.LocalizedName = Microsoft.VisualStudio.Utilities.MetadataValue.Get<string>(data, nameof(LocalizedName));
        }

        public string FixId { get; }
        public string ConfigurationKey { get; }
        public string HelpLink { get; }
        public string LocalizedName { get; }
    }
}
