// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Language.Intellisense
{
    using System;
    using System.Collections.Generic;
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Utilities;

    /// <summary>
    /// Extends <see cref="ISuggestedActionsSource2"/> with an overload of
    /// <see cref="ISuggestedActionsSource.GetSuggestedActions(ISuggestedActionCategorySet, SnapshotSpan, System.Threading.CancellationToken)"/>
    /// that accepts an <see cref="IUIThreadOperationContext"/>.
    /// </summary>
    [CLSCompliant(false)]
    public interface ISuggestedActionsSource3 : ISuggestedActionsSource2
    {
        /// <summary>
        /// Synchronously returns a list of suggested actions for a given span of text, with UI-thread
        /// operation tracking.
        /// </summary>
        IEnumerable<SuggestedActionSet> GetSuggestedActions(
            ISuggestedActionCategorySet requestedActionCategories,
            SnapshotSpan range,
            IUIThreadOperationContext operationContext);
    }
}
