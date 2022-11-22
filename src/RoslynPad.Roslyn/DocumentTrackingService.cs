using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace RoslynPad.Roslyn;

[ExportWorkspaceServiceFactory(typeof(IDocumentTrackingService), ServiceLayer.Host)]
internal sealed class DocumentTrackingServiceFactory : IWorkspaceServiceFactory
{
    private class DocumentTrackingService : IDocumentTrackingService
    {
        private readonly RoslynWorkspace _workspace;

        public bool SupportsDocumentTracking => true;

        public DocumentTrackingService(Workspace workspace)
        {
            _workspace = (RoslynWorkspace)workspace;
        }

        public DocumentId GetActiveDocument() => _workspace.OpenDocumentId ?? throw new InvalidOperationException("No active document");

        public DocumentId? TryGetActiveDocument() => _workspace.OpenDocumentId;

        public ImmutableArray<DocumentId> GetVisibleDocuments() => _workspace.OpenDocumentId != null ? ImmutableArray.Create(_workspace.OpenDocumentId) : ImmutableArray<DocumentId>.Empty;

        public event EventHandler<DocumentId?>? ActiveDocumentChanged = delegate { };

        public event EventHandler<EventArgs>? NonRoslynBufferTextChanged = delegate { };
    }

    public IWorkspaceService? CreateService(HostWorkspaceServices workspaceServices) =>
        new DocumentTrackingService(workspaceServices.Workspace);
}
