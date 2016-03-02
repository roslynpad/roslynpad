using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;

namespace RoslynPad.Roslyn.CodeFixes
{
    public sealed class FixAllCodeActionContext
    {
        private readonly Microsoft.CodeAnalysis.CodeFixes.FixAllCodeActionContext _inner;

        public FixAllProvider FixAllProvider => _inner.FixAllProvider;
        public IEnumerable<Diagnostic> OriginalDiagnostics => _inner.OriginalDiagnostics;
        public IEnumerable<FixAllScope> SupportedScopes => _inner.SupportedScopes;
        
        internal FixAllCodeActionContext(Microsoft.CodeAnalysis.CodeFixes.FixAllCodeActionContext inner)
        {
            _inner = inner;
        }
    }
}