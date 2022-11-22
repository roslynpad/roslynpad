using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.CodeActions;

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

        public IAsyncEnumerable<CodeFixCollection> StreamFixesAsync(Document document, TextSpan textSpan, CancellationToken cancellationToken)
        {
            var result = _inner.StreamFixesAsync(document, textSpan, CodeActionRequestPriority.Normal, CodeActionOptionsProviders.GetOptionsProvider(new CodeFixContext()), isBlocking: false, _ => null, cancellationToken);
            return result.Select(x => new CodeFixCollection(x));
        }

        public CodeFixProvider? GetSuppressionFixer(string language, IEnumerable<string> diagnosticIds)
        {
            return _inner.GetSuppressionFixer(language, diagnosticIds);
        }
    }
}
