using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace RoslynPad.Roslyn
{
    [ExportWorkspaceServiceFactory(typeof(IDocumentTrackingService))]
    internal sealed class DocumentTrackingServiceFactory : IWorkspaceServiceFactory
    {
        private class DocumentTrackingService : IDocumentTrackingService
        {
            private readonly RoslynWorkspace _workspace;

            public DocumentTrackingService(Workspace workspace)
            {
                _workspace = (RoslynWorkspace)workspace;
            }

            public DocumentId GetActiveDocument()
            {
                return _workspace.OpenDocumentId;
            }

            public ImmutableArray<DocumentId> GetVisibleDocuments()
            {
                return ImmutableArray.Create(_workspace.OpenDocumentId);
            }

            public event EventHandler<DocumentId> ActiveDocumentChanged;

            public event EventHandler<EventArgs> NonRoslynBufferTextChanged;

            private void OnActiveDocumentChanged(DocumentId e)
            {
                ActiveDocumentChanged?.Invoke(this, e);
            }

            private void OnNonRoslynBufferTextChanged()
            {
                NonRoslynBufferTextChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return new DocumentTrackingService(workspaceServices.Workspace);
        }
    }
}