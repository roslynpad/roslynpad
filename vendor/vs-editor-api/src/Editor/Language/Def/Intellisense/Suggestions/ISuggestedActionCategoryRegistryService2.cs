// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Language.Intellisense
{
    using System;
    using System.Collections.Generic;
    using Microsoft.VisualStudio.Utilities;

    /// <summary>
    /// The service that maintains the collection of suggested action categories.
    /// </summary>
    /// <remarks>This is a MEF component part, and should be exported with the following attribute:
    /// [Export(typeof(ISuggestedActionCategoryRegistryService2))]
    /// </remarks>
    [CLSCompliant(false)]
    public interface ISuggestedActionCategoryRegistryService2 : ISuggestedActionCategoryRegistryService
    {
        /// <summary>
        /// Accepts an <see cref="ISuggestedActionCategorySet"/> of <see cref="ISuggestedActionCategory"/>s and
        /// sorts them in order from highest to lowest precedence.
        /// </summary>
        /// <remarks>
        /// Precedence is defined by <see cref="OrderAttribute"/>s present on <see cref="SuggestedActionCategoryDefinition"/>s.
        /// </remarks>
        /// <param name="categorySet">A set of categories to sort.</param>
        /// <returns>An enumerable of sorted categories.</returns>
        IEnumerable<ISuggestedActionCategory> OrderCategoriesByPrecedence(ISuggestedActionCategorySet categorySet);
    }
}
