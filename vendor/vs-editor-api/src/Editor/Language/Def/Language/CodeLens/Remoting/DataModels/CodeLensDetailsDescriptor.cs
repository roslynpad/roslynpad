//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
using System.Collections.Generic;

namespace Microsoft.VisualStudio.Language.CodeLens.Remoting
{
    /// <summary>
    /// Represents a descriptor for the detail of a data point.
    /// </summary>
    /// <remarks>
    /// When <see cref="IAsyncCodeLensDataPoint.GetDetailsAsync"/> is called,
    /// the data point returns a <see cref="CodeLensDetailsDescriptor"/> object providing the data point details
    /// that will be presented in the details popup.
    /// </remarks>
    public sealed class CodeLensDetailsDescriptor
    {
        /// <summary>
        /// Defines the headers of the detail list.
        /// </summary>
        public IEnumerable<CodeLensDetailHeaderDescriptor> Headers { get; set; }

        /// <summary>
        /// Defines rows (entries) of the detail list.
        /// </summary>
        public IEnumerable<CodeLensDetailEntryDescriptor> Entries { get; set; }

        /// <summary>
        /// Defines the additional navigation commands in the details pane
        /// </summary>
        public IEnumerable<CodeLensDetailPaneCommand> PaneNavigationCommands { get; set; }
    }
}
