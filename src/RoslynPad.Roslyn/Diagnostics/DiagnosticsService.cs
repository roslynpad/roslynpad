using System.Collections.Immutable;
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

    private void OnDiagnosticsUpdated(object? sender, ImmutableArray<Microsoft.CodeAnalysis.Diagnostics.DiagnosticsUpdatedArgs> e)
    {
        foreach (var diagnostic in e)
        {
            DiagnosticsUpdated?.Invoke(this, new DiagnosticsUpdatedArgs(diagnostic));
        }
    }

    public event EventHandler<DiagnosticsUpdatedArgs>? DiagnosticsUpdated;
}
