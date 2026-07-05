//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Outlining
{
    using Microsoft.VisualStudio.Text.Tagging;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents a span that may be collapsed.
    /// </summary>
    public interface ICollapsible
    {
        /// <summary>
        /// Gets the extent of this collapsible region.
        /// </summary>
        ITrackingSpan Extent { get; }

        /// <summary>
        /// Determines whether this outlining region is collapsed.
        /// </summary>
        bool IsCollapsed { get; }

        /// <summary>
        /// Determines whether this region can be collapsed.
        /// </summary>
        bool IsCollapsible { get; }

        /// <summary>
        /// Gets the data object for the collapsed UI.
        /// </summary>
        object CollapsedForm { get; }

        /// <summary>
        /// Gets the data object for the collapsed UI tooltip.
        /// </summary>
        object CollapsedHintForm { get; }

        /// <summary>
        /// Gets the <see cref="IOutliningRegionTag"/> that was used to produce this collapsible region.
        /// </summary>
        IOutliningRegionTag Tag { get; }
    }

    /// <summary>
    /// Represents a collapsed <see cref="ICollapsible" />.
    /// </summary>
    public interface ICollapsed : ICollapsible
    {
        /// <summary>
        /// Enumerates the children of this collapsed region that are also collapsed.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if this collapsed region has been expanded.</exception>
        IEnumerable<ICollapsed> CollapsedChildren { get; }
    }
}
