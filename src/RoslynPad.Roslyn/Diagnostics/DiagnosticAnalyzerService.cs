using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace RoslynPad.Roslyn.Diagnostics;

[Export(typeof(IDiagnosticAnalyzerService)), Shared]
[method: ImportingConstructor]
internal sealed class DiagnosticAnalyzerService(Microsoft.CodeAnalysis.Diagnostics.IDiagnosticAnalyzerService inner) : IDiagnosticAnalyzerService
{
    public async Task<ImmutableArray<DiagnosticData>> GetDiagnosticsForSolutionAsync(Solution solution, CancellationToken cancellationToken)
    {
        var diagnostics = await inner.GetDiagnosticsForIdsAsync(solution, projectId: null, documentId: null, diagnosticIds: null, shouldIncludeAnalyzer: null, includeSuppressedDiagnostics: false, includeLocalDocumentDiagnostics: true, includeNonLocalDocumentDiagnostics: true, cancellationToken).ConfigureAwait(false);

        return ConvertDiagnostics(diagnostics);
    }

    public async Task<ImmutableArray<DiagnosticData>> GetDiagnosticsForSpanAsync(TextDocument document, TextSpan? range, CancellationToken cancellationToken)
    {
        var diagnostics = await inner.GetDiagnosticsForSpanAsync(document, range, cancellationToken).ConfigureAwait(false);

        return ConvertDiagnostics(diagnostics);
    }

    private static ImmutableArray<DiagnosticData> ConvertDiagnostics(ImmutableArray<Microsoft.CodeAnalysis.Diagnostics.DiagnosticData> diagnostics) =>
        diagnostics.Select(d => new DiagnosticData(d)).ToImmutableArray();
}
