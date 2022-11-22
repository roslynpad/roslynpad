using System;
using System.Composition;

namespace RoslynPad.Roslyn.Diagnostics;

[Export(typeof(IDiagnosticService)), Shared]
internal sealed class DiagnosticsService : IDiagnosticService
{
    [ImportingConstructor]
    public DiagnosticsService(Microsoft.CodeAnalysis.Diagnostics.IDiagnosticService inner)
    {
        inner.DiagnosticsUpdated += OnDiagnosticsUpdated;
    }

    private void OnDiagnosticsUpdated(object? sender, Microsoft.CodeAnalysis.Diagnostics.DiagnosticsUpdatedArgs e)
    {
        DiagnosticsUpdated?.Invoke(this, new DiagnosticsUpdatedArgs(e));
    }

    public event EventHandler<DiagnosticsUpdatedArgs>? DiagnosticsUpdated;
}
