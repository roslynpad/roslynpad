using System;
using System.Collections.Generic;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.Text.UI.Utilities
{
    [Export(typeof(IStatusBarService))]
    [Shared]
    public class StatusBarService : BaseProxyService<IStatusBarService>, IStatusBarService
    {
        [ImportImplementations(typeof(IStatusBarService))]
        public override IEnumerable<Lazy<IStatusBarService, Orderable>> UnorderedImplementations { get; set; }

        public Task SetTextAsync(string text)
        {
            return BestImplementation.SetTextAsync(text);
        }
    }
}
