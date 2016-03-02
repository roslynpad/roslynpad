using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;

namespace RoslynPad.Roslyn.CodeFixes
{
    public sealed class CodeFix
    {
        private readonly Microsoft.CodeAnalysis.CodeFixes.CodeFix _inner;

        public Project Project => _inner.Project;

        public CodeAction Action => _inner.Action;

        public ImmutableArray<Diagnostic> Diagnostics => _inner.Diagnostics;

        public Diagnostic PrimaryDiagnostic => _inner.PrimaryDiagnostic;

        internal CodeFix(Microsoft.CodeAnalysis.CodeFixes.CodeFix inner)
        {
            _inner = inner;
        }
    }
}