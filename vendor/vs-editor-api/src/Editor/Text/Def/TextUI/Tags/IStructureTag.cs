//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Tagging
{
    using Microsoft.VisualStudio.Text.Adornments;

    /// <summary>
    /// Represents a structural code block, which is used for vertical structural line adornments
    /// and outlining collapse regions.
    /// </summary>
    /// <remarks>
    /// IStructureTag is the replacement for the <see cref="IBlockTag"/> which should not be used.
    /// </remarks>
    public interface IStructureTag : ITag
    {
        /// <summary>
        /// The Snapshot from which this IStructureTag was generated.
        /// </summary>
        ITextSnapshot Snapshot { get; }

        /// <summary>
        /// Gets the span containing the entire contents of the block (minus the block header).
        /// This span will be collapsed or expanded when the block outlining adornment is invoked.
        /// </summary>
        /// <remarks>
        /// If this parameter is null, block structure adornments will still be drawn as long as
        /// the <see cref="GuideLineHorizontalAnchorPoint"/> and the <see cref="GuideLineSpan"/> are both provided,
        /// however, no outlining adornment will be drawn.
        /// </remarks>
        Span? OutliningSpan { get; }

        /// <summary>
        /// Gets the span of the statement that controls the structural block.
        /// </summary>
        /// <remarks>
        /// <para>
        /// For example, in the following snippet of code,
        /// <code>
        /// if (condition1 &amp;&amp;
        ///     condition2) // comment
        /// {
        ///     something;
        /// }
        /// </code>
        /// this.HeaderSpan would extend from the start of the "if" to the end of comment.
        /// this.OutliningSpan would extend from the end of "// comment" to the end of the "}".
        /// </para>
        /// <para>
        /// If this parameter is null, block structure adornments will still be drawn as long as
        /// the <see cref="OutliningSpan"/> is provided, or the <see cref="GuideLineHorizontalAnchorPoint"/>
        /// and the <see cref="GuideLineSpan"/> are both provided.
        /// </para>
        /// </remarks>
        Span? HeaderSpan { get; }

        /// <summary>
        /// Gets the vertical span within which the block structure adornment will be drawn.
        /// </summary>
        /// <remarks>
        /// For a block to have an adornment, it must not be of type <see cref="PredefinedStructureTagTypes.Nonstructural"/>,
        /// and the implementer must also provide the GuideLineHorizontalAnchor. The adornment is drawn from the top of the
        /// line containing the start of the span to the bottom of the line containing the bottom of the span. If null,
        /// the GuideLineSpan is inferred from the OutliningSpan and the HeaderSpan.
        /// </remarks>
        Span? GuideLineSpan { get; }

        /// <summary>
        /// Gets the point with which the block structure adornment will be horizontally aligned.
        /// </summary>
        /// <remarks>
        /// This point can be on any line and is used solely for determining horizontal position. If null,
        /// or if <see cref="GuideLineSpan"/> is null, this point is computed from the HeaderSpan and
        /// OutliningSpan via heuristics.
        /// </remarks>
        int? GuideLineHorizontalAnchorPoint { get; }

        /// <summary>
        /// Determines the semantic type of the structural block.
        /// </summary>
        /// <remarks>
        /// See <see cref="PredefinedStructureTagTypes"/> for the canonical types.
        /// Use <see cref="PredefinedStructureTagTypes.Nonstructural"/> for blocks that will not have any visible affordance
        /// (but will be used for outlining).
        /// </remarks>
        string Type { get; }

        /// <summary>
        /// Determines whether a block can be collapsed.
        /// </summary>
        bool IsCollapsible { get; }

        /// <summary>
        /// Determines whether a block is collapsed by default.
        /// </summary>
        bool IsDefaultCollapsed { get; }

        /// <summary>
        /// Determines whether a block is an implementation block.
        /// </summary>
        /// <remarks>
        /// Implementation blocks are the blocks of code following a method definition.
        /// They are used for commands such as the Visual Studio Collapse to Definition command,
        /// which hides the implementation block and leaves only the method definition exposed.
        /// </remarks>
        bool IsImplementation { get; }

        /// <summary>
        /// Gets the data object for the collapsed UI. If the default is set, returns null.
        /// </summary>
        object GetCollapsedForm();

        /// <summary>
        /// Gets the data object for the collapsed UI tooltip. If the default is set, returns null.
        /// </summary>
        object GetCollapsedHintForm();
    }
}
