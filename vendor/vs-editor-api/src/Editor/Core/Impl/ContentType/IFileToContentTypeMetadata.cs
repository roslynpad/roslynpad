//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
namespace Microsoft.VisualStudio.Utilities.Implementation
{
    using System;
    using System.Collections.Generic;

    public interface IFileToContentTypeMetadata
    {
        [System.ComponentModel.DefaultValue(null)]
        string FileExtension { get; }

        [System.ComponentModel.DefaultValue(null)]
        string FileName { get; }

        IEnumerable<string> ContentTypes { get; }
    }

    /// <summary>
    /// Concrete metadata view for <see cref="IFileToContentTypeMetadata"/>; System.Composition cannot
    /// proxy interface views, so imports use this class (PLAN §5.2 rule 2).
    /// </summary>
    public sealed class FileToContentTypeMetadata : IFileToContentTypeMetadata
    {
        public FileToContentTypeMetadata(System.Collections.Generic.IDictionary<string, object> data)
        {
            this.FileExtension = Microsoft.VisualStudio.Utilities.MetadataValue.Get<string>(data, nameof(FileExtension));
            this.FileName = Microsoft.VisualStudio.Utilities.MetadataValue.Get<string>(data, nameof(FileName));
            this.ContentTypes = Microsoft.VisualStudio.Utilities.MetadataValue.GetMany<string>(data, nameof(ContentTypes));
        }

        public string FileExtension { get; }
        public string FileName { get; }
        public System.Collections.Generic.IEnumerable<string> ContentTypes { get; }
    }
}

