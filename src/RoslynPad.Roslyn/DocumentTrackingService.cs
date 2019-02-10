using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace RoslynPad.Roslyn
{
    public interface IDocumentTrackingService : IWorkspaceService
    {
        event EventHandler<DocumentId> ActiveDocumentChanged;
        event EventHandler<EventArgs> NonRoslynBufferTextChanged;
        DocumentId GetActiveDocument();
        DocumentId? TryGetActiveDocument();
        ImmutableArray<DocumentId> GetVisibleDocuments();
    }

    [ExportWorkspaceServiceFactory(typeof(Microsoft.CodeAnalysis.IDocumentTrackingService))]
    internal sealed class DocumentTrackingServiceFactory : IWorkspaceServiceFactory
    {
        private class DocumentTrackingService : Microsoft.CodeAnalysis.IDocumentTrackingService
        {
            private readonly IDocumentTrackingService _inner;

            public DocumentTrackingService(IDocumentTrackingService inner)
            {
                _inner = inner;
            }

            public event EventHandler<DocumentId> ActiveDocumentChanged
            {
                add => _inner.ActiveDocumentChanged += value;
                remove => _inner.ActiveDocumentChanged -= value;
            }

            public event EventHandler<EventArgs> NonRoslynBufferTextChanged
            {
                add => _inner.NonRoslynBufferTextChanged += value;
                remove => _inner.NonRoslynBufferTextChanged -= value;
            }

            public DocumentId GetActiveDocument() => _inner.GetActiveDocument();

            public ImmutableArray<DocumentId> GetVisibleDocuments() => _inner.GetVisibleDocuments();

            public DocumentId? TryGetActiveDocument() => _inner.TryGetActiveDocument();
        }

        public IWorkspaceService? CreateService(HostWorkspaceServices workspaceServices)
        {
            var innerService = workspaceServices.GetService<IDocumentTrackingService>();
            return innerService != null ? new DocumentTrackingService(innerService) : null;
        }
    }
}