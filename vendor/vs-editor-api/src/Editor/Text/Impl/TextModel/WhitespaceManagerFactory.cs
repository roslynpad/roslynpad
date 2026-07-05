using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text.Document;

namespace Microsoft.VisualStudio.Text.Implementation
{
    [Export(typeof(IWhitespaceManagerFactory))]
    [Shared]
    public class WhitespaceManagerFactory : IWhitespaceManagerFactory
    {
        public IWhitespaceManager GetOrCreateWhitespaceManager(
            ITextBuffer buffer,
            NewlineState initialNewlineState,
            LeadingWhitespaceState initialLeadingWhitespaceState)
        {
            return buffer.Properties.GetOrCreateSingletonProperty(
                typeof(IWhitespaceManager),
                () => new WhitespaceManager(buffer, initialNewlineState, initialLeadingWhitespaceState));
        }

        public bool TryGetExistingWhitespaceManager(ITextBuffer buffer, out IWhitespaceManager manager)
        {
            return buffer.Properties.TryGetProperty(typeof(IWhitespaceManager), out manager);
        }
    }
}
