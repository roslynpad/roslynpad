namespace Microsoft.VisualStudio.Language.Intellisense.Implementation
{
    using System.Composition;
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Tagging;
    using Microsoft.VisualStudio.Threading;
    using Microsoft.VisualStudio.Utilities;

    [Export(typeof(IAsyncQuickInfoSourceProvider))]
    [Name("squiggle")]
    [Order]
    [ContentType("any")]
    [Shared]
    public sealed class SquiggleQuickInfoSourceProvider : IAsyncQuickInfoSourceProvider
    {
        [Import]
        public IViewTagAggregatorFactoryService TagAggregatorFactoryService { get; set; }

        [Import]
        public JoinableTaskContext JoinableTaskContext { get; set; }

        public IAsyncQuickInfoSource TryCreateQuickInfoSource(ITextBuffer textBuffer)
        {
            return new SquiggleQuickInfoSource(this, textBuffer);
        }
    }
}
