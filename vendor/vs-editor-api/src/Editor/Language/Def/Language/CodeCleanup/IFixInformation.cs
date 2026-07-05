using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Language.CodeCleanUp
{
    public interface IFixInformation
    {
        /// <summary>
        /// Fixer Id for example "IDE001"
        /// </summary>
        string FixerId { get; }

        /// <summary>
        /// Key for use in the .editorconfig file
        /// </summary>
        string ConfigurationKey { get; }

        /// <summary>
        /// Optional help link to provide more information about the fixer code
        /// </summary>
        string HelpLink { get; }

        /// <summary>
        /// Localized display name
        /// </summary>
        string LocalizedDisplayName { get; }
    }
}
