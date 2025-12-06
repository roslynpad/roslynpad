using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;

namespace RoslynPad.Roslyn.Diagnostics;

internal sealed class DiagnosticAnalyzerService(Microsoft.CodeAnalysis.Diagnostics.IDiagnosticAnalyzerService inner) : IDiagnosticAnalyzerService
{
    [ExportWorkspaceServiceFactory(typeof(IDiagnosticAnalyzerService))]
    internal class Factory : IWorkspaceServiceFactory
    {
        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return new DiagnosticAnalyzerService(workspaceServices.GetRequiredService<Microsoft.CodeAnalysis.Diagnostics.IDiagnosticAnalyzerService>());
        }
    }

    public async Task<ImmutableArray<DiagnosticData>> GetDiagnosticsForSpanAsync(TextDocument document, TextSpan? range, CancellationToken cancellationToken)
    {
        var diagnostics = await inner.GetDiagnosticsForSpanAsync(document, range, DiagnosticKind.All, cancellationToken).ConfigureAwait(false);

        return ConvertDiagnostics(diagnostics);
    }

    private static ImmutableArray<DiagnosticData> ConvertDiagnostics(ImmutableArray<Microsoft.CodeAnalysis.Diagnostics.DiagnosticData> diagnostics) =>
        diagnostics.SelectAsArray(d => new DiagnosticData(d));
}
