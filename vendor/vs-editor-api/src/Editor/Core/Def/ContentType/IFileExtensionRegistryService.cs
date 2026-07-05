//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
using System;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.Utilities
{
    /// <summary>
    /// The service that manages associations between file extensions and content types.
    /// </summary>
    /// <remarks>This is a MEF component part, and should be exported with the following attribute:
    /// [Export(typeof(IFileExtensionRegistryService))]
    /// </remarks>
    public interface IFileExtensionRegistryService
    {
        /// <summary>
        /// Gets the content type associated with the given file extension.
        /// </summary>
        /// <param name="extension">The file extension.  It cannot be null, and it should not contain a period.</param>
        /// <returns>The <see cref="IContentType"></see> associated with this extension. If no association exists, it returns the "unknown" content type. It never returns null.</returns>
        IContentType GetContentTypeForExtension(string extension);

        /// <summary>
        /// Gets the list of file extensions associated with the specified content type.
        /// </summary>
        /// <param name="contentType">The content type. It cannot be null.</param>
        /// <returns>The list of file extensions associated with the content type.</returns>
        IEnumerable<string> GetExtensionsForContentType(IContentType contentType);

        /// <summary>
        /// Adds a new file extension to the registry.
        /// </summary>
        /// <param name="extension">The file extension (the period is optional).</param>
        /// <param name="contentType">The content type for the file extension.</param>
        /// <exception cref="InvalidOperationException"><see paramref="extension"/> is already present in the registry.</exception>
        void AddFileExtension(string extension, IContentType contentType);

        /// <summary>
        /// Removes the specified file extension from the registry.
        /// </summary>
        /// <remarks>If the specified extension does not exist, then the method does nothing.</remarks>
        /// <param name="extension">The file extension (the period is optional).</param>
        void RemoveFileExtension(string extension); 
    }
}
