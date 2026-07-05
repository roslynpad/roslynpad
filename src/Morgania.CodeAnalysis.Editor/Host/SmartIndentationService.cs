using System.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Morgania.CodeAnalysis.Editor;

/// <summary>
/// The smart-indentation seam the editor's <c>IEditorOperations</c> imports. Visual Studio's
/// implementation was never open-sourced; this one does what it did — find the
/// <see cref="ISmartIndentProvider"/> matching the buffer's content type (Roslyn exports one
/// for C#) and cache the resulting <see cref="ISmartIndent"/> on the view.
/// </summary>
[Shared]
[Export(typeof(ISmartIndentationService))]
public sealed class SmartIndentationService : ISmartIndentationService
{
    private readonly Lazy<ISmartIndentProvider, ContentTypeMetadata>[] _providers;

    [ImportingConstructor]
    public SmartIndentationService([ImportMany] Lazy<ISmartIndentProvider, ContentTypeMetadata>[] providers)
    {
        _providers = providers;
    }

    public int? GetDesiredIndentation(ITextView textView, ITextSnapshotLine line)
    {
        if (textView.IsClosed)
        {
            return null;
        }

        var smartIndent = textView.Properties.GetOrCreateSingletonProperty(
            typeof(SmartIndentationService),
            () => CreateSmartIndent(textView));
        return smartIndent?.GetDesiredIndentation(line);
    }

    private ISmartIndent? CreateSmartIndent(ITextView textView)
    {
        var contentType = textView.TextBuffer.ContentType;
        var smartIndent = _providers
            .Where(provider => provider.Metadata.ContentTypes.Any(contentType.IsOfType))
            .Select(provider => provider.Value.CreateSmartIndent(textView))
            .FirstOrDefault(indent => indent is not null);

        if (smartIndent is not null)
        {
            textView.Closed += (_, _) => smartIndent.Dispose();
        }

        return smartIndent;
    }
}
