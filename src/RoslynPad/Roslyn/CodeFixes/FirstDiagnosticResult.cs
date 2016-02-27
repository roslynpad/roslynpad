using RoslynPad.Roslyn.Diagnostics;
using RoslynPad.Utilities;

namespace RoslynPad.Roslyn.CodeFixes
{
    public struct FirstDiagnosticResult
    {
        public bool PartialResult { get; }

        public bool HasFix { get; }

        public DiagnosticData Diagnostic { get; }

        public FirstDiagnosticResult(object inner)
        {
            PartialResult = inner.GetFieldValue<bool>(nameof(PartialResult));
            HasFix = inner.GetFieldValue<bool>(nameof(HasFix));
            Diagnostic = inner.GetFieldValue<DiagnosticData>(nameof(Diagnostic));
        }
    }
}