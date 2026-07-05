using System;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.Utilities
{
    /// <summary>
    /// Custom orderer for sorting lists of items associated with content types that preserves original order of items with unrelated content types.
    /// </summary>
    internal class StableContentTypeOrderer<T, M> where M : IContentTypeMetadata
    {
        private readonly IContentTypeRegistryService _contentTypeRegistryService;

        public StableContentTypeOrderer(IContentTypeRegistryService contentTypeRegistryService)
        {
            _contentTypeRegistryService = contentTypeRegistryService ?? throw new ArgumentNullException(nameof(contentTypeRegistryService));
        }

        internal IEnumerable<Lazy<T, M>> Order(IEnumerable<Lazy<T, M>> items)
        {
            return StableTopologicalSort.Order(items, ContentTypeOrderDependencyFunction);
        }

        /// <summary>
        /// An element dependency function used to by topological orderer to detect whether items depend on each other based on
        /// their content type metadata. For example an item with [ContentType("CSharp")] depends on an item with
        /// [ContentType("text")] because "CSharp" inherits "text".
        /// </summary>
        /// <returns>
        /// <c>true</c> if any content type in intemY inherits any content type in itemX, <c>false</c> otherwise.
        /// </returns>
        private bool ContentTypeOrderDependencyFunction(Lazy<T, M> itemX, Lazy<T, M> itemY)
        {
            var current = itemX.Metadata.ContentTypes;
            var candidate = itemY.Metadata.ContentTypes;

            return IsMoreSpecific(candidate, current);
        }

        internal bool IsMoreSpecific(IEnumerable<string> candidate, IEnumerable<string> current)
        {
            foreach (var candidateContentTypeStr in candidate)
            {
                var candidateContentType = _contentTypeRegistryService.GetContentType(candidateContentTypeStr);
                if (candidateContentType != null)
                {
                    foreach (var currentContentTypeStr in current)
                    {
                        // IContentType.IsOfType returns true for the same content type, while we need to know only
                        // if one inherits another.
                        if (!string.Equals(candidateContentTypeStr, currentContentTypeStr, StringComparison.OrdinalIgnoreCase) &&
                            candidateContentType.IsOfType(currentContentTypeStr))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }
}
