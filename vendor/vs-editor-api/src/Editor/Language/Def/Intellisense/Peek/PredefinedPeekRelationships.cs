// Copyright (c) Microsoft Corporation
// All rights reserved

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Predefined Peek relationships.
    /// </summary>
    public static class PredefinedPeekRelationships
    {
        /// <summary>
        /// A relationship describing a connection between an <see cref="IPeekableItem"/> and its definition.
        /// </summary>
        [SuppressMessage("Microsoft.Security", "CA2104", Justification = "Read only type")]
        public static readonly IPeekRelationship Definitions = new DefinitionRelationship();

        private class DefinitionRelationship : IPeekRelationship
        {
            public string Name
            {
                get { return "IsDefinedBy"; }
            }

            public string DisplayName
            {
                get { return "Is Defined By"; }
            }
        }
    }
}
