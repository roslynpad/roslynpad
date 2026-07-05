//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Utilities
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// The service that manages associations between file names, extensions, and content types.
    /// </summary>
    /// <remarks>This is a MEF component part, and should be exported with the following attribute:
    /// [Export(typeof(IFileExtensionRegistryService))]
    /// </remarks>
    public interface IFileExtensionRegistryService2 : IFileExtensionRegistryService
    {
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
    }
}
