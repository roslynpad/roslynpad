//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain internal APIs that are subject to change without notice.
// Use at your own risk.
//
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.Language.Intellisense.Test
{
    /// <summary>
    /// Interface used by tests to interact with code sense adornment instances in the editor.
    /// </summary>
    public interface ICodeLensAdornment_Test
    {
        /// <summary>
        /// Gets the ITrackingPoint at which the adornment was placed inside the editor.
        /// </summary>
        ITrackingPoint Point { get; }
    }
}
