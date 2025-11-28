using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Host;

namespace RoslynPad.Roslyn.Diagnostics;

public interface IDiagnosticAnalyzerService : IWorkspaceService
{
    Task<ImmutableArray<DiagnosticData>> GetDiagnosticsForSpanAsync(TextDocument document, TextSpan? range, CancellationToken cancellationToken);
}
