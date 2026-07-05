// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Represents a relationship between an <see cref="INavigableSymbol"/> and its navigation target.
    /// </summary>
    public interface INavigableRelationship
    {
        /// <summary>
        /// Gets the unique name identifying this relastionship.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the localized display name of this relationship.
        /// </summary>
        string DisplayName { get; }
    }
}
