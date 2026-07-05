namespace Microsoft.VisualStudio.Text.Differencing
{
    using Avalonia.Controls;
    using Microsoft.VisualStudio.Text.Editor;

    /// <summary>
    /// A visual difference viewer, exposing the concrete views that display the differences.
    /// </summary>
    public interface IWpfDifferenceViewer : IDifferenceViewer
    {
        /// <summary>
        /// The view for displaying inline differences.
        /// </summary>
        new IWpfTextView InlineView { get; }

        /// <summary>
        /// The view for displaying the left buffer in side-by-side mode.
        /// </summary>
        new IWpfTextView LeftView { get; }

        /// <summary>
        /// The view for displaying the right buffer in side-by-side mode.
        /// </summary>
        new IWpfTextView RightView { get; }

        /// <summary>
        /// The visual element of this viewer.
        /// </summary>
        Control VisualElement { get; }
    }
}
