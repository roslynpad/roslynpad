namespace Microsoft.VisualStudio.Text.Tagging
{
    /// <summary>
    /// A tag identifying a matching pair of braces (or other delimiters) in the buffer.
    /// </summary>
    public interface IBracePairTag : ITag
    {
        /// <summary>
        /// The span of the opening brace, or null if there is no opening brace.
        /// </summary>
        SnapshotSpan? Start { get; }

        /// <summary>
        /// The span of the closing brace, or null if there is no closing brace.
        /// </summary>
        SnapshotSpan? End { get; }
    }

    /// <summary>
    /// Default implementation of <see cref="IBracePairTag"/>.
    /// </summary>
    public class BracePairTag : IBracePairTag
    {
        public BracePairTag(SnapshotSpan? start, SnapshotSpan? end)
        {
            Start = start;
            End = end;
        }

        public SnapshotSpan? Start { get; }

        public SnapshotSpan? End { get; }
    }
}
