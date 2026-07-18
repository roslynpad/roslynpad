using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MetadataAsSource;
using Microsoft.CodeAnalysis.Text;

namespace RoslynPad.UI;

/// <summary>
/// A read-only tab showing a generated metadata-as-source file (Source Link / embedded PDB
/// sources or decompilation). The view registers the buffer into the metadata-as-source
/// workspace, so the full editor experience (classification, quick info, further go-to
/// navigation) works inside it.
/// </summary>
internal sealed class MetadataDocumentViewModel(MetadataAsSourceFile file, MainViewModel mainViewModel) : NotificationObject, IDocumentContent
{
    private readonly TaskCompletionSource _viewReady = new();

    public string Id { get; } = Guid.NewGuid().ToString();
    public string Title { get; } = file.DocumentTitle;
    public bool IsDirty => false;
    public string FilePath { get; } = file.FilePath;
    public string Tooltip { get; } = file.DocumentTooltip;
    public MainViewModel MainViewModel { get; } = mainViewModel;

    /// <summary>The document's id in the metadata-as-source workspace; set once the view registers the buffer.</summary>
    public DocumentId? DocumentId { get; private set; }

    internal event Action<TextSpan>? NavigationRequested;

    internal Task ViewReady => _viewReady.Task;

    internal void RequestNavigation(TextSpan span) => NavigationRequested?.Invoke(span);

    internal void OnViewInitialized(DocumentId? documentId)
    {
        DocumentId = documentId;
        _viewReady.TrySetResult();
    }
}
