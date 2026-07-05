//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Utilities
{
    using System;
    using Microsoft.VisualStudio.Utilities;

    public interface IContentTypeRegistryService2 : IContentTypeRegistryService
    {
        /// <summary>
        /// Get the mime type associated with a content type.
        /// </summary>
        /// <remarks>Use the <see cref="MimeTypeAttribute"/> attribute on a <see cref="ContentTypeDefinition"/> to associate a mime type with a content type.</remarks>
        string GetMimeType(IContentType type);

        /// <summary>
        /// Get the content type associated with a mime type.
        /// </summary>
        /// <remarks>Use the <see cref="MimeTypeAttribute"/> attribute on a <see cref="ContentTypeDefinition"/> to associate a mime type with a content type.</remarks>
        IContentType GetContentTypeForMimeType(string mimeType);
    }
}
