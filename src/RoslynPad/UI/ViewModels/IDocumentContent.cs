namespace RoslynPad.UI;

/// <summary>
/// Interface for content that can be displayed as a tabbed document.
/// </summary>
public interface IDocumentContent
{
    /// <summary>
    /// Gets a unique identifier for the document tab.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the title displayed in the document tab.
    /// </summary>
    string Title { get; }

    /// <summary>
    /// Gets whether the document has unsaved changes.
    /// </summary>
    bool IsDirty { get; }
}
