//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Tagging
{
    using System;
    using Microsoft.VisualStudio.Text.Adornments;
    using Microsoft.VisualStudio.Text.UI.Adornments;

    /// <summary>
    /// An implementation of <see cref="IStructureTag" />.
    /// </summary>
    /// <remarks>
    /// Using this class is the recommended way to create an instance of <see cref="IStructureElement"/>
    /// for most purposes. IStructureTag is the replacement for the IBlockTag which should not be used.
    /// </remarks>
    public class StructureTag : IStructureTag
    {
        private readonly object collapsedForm;
        private readonly object collapsedHintForm;

        /// <summary>
        /// Constructs an instance of the <see cref="IStructureTag"/>.
        /// </summary>
        /// <remarks>
        /// StructureTag is intended to replace <see cref="IBlockTag"/> and offers more explicit control
        /// of the block structure adornments. This class operates on the pay-to-play principle, in that,
        /// it will allow you to create a tag with just a subset of fields, but if a field is missing, it
        /// will attempt to guess the missing fields from the information that it has. The most useful example
        /// of this is to omit the GuideLineSpan and GuideLineHorizontalAnchor point to have the API guess
        /// them from the HeaderSpan and StatementSpan indentation. If enough information is missing, the tag
        /// does nothing.
        /// </remarks>
        /// <param name="snapshot">The snapshot used to generate this StructureTag.</param>
        /// <param name="outliningSpan">The block contents, used to determine the collapse region.</param>
        /// <param name="headerSpan">The control statement at the start of the block.</param>
        /// <param name="guideLineSpan">
        /// The vertical span within which the block structure guide is drawn.
        /// If this member is omitted, it is computed from the HeaderSpan and the OutliningSpan via heuristics.</param>
        /// <param name="guideLineHorizontalAnchor">
        /// A point capturing the horizontal offset at which the guide is drawn.
        /// If this member is omitted, it is computed from the HeaderSpan and the OutliningSpan via heuristics.</param>
        /// <param name="type">The structure type of the block.</param>
        /// <param name="isCollapsible">If true, block will have block adornments.</param>
        /// <param name="isDefaultCollapsed">If true, block is collapsed by default.</param>
        /// <param name="isImplementation">Defines whether or not the block defines a region following a function declaration.</param>
        /// <param name="collapsedForm">The form the block appears when collapsed.</param>
        /// <param name="collapsedHintForm">The form of the collapsed region tooltip.</param>
        public StructureTag(
            ITextSnapshot snapshot,
            Span? outliningSpan = null,
            Span? headerSpan = null,
            Span? guideLineSpan = null,
            int? guideLineHorizontalAnchor = null,
            string type = null,
            bool isCollapsible = false,
            bool isDefaultCollapsed = false,
            bool isImplementation = false,
            object collapsedForm = null,
            object collapsedHintForm = null)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            if (outliningSpan != null && outliningSpan.Value.End > snapshot.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(outliningSpan));
            }

            if (headerSpan != null && headerSpan.Value.End > snapshot.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(headerSpan));
            }

            if (guideLineSpan != null && guideLineSpan.Value.End > snapshot.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(guideLineSpan));
            }

            if (guideLineHorizontalAnchor != null && guideLineHorizontalAnchor.Value > snapshot.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(guideLineHorizontalAnchor));
            }

            this.Snapshot = snapshot;
            this.OutliningSpan = outliningSpan;
            this.HeaderSpan = headerSpan;
            this.GuideLineSpan = guideLineSpan;
            this.GuideLineHorizontalAnchorPoint = guideLineHorizontalAnchor;
            this.Type = type;
            this.IsCollapsible = isCollapsible;
            this.IsDefaultCollapsed = isDefaultCollapsed;
            this.IsImplementation = isImplementation;
            this.collapsedForm = collapsedForm;
            this.collapsedHintForm = collapsedHintForm;
        }

        /// <summary>
        /// The Snapshot from which this structure tag was generated.
        /// </summary>
        public virtual ITextSnapshot Snapshot { get; }

        /// <summary>
        /// Gets the span containing the entire contents of the block (minus the block header).
        /// This span will be collapsed or expanded when the block outlining adornment is invoked.
        /// </summary>
        /// <remarks>
        /// If this parameter is null, block structure adornments will still be drawn as long as
        /// the <see cref="GuideLineHorizontalAnchorPoint"/> and the <see cref="GuideLineSpan"/> are both provided,
        /// however, no outlining adornment will be drawn.
        /// </remarks>
        public virtual Span? OutliningSpan { get; set; }

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
        public virtual Span? HeaderSpan { get; }

        /// <summary>
        /// Gets the point with which the block structure adornment will be horizontally aligned.
        /// </summary>
        /// <remarks>
        /// This point can be on any line and is used solely for determining horizontal position. If null,
        /// this point is computed from the HeaderSpan and OutliningSpan via heuristics.
        /// </remarks>
        public virtual int? GuideLineHorizontalAnchorPoint { get; }

        /// <summary>
        /// Gets the vertical span within which the block structure adornment will be drawn.
        /// </summary>
        /// <remarks>
        /// For a block to have an adornment, it must not be of type <see cref="PredefinedStructureTagTypes.Nonstructural"/>,
        /// and the implementer must also provide the GuideLineHorizontalAnchor. The adornment is drawn from the top of the
        /// line containing the start of the span to the bottom of the line containing the bottom of the span. If null,
        /// the GuideLineSpan is inferred from the OutliningSpan and the HeaderSpan.
        /// </remarks>
        public virtual Span? GuideLineSpan { get; }

        /// <summary>
        /// Determines the semantic type of the structural block.
        /// </summary>
        /// <remarks>
        /// See <see cref="PredefinedStructureTagTypes"/> for the canonical types.
        /// Use <see cref="PredefinedStructureTagTypes.Nonstructural"/> for blocks that will not have any visible affordance
        /// (but will be used for outlining).
        /// </remarks>
        public virtual string Type { get; }

        /// <summary>
        /// Determines whether or not a block can be collapsed.
        /// </summary>
        public virtual bool IsCollapsible { get; }

        /// <summary>
        /// Determines whether a block is collapsed by default.
        /// </summary>
        public virtual bool IsDefaultCollapsed { get; }

        /// <summary>
        /// Determines whether a StructureTag represents an implementation block region.
        /// </summary>
        /// <remarks>
        /// Implementation blocks are the blocks of code following a method definition.
        /// They are used for commands such as the Visual Studio Collapse to Definition command,
        /// which hides the implementation block and leaves only the method definition exposed.
        /// </remarks>
        public virtual bool IsImplementation { get; }

        /// <summary>
        /// Gets the data object for the collapsed UI. If the default is set, returns null.
        /// </summary>
        public virtual object GetCollapsedForm()
        {
            return this.collapsedForm;
        }

        /// <summary>
        /// Gets the data object for the collapsed UI tooltip. If the default is set, returns null.
        /// </summary>
        public virtual object GetCollapsedHintForm()
        {
            return this.collapsedHintForm;
        }
    }
}
