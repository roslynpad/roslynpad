using System.Composition;
using Avalonia.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Text;

namespace RoslynPad.Roslyn.Navigation;

[ExportWorkspaceService(typeof(IDocumentNavigationService), ServiceLayer.Host), Shared]
[method: ImportingConstructor]
internal sealed class DocumentNavigationService(NavigationBridge bridge) : IDocumentNavigationService
{
    public Task<bool> CanNavigateToSpanAsync(Workspace workspace, DocumentId documentId, TextSpan textSpan, bool allowInvalidSpan, CancellationToken cancellationToken) =>
        Task.FromResult(bridge.Host is not null);

    public Task<bool> CanNavigateToPositionAsync(Workspace workspace, DocumentId documentId, int position, int virtualSpace, bool allowInvalidPosition, CancellationToken cancellationToken) =>
        Task.FromResult(bridge.Host is not null);

    public Task<INavigableLocation?> GetLocationForSpanAsync(Workspace workspace, DocumentId documentId, TextSpan textSpan, bool allowInvalidSpan, CancellationToken cancellationToken) =>
        GetLocation(documentId, textSpan);

    public Task<INavigableLocation?> GetLocationForPositionAsync(Workspace workspace, DocumentId documentId, int position, int virtualSpace, bool allowInvalidPosition, CancellationToken cancellationToken) =>
        GetLocation(documentId, new TextSpan(position, 0));

    private Task<INavigableLocation?> GetLocation(DocumentId documentId, TextSpan span) =>
        Task.FromResult<INavigableLocation?>(bridge.Host is not { } host
            ? null
            : new NavigableLocation((_, cancellationToken) =>
                Dispatcher.UIThread.InvokeAsync(() => host.NavigateToDocumentAsync(documentId, span, cancellationToken))));
}
