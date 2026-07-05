//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Outlining
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Provides information for the <see cref="IOutliningManager.RegionsCollapsed" /> event.
    /// </summary>
    /// <remarks>
    /// Provides the <see cref="ICollapsed"/> regions that are now collapsed.
    /// </remarks>
    public class RegionsCollapsedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the <see cref="ICollapsed" /> regions that are now collapsed.
        /// </summary>
        public IEnumerable<ICollapsed> CollapsedRegions { get; private set; }

        /// <summary>
        /// Initializes a new instance of <see cref="RegionsCollapsedEventArgs"/> with the specified <see cref="ICollapsed" /> regions.
        /// </summary>
        /// <param name="collapsedRegions">The newly-collapsed regions.</param>
        public RegionsCollapsedEventArgs(IEnumerable<ICollapsed> collapsedRegions)
        {
            this.CollapsedRegions = collapsedRegions;
        }
    }
}

