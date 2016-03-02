using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace RoslynPad.Roslyn.Editor
{
    [Export(typeof(IInlineRenameService)), Shared]
    internal sealed class InlineRenameService : IInlineRenameService
    {
        private readonly Microsoft.CodeAnalysis.Editor.IInlineRenameService _inner;

        [ImportingConstructor]
        public InlineRenameService(Microsoft.CodeAnalysis.Editor.IInlineRenameService inner)
        {
            _inner = inner;
        }
        
        public IInlineRenameSession ActiveSession => new InlineRenameSession(_inner.ActiveSession);

        public InlineRenameSessionInfo StartInlineSession(Document document, TextSpan triggerSpan,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return new InlineRenameSessionInfo(_inner.StartInlineSession(document, triggerSpan, cancellationToken));
        }
    }
}