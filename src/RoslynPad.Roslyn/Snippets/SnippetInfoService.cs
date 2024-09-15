using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;

namespace RoslynPad.Roslyn.Snippets;

[ExportLanguageService(typeof(Microsoft.CodeAnalysis.Snippets.ISnippetInfoService), LanguageNames.CSharp)]
[method: ImportingConstructor]
internal sealed class SnippetInfoService([Import(AllowDefault = true)] ISnippetInfoService inner) : Microsoft.CodeAnalysis.Snippets.ISnippetInfoService
{
    public IEnumerable<Microsoft.CodeAnalysis.Snippets.SnippetInfo> GetSnippetsIfAvailable()
    {
        return inner?.GetSnippets().Select(x => 
            new Microsoft.CodeAnalysis.Snippets.SnippetInfo(x.Shortcut, x.Title, x.Description, null))
            ?? [];
    }

    public bool SnippetShortcutExists_NonBlocking(string shortcut)
    {
        return false;
    }

    public bool ShouldFormatSnippet(Microsoft.CodeAnalysis.Snippets.SnippetInfo snippetInfo)
    {
        return false;
    }
}
