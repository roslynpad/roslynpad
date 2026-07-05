//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Utilities
{
    using Microsoft.VisualStudio.Utilities;

    /// <summary>
    /// MEF metadata definition for <see cref="IEncodingDetector"/>.
    /// </summary>
    public interface IEncodingDetectorMetadata : IOrderable, IContentTypeMetadata
    {
    }

    /// <summary>
    /// Concrete metadata view for <see cref="IEncodingDetectorMetadata"/>; System.Composition cannot
    /// proxy interface views, so imports use this class (PLAN §5.2 rule 2).
    /// </summary>
    public sealed class EncodingDetectorMetadata : IEncodingDetectorMetadata
    {
        public EncodingDetectorMetadata(System.Collections.Generic.IDictionary<string, object> data)
        {
            this.Name = Microsoft.VisualStudio.Utilities.MetadataValue.Get<string>(data, nameof(Name));
            this.Before = Microsoft.VisualStudio.Utilities.MetadataValue.GetMany<string>(data, nameof(Before));
            this.After = Microsoft.VisualStudio.Utilities.MetadataValue.GetMany<string>(data, nameof(After));
            this.ContentTypes = Microsoft.VisualStudio.Utilities.MetadataValue.GetMany<string>(data, nameof(ContentTypes));
        }

        public string Name { get; }
        public System.Collections.Generic.IEnumerable<string> Before { get; }
        public System.Collections.Generic.IEnumerable<string> After { get; }
        public System.Collections.Generic.IEnumerable<string> ContentTypes { get; }
    }
}
