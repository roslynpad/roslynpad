using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using RoslynPad.Utilities;

namespace RoslynPad.Roslyn.CodeFixes
{
    public sealed class CodeFix
    {
        public Project Project { get; }

        public CodeAction Action { get; }

        public ImmutableArray<Diagnostic> Diagnostics { get; }

        public Diagnostic PrimaryDiagnostic => Diagnostics[0];

        internal CodeFix(object inner)
        {
            Project = inner.GetFieldValue<Project>(nameof(Project));
            Action = inner.GetFieldValue<CodeAction>(nameof(Action));
            Diagnostics = inner.GetFieldValue<ImmutableArray<Diagnostic>>(nameof(Diagnostics));
        }
    }
}