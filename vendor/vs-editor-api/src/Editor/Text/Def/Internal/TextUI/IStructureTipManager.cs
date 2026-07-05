namespace Microsoft.VisualStudio.Text.Structure
{
    using Microsoft.VisualStudio.Text.Editor;

    /// <summary>
    /// Facilitates invocation of Structure Guide Lines tooltip.
    /// </summary>
    public interface IStructureTipManager
    {
        /// <summary>
        /// Gets whether or not Structure Tips are available in the current view.
        /// </summary>
        /// <param name="textView">The current view.</param>
        /// <returns>Returns true if structure tips are available.</returns>
        bool CanTriggerStructureTip(ITextView textView);

        /// <summary>
        /// Displays the structure guide lines tooltip containing the context at the
        /// specified trigger point.
        /// </summary>
        /// <param name="textView">The textview to display the tip for.</param>
        /// <param name="point">The point to display context for.</param>
        void TriggerStructureTip(ITextView textView, SnapshotPoint point);
    }
}
