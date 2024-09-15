using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace RoslynPad.Roslyn;

[ExportWorkspaceServiceFactory(typeof(IDocumentTrackingService), ServiceLayer.Host)]
internal sealed class DocumentTrackingServiceFactory : IWorkspaceServiceFactory
{
    private class DocumentTrackingService(Workspace workspace) : IDocumentTrackingService
    {
        private readonly RoslynWorkspace _workspace = (RoslynWorkspace)workspace;

        public bool SupportsDocumentTracking => true;

        public DocumentId GetActiveDocument() => _workspace.OpenDocumentId ?? throw new InvalidOperationException("No active document");

        public DocumentId? TryGetActiveDocument() => _workspace.OpenDocumentId;

        public ImmutableArray<DocumentId> GetVisibleDocuments() => _workspace.OpenDocumentId != null ? [_workspace.OpenDocumentId] : [];

        public event EventHandler<DocumentId?>? ActiveDocumentChanged = delegate { };

        public event EventHandler<EventArgs>? NonRoslynBufferTextChanged = delegate { };
    }

    public IWorkspaceService? CreateService(HostWorkspaceServices workspaceServices) =>
        new DocumentTrackingService(workspaceServices.Workspace);
}
