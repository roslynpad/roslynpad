using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace RoslynPad.Roslyn.Diagnostics
{
    [Export(typeof(IDiagnosticService)), Shared]
    internal sealed class DiagnosticsService : IDiagnosticService
    {
        private readonly Microsoft.CodeAnalysis.Diagnostics.IDiagnosticService _inner;

        [ImportingConstructor]
        public DiagnosticsService(Microsoft.CodeAnalysis.Diagnostics.IDiagnosticService inner)
        {
            _inner = inner;
            inner.DiagnosticsUpdated += OnDiagnosticsUpdated;
        }

        private void OnDiagnosticsUpdated(object sender, Microsoft.CodeAnalysis.Diagnostics.DiagnosticsUpdatedArgs e)
        {
            DiagnosticsUpdated?.Invoke(this, new DiagnosticsUpdatedArgs(e));
        }

        public event EventHandler<DiagnosticsUpdatedArgs>? DiagnosticsUpdated;

        public IEnumerable<DiagnosticData> GetDiagnostics(Workspace workspace, ProjectId projectId, DocumentId documentId, object id,
            bool includeSuppressedDiagnostics, CancellationToken cancellationToken)
        {
            // TODO: migrate to diagnostics push/pull model
#pragma warning disable CS0618 // Type or member is obsolete
            return _inner.GetDiagnostics(workspace, projectId, documentId, id, includeSuppressedDiagnostics,
                cancellationToken).Select(x => new DiagnosticData(x));
#pragma warning restore CS0618 // Type or member is obsolete
        }
    }
}
