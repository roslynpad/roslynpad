using System;

namespace RoslynPad.Roslyn.Diagnostics
{
    public interface IDiagnosticService
    {
        event EventHandler<DiagnosticsUpdatedArgs> DiagnosticsUpdated;
    }
}
