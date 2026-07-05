//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
using System.Collections.Generic;

namespace Microsoft.VisualStudio.Utilities
{
    /// <summary>
    /// Represents MEF metadata view corresponding to the <see cref="ContentTypeAttribute"/>s.
    /// </summary>
    public interface IContentTypeMetadata
    {
        /// <summary>
        /// List of declared content types.
        /// </summary>
        IEnumerable<string> ContentTypes { get; }
    }

    /// <summary>
    /// Concrete metadata view for <see cref="IContentTypeMetadata"/>; System.Composition cannot
    /// proxy interface views, so imports use this class (PLAN §5.2 rule 2).
    /// </summary>
    public sealed class ContentTypeMetadata : IContentTypeMetadata
    {
        public ContentTypeMetadata(System.Collections.Generic.IDictionary<string, object> data)
        {
            this.ContentTypes = Microsoft.VisualStudio.Utilities.MetadataValue.GetMany<string>(data, nameof(ContentTypes));
        }

        public System.Collections.Generic.IEnumerable<string> ContentTypes { get; }
    }
}
