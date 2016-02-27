using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using RoslynPad.Utilities;

namespace RoslynPad.Roslyn.CodeFixes
{
    public sealed class FixAllCodeActionContext : FixAllContext
    {
        public FixAllProvider FixAllProvider { get; }
        public IEnumerable<Diagnostic> OriginalDiagnostics { get; }
        public IEnumerable<FixAllScope> SupportedScopes { get; }

        private FixAllCodeActionContext(Document document, FixAllProvider fixAllProvider, CodeFixProvider originalFixProvider, IEnumerable<Diagnostic> originalFixDiagnostics, ImmutableHashSet<string> diagnosticIds, DiagnosticProvider diagnosticProvider, CancellationToken cancellationToken) : base(document, originalFixProvider, FixAllScope.Document, null, diagnosticIds, diagnosticProvider, cancellationToken)
        {
            FixAllProvider = fixAllProvider;
            OriginalDiagnostics = originalFixDiagnostics;
            SupportedScopes = fixAllProvider.GetSupportedFixAllScopes();
        }

        private FixAllCodeActionContext(Project project, FixAllProvider fixAllProvider, CodeFixProvider originalFixProvider, IEnumerable<Diagnostic> originalFixDiagnostics, ImmutableHashSet<string> diagnosticIds, DiagnosticProvider diagnosticProvider, CancellationToken cancellationToken) : base(project, originalFixProvider, FixAllScope.Project, null, diagnosticIds, diagnosticProvider, cancellationToken)
        {
            FixAllProvider = fixAllProvider;
            OriginalDiagnostics = originalFixDiagnostics;
            SupportedScopes = fixAllProvider.GetSupportedFixAllScopes();
        }

        public FixAllCodeActionContext(FixAllContext fixAllContext, Document document)
            : this(document, fixAllContext.GetPropertyValue<FixAllProvider>(nameof(FixAllProvider)), fixAllContext.CodeFixProvider,
                fixAllContext.GetPropertyValue<IEnumerable<Diagnostic>>(nameof(OriginalDiagnostics)), fixAllContext.DiagnosticIds,
                fixAllContext.GetFieldValue<DiagnosticProvider>("_diagnosticProvider"), fixAllContext.CancellationToken)
        {
        }

        public FixAllCodeActionContext(FixAllContext fixAllContext, Project project)
            : this(project, fixAllContext.GetPropertyValue<FixAllProvider>(nameof(FixAllProvider)), fixAllContext.CodeFixProvider,
                fixAllContext.GetPropertyValue<IEnumerable<Diagnostic>>(nameof(OriginalDiagnostics)), fixAllContext.DiagnosticIds,
                fixAllContext.GetFieldValue<DiagnosticProvider>("_diagnosticProvider"), fixAllContext.CancellationToken)
        {
        }
    }
}