//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
namespace Microsoft.VisualStudio.Text.Tagging
{
    using System.Collections.Generic;
    using System.ComponentModel;

    /// <summary>
    /// The metadata interface for exporters and importers of metadata on <see cref="IViewTaggerProvider"/> factories.
    /// </summary>
    public interface IViewTaggerMetadata : INamedTaggerMetadata
    {
        /// <summary>
        /// Text view roles to which the tagger provider applies. Default value of null is provided for backward
        /// compatibility.
        /// </summary>
        [DefaultValue(null)]
        IEnumerable<string> TextViewRoles { get; }
    }

    /// <summary>
    /// Concrete metadata view for <see cref="IViewTaggerMetadata"/>; System.Composition cannot
    /// proxy interface views, so imports use this class (PLAN §5.2 rule 2).
    /// </summary>
    public sealed class ViewTaggerMetadata : IViewTaggerMetadata
    {
        public ViewTaggerMetadata(System.Collections.Generic.IDictionary<string, object> data)
        {
            this.ContentTypes = Microsoft.VisualStudio.Utilities.MetadataValue.GetMany<string>(data, nameof(ContentTypes));
            this.TagTypes = Microsoft.VisualStudio.Utilities.MetadataValue.GetMany<System.Type>(data, nameof(TagTypes));
            this.Name = Microsoft.VisualStudio.Utilities.MetadataValue.Get<string>(data, nameof(Name));
            this.Replaces = Microsoft.VisualStudio.Utilities.MetadataValue.GetMany<string>(data, nameof(Replaces));
            this.TextViewRoles = Microsoft.VisualStudio.Utilities.MetadataValue.GetMany<string>(data, nameof(TextViewRoles));
        }

        public System.Collections.Generic.IEnumerable<string> ContentTypes { get; }
        public System.Collections.Generic.IEnumerable<System.Type> TagTypes { get; }
        public string Name { get; }
        public System.Collections.Generic.IEnumerable<string> Replaces { get; }
        public System.Collections.Generic.IEnumerable<string> TextViewRoles { get; }
    }
}
