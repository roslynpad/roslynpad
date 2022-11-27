using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Options;

namespace RoslynPad.Roslyn.CodeFixes;

[Export(typeof(ICodeFixService)), Shared]
internal sealed class CodeFixService : ICodeFixService
{
    private readonly Microsoft.CodeAnalysis.CodeFixes.ICodeFixService _inner;
    private readonly IGlobalOptionService _globalOption;

    [ImportingConstructor]
    public CodeFixService(Microsoft.CodeAnalysis.CodeFixes.ICodeFixService inner, IGlobalOptionService globalOption)
    {
        _inner = inner;
        _globalOption = globalOption;
    }

    public IAsyncEnumerable<CodeFixCollection> StreamFixesAsync(Document document, TextSpan textSpan, CancellationToken cancellationToken)
    {
        var options = _globalOption.GetCodeActionOptionsProvider();
        var result = _inner.StreamFixesAsync(document, textSpan, options, isBlocking: false, cancellationToken);
        return result.Select(x => new CodeFixCollection(x));
    }

    public CodeFixProvider? GetSuppressionFixer(string language, IEnumerable<string> diagnosticIds)
    {
        return _inner.GetSuppressionFixer(language, diagnosticIds);
    }
}
