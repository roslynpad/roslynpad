using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace RoslynPad.Roslyn;

[ExportWorkspaceServiceFactory(typeof(IDocumentTrackingService), ServiceLayer.Host)]
internal sealed class DocumentTrackingServiceFactory : IWorkspaceServiceFactory
{
    // Also serves non-RoslynPad workspaces (e.g. the metadata-as-source workspace), so it
    // relies on the open-document tracking every workspace maintains via OnDocumentOpened.
    private class DocumentTrackingService(Workspace workspace) : IDocumentTrackingService
    {
        public bool SupportsDocumentTracking => true;

        public DocumentId GetActiveDocument() => TryGetActiveDocument() ?? throw new InvalidOperationException("No active document");

        public DocumentId? TryGetActiveDocument() => workspace.GetOpenDocumentIds().FirstOrDefault();

        public ImmutableArray<DocumentId> GetVisibleDocuments() => [.. workspace.GetOpenDocumentIds()];

        public event EventHandler<DocumentId?>? ActiveDocumentChanged = delegate { };

        public event EventHandler<EventArgs>? NonRoslynBufferTextChanged = delegate { };
    }

    public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices) =>
        new DocumentTrackingService(workspaceServices.Workspace);
}
