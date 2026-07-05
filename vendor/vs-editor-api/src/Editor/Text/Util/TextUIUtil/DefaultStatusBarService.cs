using System.Threading.Tasks;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.Text.Utilities
{
    [System.Composition.Shared]
    [ExportImplementation(typeof(IStatusBarService))]
    [Name("default")]
    public class DefaultStatusBarService : IStatusBarService
    {
        public Task SetTextAsync(string text)
        {
            return Task.CompletedTask;
        }
    }
}
