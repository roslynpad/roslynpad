//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.Text.Utilities
{
    /// <summary>
    /// Metadata which includes Ordering and Content Types
    /// </summary>
    public interface IOrderableContentTypeMetadata : IContentTypeMetadata, IOrderable
    {
    }

    /// <summary>
    /// Concrete metadata view for <see cref="IOrderableContentTypeMetadata"/>; System.Composition cannot
    /// proxy interface views, so imports use this class (PLAN §5.2 rule 2).
    /// </summary>
    public sealed class OrderableContentTypeMetadata : IOrderableContentTypeMetadata
    {
        public OrderableContentTypeMetadata(System.Collections.Generic.IDictionary<string, object> data)
        {
            this.ContentTypes = Microsoft.VisualStudio.Utilities.MetadataValue.GetMany<string>(data, nameof(ContentTypes));
            this.Name = Microsoft.VisualStudio.Utilities.MetadataValue.Get<string>(data, nameof(Name));
            this.Before = Microsoft.VisualStudio.Utilities.MetadataValue.GetMany<string>(data, nameof(Before));
            this.After = Microsoft.VisualStudio.Utilities.MetadataValue.GetMany<string>(data, nameof(After));
        }

        public System.Collections.Generic.IEnumerable<string> ContentTypes { get; }
        public string Name { get; }
        public System.Collections.Generic.IEnumerable<string> Before { get; }
        public System.Collections.Generic.IEnumerable<string> After { get; }
    }
}
