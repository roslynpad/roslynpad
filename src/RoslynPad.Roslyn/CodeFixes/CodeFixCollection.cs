using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Text;

namespace RoslynPad.Roslyn.CodeFixes
{
    public sealed class CodeFixCollection
    {
        private readonly Microsoft.CodeAnalysis.CodeFixes.CodeFixCollection _inner;

        public object Provider => _inner.Provider;

        public TextSpan TextSpan => _inner.TextSpan;

        public ImmutableArray<CodeFix> Fixes { get; }

        internal CodeFixCollection(Microsoft.CodeAnalysis.CodeFixes.CodeFixCollection inner)
        {
            _inner = inner;
            Fixes = inner.Fixes.Select(x => new CodeFix(x)).ToImmutableArray();
        }
    }
}