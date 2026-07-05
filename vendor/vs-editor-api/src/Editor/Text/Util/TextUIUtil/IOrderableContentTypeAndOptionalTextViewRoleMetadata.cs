//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.Text.Utilities
{
    /// <summary>
    /// Metadata which includes Content Types and Text View Roles
    /// </summary>
    public interface IOrderableContentTypeAndOptionalTextViewRoleMetadata : IContentTypeMetadata, IOrderable
    {
        [DefaultValue(null)]
        IEnumerable<string> TextViewRoles { get; }
    }

    /// <summary>
    /// Concrete metadata view for <see cref="IOrderableContentTypeAndOptionalTextViewRoleMetadata"/>; System.Composition cannot
    /// proxy interface views, so imports use this class (PLAN §5.2 rule 2).
    /// </summary>
    public sealed class OrderableContentTypeAndOptionalTextViewRoleMetadata : IOrderableContentTypeAndOptionalTextViewRoleMetadata
    {
        public OrderableContentTypeAndOptionalTextViewRoleMetadata(System.Collections.Generic.IDictionary<string, object> data)
        {
            this.ContentTypes = Microsoft.VisualStudio.Utilities.MetadataValue.GetMany<string>(data, nameof(ContentTypes));
            this.Name = Microsoft.VisualStudio.Utilities.MetadataValue.Get<string>(data, nameof(Name));
            this.Before = Microsoft.VisualStudio.Utilities.MetadataValue.GetMany<string>(data, nameof(Before));
            this.After = Microsoft.VisualStudio.Utilities.MetadataValue.GetMany<string>(data, nameof(After));
            this.TextViewRoles = Microsoft.VisualStudio.Utilities.MetadataValue.GetMany<string>(data, nameof(TextViewRoles));
        }

        public System.Collections.Generic.IEnumerable<string> ContentTypes { get; }
        public string Name { get; }
        public System.Collections.Generic.IEnumerable<string> Before { get; }
        public System.Collections.Generic.IEnumerable<string> After { get; }
        public System.Collections.Generic.IEnumerable<string> TextViewRoles { get; }
    }
}
