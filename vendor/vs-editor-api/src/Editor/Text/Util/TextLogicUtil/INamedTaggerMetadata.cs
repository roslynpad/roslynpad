//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Tagging
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Microsoft.VisualStudio.Text.Utilities;
    using Microsoft.VisualStudio.Utilities;

    /// <summary>
    /// The metadata interface for exporters and importers of metadata on <see cref="ITaggerProvider"/> factories.
    /// </summary>
    public interface INamedTaggerMetadata : ITaggerMetadata, INamedContentTypeMetadata
    {
    }

    /// <summary>
    /// Concrete metadata view for <see cref="INamedTaggerMetadata"/>; System.Composition cannot
    /// proxy interface views, so imports use this class (PLAN §5.2 rule 2).
    /// </summary>
    public sealed class NamedTaggerMetadata : INamedTaggerMetadata
    {
        public NamedTaggerMetadata(System.Collections.Generic.IDictionary<string, object> data)
        {
            this.ContentTypes = Microsoft.VisualStudio.Utilities.MetadataValue.GetMany<string>(data, nameof(ContentTypes));
            this.TagTypes = Microsoft.VisualStudio.Utilities.MetadataValue.GetMany<System.Type>(data, nameof(TagTypes));
            this.Name = Microsoft.VisualStudio.Utilities.MetadataValue.Get<string>(data, nameof(Name));
            this.Replaces = Microsoft.VisualStudio.Utilities.MetadataValue.GetMany<string>(data, nameof(Replaces));
        }

        public System.Collections.Generic.IEnumerable<string> ContentTypes { get; }
        public System.Collections.Generic.IEnumerable<System.Type> TagTypes { get; }
        public string Name { get; }
        public System.Collections.Generic.IEnumerable<string> Replaces { get; }
    }
}
