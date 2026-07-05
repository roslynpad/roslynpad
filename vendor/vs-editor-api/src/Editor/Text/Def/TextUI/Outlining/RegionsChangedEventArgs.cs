//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Outlining
{
    using System;

    /// <summary>
    /// Provides information for the <see cref="IOutliningManager.RegionsChanged" /> event.
    /// </summary>
    /// <remarks>
    /// Provides the <see cref="SnapshotSpan"/> over which <see cref="ICollapsible"/> regions were added or 
    /// removed. Call GetAllRegions to get the current set of <see cref="ICollapsible"/> regions over the affected snapshot span.
    /// </remarks>
    public class RegionsChangedEventArgs : EventArgs
    {   
        /// <summary>
        /// Gets the <see cref="SnapshotSpan"/> over which collapsible spans have changed.
        /// </summary>
        public SnapshotSpan AffectedSpan { get; private set; }

        /// <summary>
        /// Initializes a new instance of <see cref="RegionsChangedEventArgs"/> with the specified <see cref="SnapshotSpan" />.
        /// </summary>
        /// <param name="affectedSpan">The <see cref="SnapshotSpan"/> over which collapsible regions have changed.</param>
        public RegionsChangedEventArgs (SnapshotSpan affectedSpan)
        {
            this.AffectedSpan = affectedSpan;
        }
    }
}

