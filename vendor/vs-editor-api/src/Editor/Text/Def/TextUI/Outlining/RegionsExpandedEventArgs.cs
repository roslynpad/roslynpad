//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Outlining
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Provides information for the <see cref="IOutliningManager.RegionsExpanded" /> event.
    /// </summary>
    /// <remarks>
    /// Provides the <see cref="ICollapsible"/> regions that are now expanded.
    /// </remarks>
    public class RegionsExpandedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the <see cref="ICollapsible"/> regions which are now expanded.
        /// </summary>
        public IEnumerable<ICollapsible> ExpandedRegions { get; private set; }

        /// <summary>
        /// <c>true</c> if the regions are being expanded because they are being removed.
        /// </summary>
        public bool RemovalPending { get; private set; }

        /// <summary>
        /// Initializes a new instance of <see cref="RegionsExpandedEventArgs"/> with the specified <see cref="ICollapsible"/> regions, assuming that they are not also being removed.
        /// </summary>
        /// <param name="expandedRegions">The newly-expanded regions.</param>
        public RegionsExpandedEventArgs (IEnumerable<ICollapsible> expandedRegions) : this(expandedRegions, false)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="RegionsExpandedEventArgs"/> with the specified <see cref="ICollapsible"/> regions.
        /// </summary>
        /// <param name="expandedRegions">The newly-expanded regions.</param>
        /// <param name="removalPending">If these regions are being expanded as part of being removed.</param>
        public RegionsExpandedEventArgs (IEnumerable<ICollapsible> expandedRegions, bool removalPending)
        {
            this.ExpandedRegions = expandedRegions;
            this.RemovalPending = removalPending;
        }
    }
}

