using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Text;

namespace RoslynPad.Roslyn.Navigation
{
    [ExportWorkspaceService(typeof(IDocumentNavigationService))]
    internal sealed class DocumentNavigationService : IDocumentNavigationService
    {
        public Task<bool> CanNavigateToSpanAsync(Workspace workspace, DocumentId documentId, TextSpan textSpan, bool allowInvalidSpan, CancellationToken cancellationToken) => Task.FromResult(true);
        public Task<bool> CanNavigateToLineAndOffsetAsync(Workspace workspace, DocumentId documentId, int lineNumber, int offset, CancellationToken cancellationToken) => Task.FromResult(true);
        public Task<bool> CanNavigateToPositionAsync(Workspace workspace, DocumentId documentId, int position, int virtualSpace, CancellationToken cancellationToken) => Task.FromResult(true);
        public Task<INavigableLocation?> GetLocationForSpanAsync(Workspace workspace, DocumentId documentId, TextSpan textSpan, bool allowInvalidSpan, CancellationToken cancellationToken) => Task.FromResult<INavigableLocation?>(null);
        public Task<INavigableLocation?> GetLocationForPositionAsync(Workspace workspace, DocumentId documentId, int position, int virtualSpace, CancellationToken cancellationToken) => Task.FromResult<INavigableLocation?>(null);
        public Task<INavigableLocation?> GetLocationForLineAndOffsetAsync(Workspace workspace, DocumentId documentId, int lineNumber, int offset, CancellationToken cancellationToken) => Task.FromResult<INavigableLocation?>(null);
    }
}
