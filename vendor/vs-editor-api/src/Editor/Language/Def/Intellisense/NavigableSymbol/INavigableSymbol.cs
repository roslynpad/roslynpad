// Copyright (c) Microsoft Corporation
// All rights reserved

using System.Collections.Generic;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Represents a navigable symbol in code document.
    /// </summary>
    public interface INavigableSymbol
    {
        /// <summary>
        /// Gets the span of the symbol.
        /// </summary>
        SnapshotSpan SymbolSpan { get; }

        /// <summary>
        /// Gets all the supported <see cref="INavigableRelationship"/>s of this symbol.
        /// </summary>
       IEnumerable<INavigableRelationship> Relationships { get; }

        /// <summary>
        /// When invoked, navigates to the target of the specified relationship to the symbol.
        /// </summary>
        void Navigate(INavigableRelationship relationship);
    }
}
