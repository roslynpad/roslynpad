using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Text;

namespace RoslynPad.Roslyn.CodeFixes
{
    [Export(typeof(ICodeFixService)), Shared]
    internal sealed class CodeFixService : ICodeFixService
    {
        private readonly Microsoft.CodeAnalysis.CodeFixes.ICodeFixService _inner;

        [ImportingConstructor]
        public CodeFixService(Microsoft.CodeAnalysis.CodeFixes.ICodeFixService inner)
        {
            _inner = inner;
        }
        
        public async Task<IEnumerable<CodeFixCollection>> GetFixesAsync(Document document, TextSpan textSpan, bool includeSuppressionFixes,
            CancellationToken cancellationToken)
        {
            var result = await _inner.GetFixesAsync(document, textSpan, includeSuppressionFixes, cancellationToken).ConfigureAwait(false);
            return result.Select(x => new CodeFixCollection(x)).ToImmutableArray();
        }

        public async Task<FirstDiagnosticResult> GetFirstDiagnosticWithFixAsync(Document document, TextSpan textSpan, bool considerSuppressionFixes,
            CancellationToken cancellationToken)
        {
            var result = await _inner.GetFirstDiagnosticWithFixAsync(document, textSpan, considerSuppressionFixes, cancellationToken).ConfigureAwait(false);
            return new FirstDiagnosticResult(result);
        }

        public CodeFixProvider GetSuppressionFixer(string language, IEnumerable<string> diagnosticIds)
        {
            return _inner.GetSuppressionFixer(language, diagnosticIds);
        }
    }
}