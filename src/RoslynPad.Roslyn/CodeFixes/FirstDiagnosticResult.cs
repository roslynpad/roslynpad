using RoslynPad.Roslyn.Diagnostics;

namespace RoslynPad.Roslyn.CodeFixes
{
    public struct FirstDiagnosticResult
    {
        private readonly Microsoft.CodeAnalysis.CodeFixes.FirstDiagnosticResult _inner;
        public bool PartialResult => _inner.PartialResult;

        public bool HasFix => _inner.HasFix;

        public DiagnosticData Diagnostic { get; }

        internal FirstDiagnosticResult(Microsoft.CodeAnalysis.CodeFixes.FirstDiagnosticResult inner)
        {
            _inner = inner;
            Diagnostic = new DiagnosticData(inner.Diagnostic);
        }
    }
}