//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain internal APIs that are subject to change without notice.
// Use at your own risk.
//
using System.Collections.Generic;
using System;

namespace Microsoft.VisualStudio.Text.OverviewMargin.Test
{
    /// <summary>
    /// Test contract for the OverviewMarkMargin (use host.GetTextViewMargin(PredefinedMarginNames.OverviewMark) as IOverviewMarkMargin_Test).
    /// </summary>
    public interface IOverviewMarkMargin_Test
    {
        /// <summary>
        /// Get a list of all marks being drawn by the margin.
        /// </summary>
        IList<Tuple<string, NormalizedSnapshotSpanCollection, int>> GetMarks();
    }
}
