// Copyright (c) Microsoft Corporation
// All rights reserved

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Provides predefined <see cref="INavigableRelationship"/>s.
    /// </summary>
    public static class PredefinedNavigableRelationships
    {
        /// <summary>
        /// A definition relationship.
        /// </summary>
        [SuppressMessage("Microsoft.Security", "CA2104", Justification = "Read only type")]
        public static readonly INavigableRelationship Definition = new DefinitionRelationship();

        private class DefinitionRelationship : INavigableRelationship
        {
            public string Name => "IsDefinedBy";

            // TODO: This must be a localized text.
            //       The assembly doesn't contain any resource for now so leave it as is.
            //       Will revisit later.
            public string DisplayName => "Definition";
        }
    }
}
