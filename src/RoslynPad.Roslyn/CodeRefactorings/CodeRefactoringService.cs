using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;

namespace RoslynPad.Roslyn.CodeRefactorings;

[Export(typeof(ICodeRefactoringService)), Shared]
[method: ImportingConstructor]
internal sealed class CodeRefactoringService(Microsoft.CodeAnalysis.CodeRefactorings.ICodeRefactoringService inner, IGlobalOptionService globalOption) : ICodeRefactoringService
{
    private readonly Microsoft.CodeAnalysis.CodeRefactorings.ICodeRefactoringService _inner = inner;
    private readonly IGlobalOptionService _globalOption = globalOption;

    public Task<bool> HasRefactoringsAsync(Document document, TextSpan textSpan, CancellationToken cancellationToken)
    {
        var options = _globalOption.GetCodeActionOptionsProvider();
        return _inner.HasRefactoringsAsync(document, textSpan, options, cancellationToken);
    }

    public async Task<IEnumerable<CodeRefactoring>> GetRefactoringsAsync(Document document, TextSpan textSpan, CancellationToken cancellationToken)
    {
        var options = _globalOption.GetCodeActionOptionsProvider();
        var result = await _inner.GetRefactoringsAsync(document, textSpan, CodeActionRequestPriority.Default,
            options, addOperationScope: _ => null, cancellationToken).ConfigureAwait(false);
        return result.Select(x => new CodeRefactoring(x)).ToArray();
    }
}
