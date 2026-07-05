namespace Microsoft.VisualStudio.Text.Tagging
{
    using System.Collections.Generic;

    /// <summary>
    /// Describes a sub-heading within an <see cref="IContainerStructureTag"/>, e.g. a member
    /// within a type when the type's header is shown as a sticky scroll container.
    /// </summary>
    public sealed class SubHeadingStructureData
    {
        public SubHeadingStructureData(Span span, Span headerSpan, string type)
        {
            Span = span;
            HeaderSpan = headerSpan;
            Type = type;
        }

        /// <summary>
        /// The full span of the sub-heading's block.
        /// </summary>
        public Span Span { get; }

        /// <summary>
        /// The span of the sub-heading's header line(s).
        /// </summary>
        public Span HeaderSpan { get; }

        /// <summary>
        /// The type of the structural block, from <see cref="PredefinedStructureTagTypes"/>.
        /// </summary>
        public string Type { get; }
    }

    /// <summary>
    /// An <see cref="IStructureTag"/> that additionally exposes the sub-headings contained
    /// within the block (used by features such as sticky scroll).
    /// </summary>
    public interface IContainerStructureTag : IStructureTag
    {
        /// <summary>
        /// The sub-headings directly contained within this block, or null if there are none.
        /// </summary>
        IReadOnlyList<SubHeadingStructureData> SubHeadings { get; }
    }
}
