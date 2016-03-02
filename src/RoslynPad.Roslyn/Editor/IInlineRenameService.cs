using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace RoslynPad.Roslyn.Editor
{
    public interface IInlineRenameService
    {
        IInlineRenameSession ActiveSession { get; }

        InlineRenameSessionInfo StartInlineSession(Document document, TextSpan triggerSpan, CancellationToken cancellationToken = default(CancellationToken));
    }
}