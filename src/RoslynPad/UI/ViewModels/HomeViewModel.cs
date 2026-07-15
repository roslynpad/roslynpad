namespace RoslynPad.UI;

/// <summary>
/// Content for the Home tab, shown as the empty state when no documents are open.
/// </summary>
public sealed class HomeViewModel : IDocumentContent
{
    public string Id => "NewDoc";

    public string Title => "Home";

    public bool IsDirty => false;
}
