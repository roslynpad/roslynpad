using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace RoslynPad.Roslyn.Diagnostics;

public interface IDiagnosticAnalyzerService
{
    Task<ImmutableArray<DiagnosticData>> GetDiagnosticsForSpanAsync(TextDocument document, TextSpan? range, CancellationToken cancellationToken);
}
