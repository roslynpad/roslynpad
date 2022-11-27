using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;

namespace RoslynPad.Roslyn.CodeRefactorings;

[Export(typeof(ICodeRefactoringService)), Shared]
internal sealed class CodeRefactoringService : ICodeRefactoringService
{
    private readonly Microsoft.CodeAnalysis.CodeRefactorings.ICodeRefactoringService _inner;
    private readonly IGlobalOptionService _globalOption;

    [ImportingConstructor]
    public CodeRefactoringService(Microsoft.CodeAnalysis.CodeRefactorings.ICodeRefactoringService inner, IGlobalOptionService globalOption)
    {
        _inner = inner;
        _globalOption = globalOption;
    }

    public Task<bool> HasRefactoringsAsync(Document document, TextSpan textSpan, CancellationToken cancellationToken)
    {
        var options = _globalOption.GetCodeActionOptionsProvider();
        return _inner.HasRefactoringsAsync(document, textSpan, options, cancellationToken);
    }

    public async Task<IEnumerable<CodeRefactoring>> GetRefactoringsAsync(Document document, TextSpan textSpan, CancellationToken cancellationToken)
    {
        var options = _globalOption.GetCodeActionOptionsProvider();
        var result = await _inner.GetRefactoringsAsync(document, textSpan, CodeActionRequestPriority.Normal,
            options, isBlocking: false, addOperationScope: _ => null, cancellationToken).ConfigureAwait(false);
        return result.Select(x => new CodeRefactoring(x)).ToArray();
    }
}
