//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//

using System;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.Utilities
{
    /// <summary>
    /// Service for mapping files to the appropriate <see cref="IContentType"/> for that file.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Note that this interface duplicates the methods from <see cref="IFileExtensionRegistryService"/> and
    /// <see cref="IFileExtensionRegistryService2"/>. The eventual goal is to deprecate the other interfaces
    /// and only use <see cref="IFileToContentTypeService"/>.
    /// </para></remarks>
    public interface IFileToContentTypeService
    {
        /// <summary>
        /// Get the default <see cref="IContentType"/> for a file located at <paramref name="filePath"/>.
        /// </summary>
        /// <param name="filePath">Name of the file in question.</param>
        /// <returns>Excpected content type or
        /// <see cref="IContentTypeRegistryService.UnknownContentType"/> if no content type is found.</returns>
        /// <remarks>If no <see cref="IContentType"/> is found using declared <see cref="IFilePathToContentTypeProvider"/>
        /// assets, then the <see cref="GetContentTypeForFileNameOrExtension(string)"/> is used.</remarks>
        IContentType GetContentTypeForFilePath(string filePath);

        /// <summary>
        /// Get the default <see cref="IContentType"/> for a file located at <paramref name="filePath"/>.
        /// </summary>
        /// <param name="filePath">Name of the file in question.</param>
        /// <returns>Excpected content type or
        /// <see cref="IContentTypeRegistryService.UnknownContentType"/> if no content type is found.</returns>
        /// <remarks>If no <see cref="IContentType"/> is found using declared <see cref="IFilePathToContentTypeProvider"/>
        /// assets, then <see cref="IContentTypeRegistryService.UnknownContentType"/> is returned.</remarks>
        IContentType GetContentTypeForFilePathOnly(string filePath);

        /// <summary>
        /// Gets the content type associated with the given file name.
        /// </summary>
        /// <param name="name">The file name. It cannot be null.</param>
        /// <returns>The <see cref="IContentType"></see> associated with this name. If no association exists, it returns the "unknown" content type. It never returns null.</returns>
        IContentType GetContentTypeForFileName(string name);

        /// <summary>
        /// Gets the content type associated with the given file name or its extension.
        /// </summary>
        /// <param name="name">The file name. It cannot be null.</param>
        /// <returns>The <see cref="IContentType"></see> associated with this name. If no association exists, it returns the "unknown" content type. It never returns null.</returns>
        IContentType GetContentTypeForFileNameOrExtension(string name);

        /// <summary>
        /// Gets the list of file names associated with the specified content type.
        /// </summary>
        /// <param name="contentType">The content type. It cannot be null.</param>
        /// <returns>The list of file names associated with the content type.</returns>
        IEnumerable<string> GetFileNamesForContentType(IContentType contentType);

        /// <summary>
        /// Adds a new file name to the registry.
        /// </summary>
        /// <param name="name">The file name (the period is optional).</param>
        /// <param name="contentType">The content type for the file name.</param>
        /// <exception cref="InvalidOperationException"><see paramref="name"/> is already present in the registry.</exception>
        void AddFileName(string name, IContentType contentType);

        /// <summary>
        /// Removes the specified file name from the registry.
        /// </summary>
        /// <remarks>If the specified name does not exist, then the method does nothing.</remarks>
        /// <param name="name">The file name (the period is optional).</param>
        void RemoveFileName(string name);

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
