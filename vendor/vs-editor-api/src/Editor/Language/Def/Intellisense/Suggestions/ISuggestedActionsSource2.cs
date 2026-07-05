// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Language.Intellisense
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.Text;

    /// <summary>
    /// Represents a provider of suggested actions for a span of text in a <see cref="ITextBuffer" />.
    /// <see cref="ISuggestedActionsSource"/> instances are created by <see cref="ISuggestedActionsSourceProvider"/> 
    /// MEF components matching text buffer's content type.
    /// </summary>
    [CLSCompliant(false)]
    public interface ISuggestedActionsSource2 : ISuggestedActionsSource
    {
        /// <summary>
        /// Gets a <see cref="ISuggestedActionCategorySet"/> which are known to have <see cref="ISuggestedAction"/>s
        /// which are applicable to the span of text defined by <paramref name="range"/>.
        /// </summary>
        /// <param name="requestedActionCategories">A set of suggested action categories requested.</param>
        /// <param name="range">A span of text in the <see cref="ITextBuffer" /> over which to return suggested actions.</param>
        /// <param name="cancellationToken">A cancellation token that allows to cancel getting list of suggested actions.</param>
        /// <returns>
        /// A <see cref="ISuggestedActionCategorySet"/> containing the set of categories with actions applicable to <paramref name="range"/>.
        /// Implementers are encouraged to use the predefined sets on <see cref="ISuggestedActionCategoryRegistryService"/>.
        /// </returns>
        /// <remarks>
        /// Usage of this method supersedes
        /// <see cref="ISuggestedActionsSource.HasSuggestedActionsAsync(ISuggestedActionCategorySet, SnapshotSpan, CancellationToken)"/>.
        /// Implementers must return this same set of categories from
        /// <see cref="ISuggestedActionsSource.GetSuggestedActions(ISuggestedActionCategorySet, SnapshotSpan, CancellationToken)"/>,
        /// mapping each category to an ISuggestedActionSet containing all of the actions for that category.
        /// </remarks>
        Task<ISuggestedActionCategorySet> GetSuggestedActionCategoriesAsync(
            ISuggestedActionCategorySet requestedActionCategories,
            SnapshotSpan range,
            CancellationToken cancellationToken);
    }
}
