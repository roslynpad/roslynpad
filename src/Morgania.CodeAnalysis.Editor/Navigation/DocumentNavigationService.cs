using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Text;

namespace Morgania.CodeAnalysis.Editor.Navigation;

[ExportWorkspaceService(typeof(IDocumentNavigationService))]
internal sealed class DocumentNavigationService : IDocumentNavigationService
{
    public Task<bool> CanNavigateToSpanAsync(Workspace workspace, DocumentId documentId, TextSpan textSpan, bool allowInvalidSpan, CancellationToken cancellationToken) => Task.FromResult(true);
    public Task<bool> CanNavigateToPositionAsync(Workspace workspace, DocumentId documentId, int position, int virtualSpace, bool allowInvalidPosition, CancellationToken cancellationToken) => Task.FromResult(true);
    public Task<INavigableLocation?> GetLocationForSpanAsync(Workspace workspace, DocumentId documentId, TextSpan textSpan, bool allowInvalidSpan, CancellationToken cancellationToken) => Task.FromResult<INavigableLocation?>(null);
    public Task<INavigableLocation?> GetLocationForPositionAsync(Workspace workspace, DocumentId documentId, int position, int virtualSpace, bool allowInvalidPosition, CancellationToken cancellationToken) => Task.FromResult<INavigableLocation?>(null);
}
