using Microsoft;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Language.CodeCleanUp;

namespace Microsoft.VisualStudio.Language.CodeCleanUp
{
    public sealed class FixIdContainer
    {
        private readonly IReadOnlyCollection<IFixInformation> fixes;

        public FixIdContainer(IReadOnlyCollection<IFixInformation> fixes)
        {
            Requires.NotNull(fixes, nameof(fixes));
            this.fixes = fixes;
        }

        public IReadOnlyCollection<IFixInformation> Fixes => fixes;

        /// <summary>
        /// Is the fix id enabled
        /// </summary>
        public bool IsFixIdEnabled(string fixid)
        {
            return fixes.Any((s) => string.Equals(s.FixerId, fixid, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Is the fix id enabled based on the path to a file
        /// </summary>
        public bool IsFixIdEnabled(string path, string fixid)
        {
            Requires.NotNullOrWhiteSpace(path, nameof(path));
            return this.IsFixIdEnabled(fixid);
        }
    }
}
