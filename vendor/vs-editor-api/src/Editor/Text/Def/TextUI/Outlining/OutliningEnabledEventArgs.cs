//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Outlining
{
    using System;

    /// <summary>
    /// Provides information for the <see cref="IOutliningManager.OutliningEnabledChanged" /> event.
    /// </summary>
    /// <remarks>
    /// The event is raised when outlining has been enabled or disabled.
    /// </remarks>
    public class OutliningEnabledEventArgs : EventArgs
    {
        /// <summary>
        /// Determines whether outlining has been enabled or disabled.
        /// </summary>
        public bool Enabled { get; private set; }

        /// <summary>
        /// Initializes a new instance of <see cref="OutliningEnabledEventArgs"/> for the specified change.
        /// </summary>
        /// <param name="enabled"><c>true</c> if outlining has been enabled, <c>false</c> if it has been disabled.</param>
        public OutliningEnabledEventArgs(bool enabled)
        {
            this.Enabled = enabled;
        }
    }
}

