using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace RoslynPad.Roslyn.CodeRefactorings
{
    [Export(typeof(ICodeRefactoringService)), Shared]
    internal sealed class CodeRefactoringService : ICodeRefactoringService
    {
        private readonly Microsoft.CodeAnalysis.CodeRefactorings.ICodeRefactoringService _inner;

        [ImportingConstructor]
        public CodeRefactoringService(Microsoft.CodeAnalysis.CodeRefactorings.ICodeRefactoringService inner)
        {
            _inner = inner;
        }

        public Task<bool> HasRefactoringsAsync(Document document, TextSpan textSpan, CancellationToken cancellationToken)
        {
            return _inner.HasRefactoringsAsync(document, textSpan, cancellationToken);
        }

        public async Task<IEnumerable<CodeRefactoring>> GetRefactoringsAsync(Document document, TextSpan textSpan, CancellationToken cancellationToken)
        {
            var result = await _inner.GetRefactoringsAsync(document, textSpan, cancellationToken).ConfigureAwait(false);
            return result.Select(x => new CodeRefactoring(x)).ToArray();
        }
    }
}