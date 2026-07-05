//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//

namespace Microsoft.VisualStudio.Text.UI.Adornments
{
    using System.Collections.Generic;
    using Microsoft.VisualStudio.Text.Adornments;

    /// <summary>
    /// Represents a unit of structure in a code <see cref="ITextBuffer"/>.
    /// </summary>
    /// <remarks>
    /// This class is an immutable subtree of the code structure in the view.
    /// </remarks>
    public interface IStructureElement
    {
        /// <summary>
        /// The <see cref="IStructureElement"/>s nested within this one.
        /// </summary>
        IReadOnlyList<IStructureElement> Children { get; }

        /// <summary>
        /// The span of text at the top of the structural element. e.g.: 'if (true)'
        /// </summary>
        SnapshotSpan? HeaderSpan { get; }

        /// <summary>
        /// The vertical span within which the structure guide line adornment should
        /// be drawn.
        /// </summary>
        SnapshotSpan? GuideLineSpan { get; }

        /// <summary>
        /// The span of text to collapse when the outlining adornment is invoked.
        /// </summary>
        SnapshotSpan? OutliningSpan { get; }

        /// <summary>
        /// The full extent of the block, from the start of the header to the end of the guideline.
        /// </summary>
        SnapshotSpan ExtentSpan { get; }

        /// <summary>
        /// The horizontal offset with which to align the structure guide line.
        /// </summary>
        SnapshotPoint? GuideLineHorizontalAnchorPoint { get; }

        /// <summary>
        /// One of the <see cref="PredefinedStructureTypes"/>, indicating the semantics
        /// of this structural element.
        /// </summary>
        string Type { get; }

        /// <summary>
        /// Indicates whether or not this element should display a collapse adornment.
        /// </summary>
        bool IsCollapsible { get; }

        /// <summary>
        /// Indicates whether or not this element should be collapsed at document open.
        /// </summary>
        bool IsDefaultCollapsed { get; }

        /// <summary>
        /// Indicates whether or not this element is related to implementation of a method,
        /// function, or property.
        /// </summary>
        bool IsImplementation { get; }

        /// <summary>
        /// Gets the text to display in the collapse adornment.
        /// </summary>
        /// <returns>The text to display in the collapse adornment.</returns>
        object GetCollapsedForm();

        /// <summary>
        /// Gets the text to display in the collapse adornment tooltip.
        /// </summary>
        /// <returns>The text displayed in the collapse adornment tooltip.</returns>
        object GetCollapsedHintForm();
    }
}
