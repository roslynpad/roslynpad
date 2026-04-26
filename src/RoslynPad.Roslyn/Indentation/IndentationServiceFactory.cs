using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Indentation;

namespace RoslynPad.Roslyn.Indentation;

[ExportLanguageServiceFactory(typeof(IIndentationService), LanguageNames.CSharp), Shared]
internal class IndentationServiceFactory : ILanguageServiceFactory
{
    public ILanguageService CreateLanguageService(HostLanguageServices languageServices)
    {
        var inner = languageServices.LanguageServices.GetRequiredService<Microsoft.CodeAnalysis.Indentation.IIndentationService>();
        return new IndentationServiceWrapper(inner, languageServices.LanguageServices);
    }

    private class IndentationServiceWrapper(
        Microsoft.CodeAnalysis.Indentation.IIndentationService inner,
        Microsoft.CodeAnalysis.Host.LanguageServices languageServices) : ILanguageService, IIndentationService
    {
        public IndentationResult GetIndentation(RoslynParsedDocument document, int lineNumber, CancellationToken cancellationToken)
        {
            var options = IndentationOptionsProviders.GetDefault(languageServices);
            var result = inner.GetIndentation(document.Inner, lineNumber, options, cancellationToken);
            return new IndentationResult(result.BasePosition, result.Offset);
        }
    }
}
