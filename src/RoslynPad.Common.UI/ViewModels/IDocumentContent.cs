namespace RoslynPad.UI;

/// <summary>
/// Interface for content that can be displayed as a tabbed document.
/// </summary>
public interface IDocumentContent
{
    /// <summary>
    /// Gets the title displayed in the document tab.
    /// </summary>
    string Title { get; }

    /// <summary>
    /// Gets whether the document has unsaved changes.
    /// </summary>
    bool IsDirty { get; }
}
