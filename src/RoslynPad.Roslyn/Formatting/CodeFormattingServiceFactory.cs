using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Indentation;
using Microsoft.CodeAnalysis.Text;

namespace RoslynPad.Roslyn.Formatting;

[ExportLanguageServiceFactory(typeof(ICodeFormattingService), LanguageNames.CSharp), Shared]
internal class CodeFormattingServiceFactory : ILanguageServiceFactory
{
    public ILanguageService CreateLanguageService(HostLanguageServices languageServices)
    {
        var inner = languageServices.LanguageServices.GetRequiredService<ISyntaxFormattingService>();
        return new CodeFormattingServiceWrapper(inner, languageServices.LanguageServices);
    }

    private class CodeFormattingServiceWrapper(
        ISyntaxFormattingService inner,
        Microsoft.CodeAnalysis.Host.LanguageServices languageServices) : ILanguageService, ICodeFormattingService
    {
        public bool ShouldFormatOnTypedCharacter(RoslynParsedDocument document, char typedChar, int caretPosition, CancellationToken cancellationToken)
        {
            return inner.ShouldFormatOnTypedCharacter(document.Inner, typedChar, caretPosition, cancellationToken);
        }

        public ImmutableArray<TextChange> GetFormattingChangesOnTypedCharacter(RoslynParsedDocument document, int caretPosition, CancellationToken cancellationToken)
        {
            var options = IndentationOptionsProviders.GetDefault(languageServices);
            return inner.GetFormattingChangesOnTypedCharacter(document.Inner, caretPosition, options, cancellationToken);
        }
    }
}
