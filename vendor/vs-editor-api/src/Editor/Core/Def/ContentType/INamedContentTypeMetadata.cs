//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Utilities
{
    using System.Collections.Generic;

    /// <summary>
    /// Represents MEF metadata view combining <see cref="IContentTypeMetadata"/> and <see cref="INameAndReplacesMetadata"/> views.
    /// </summary>
    public interface INamedContentTypeMetadata : IContentTypeMetadata, INameAndReplacesMetadata
    {
    }

    /// <summary>
    /// Concrete metadata view for <see cref="INamedContentTypeMetadata"/>; System.Composition cannot
    /// proxy interface views, so imports use this class (PLAN §5.2 rule 2).
    /// </summary>
    public sealed class NamedContentTypeMetadata : INamedContentTypeMetadata
    {
        public NamedContentTypeMetadata(System.Collections.Generic.IDictionary<string, object> data)
        {
            this.ContentTypes = Microsoft.VisualStudio.Utilities.MetadataValue.GetMany<string>(data, nameof(ContentTypes));
            this.Name = Microsoft.VisualStudio.Utilities.MetadataValue.Get<string>(data, nameof(Name));
            this.Replaces = Microsoft.VisualStudio.Utilities.MetadataValue.GetMany<string>(data, nameof(Replaces));
        }

        public System.Collections.Generic.IEnumerable<string> ContentTypes { get; }
        public string Name { get; }
        public System.Collections.Generic.IEnumerable<string> Replaces { get; }
    }
}
