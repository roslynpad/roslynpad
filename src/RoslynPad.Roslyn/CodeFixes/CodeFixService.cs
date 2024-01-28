using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Options;

namespace RoslynPad.Roslyn.CodeFixes;

[Export(typeof(ICodeFixService)), Shared]
[method: ImportingConstructor]
internal sealed class CodeFixService(Microsoft.CodeAnalysis.CodeFixes.ICodeFixService inner, IGlobalOptionService globalOption) : ICodeFixService
{
    private readonly Microsoft.CodeAnalysis.CodeFixes.ICodeFixService _inner = inner;
    private readonly IGlobalOptionService _globalOption = globalOption;

    public IAsyncEnumerable<CodeFixCollection> StreamFixesAsync(Document document, TextSpan textSpan, CancellationToken cancellationToken)
    {
        var options = _globalOption.GetCodeActionOptionsProvider();
        var result = _inner.StreamFixesAsync(document, textSpan, options, cancellationToken);
        return result.Select(x => new CodeFixCollection(x));
    }

    public CodeFixProvider? GetSuppressionFixer(string language, IEnumerable<string> diagnosticIds)
    {
        return _inner.GetSuppressionFixer(language, diagnosticIds);
    }
}
