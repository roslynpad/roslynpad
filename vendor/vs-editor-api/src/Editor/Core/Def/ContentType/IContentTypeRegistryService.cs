//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
using System;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.Utilities
{
    /// <summary>
    /// The service that maintains the collection of content types.
    /// </summary>
    /// <remarks>This is a MEF component part, and should be exported with the following attribute:
    /// [Export(typeof(IContentTypeRegistryService))]
    /// </remarks>
    public interface IContentTypeRegistryService
    {
        /// <summary>
        /// Gets the <see cref="IContentType"></see> object with the specified <paramref name="typeName"/>.
        /// </summary>
        /// <param name="typeName">The name of the content type. Name comparisons are case-insensitive.</param>
        /// <returns>The content type, or null if no content type is found.</returns>
        IContentType GetContentType(string typeName);

        /// <summary>
        /// Creates and adds a new content type.
        /// </summary>
        /// <param name="typeName">The name of the content type.</param>
        /// <param name="baseTypeNames">The list of content type names to be used as base content types. Optional.</param>
        /// <returns>The <see cref="IContentType"></see>.</returns>
        /// <exception cref="ArgumentException"><paramref name="typeName"/> is null or empty.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="typeName"/> or one of the <paramref name="baseTypeNames"/> 
        /// is the name of <see cref="UnknownContentType"/>, or the content type already exists, or one of the base types would
        /// introduce a cyclic base type relationship.</exception>
        IContentType AddContentType(string typeName, IEnumerable<string> baseTypeNames);

        /// <summary>
        /// Removes a content type.
        /// </summary>
        /// <remarks>The "unknown" content type cannot be removed. Any content type that is used for file extension 
        /// mapping or as a base for other content types cannot be removed.</remarks>
        /// <param name="typeName">The content type to be removed. </param>
        /// <exception cref="InvalidOperationException">The specified content type cannot be removed.</exception>
        /// <remarks>Has no effect if <paramref name="typeName"/> is not the name of a registered content type.</remarks>
        void RemoveContentType(string typeName);

        /// <summary>
        /// Gets the "unknown" content type.
        /// </summary>
        /// <remarks>The "unknown" content type indicates that the content type cannot be determined.</remarks>
        /// <value>This value is never null.</value>
        IContentType UnknownContentType { get; }

        /// <summary>Gets an enumeration of all content types, including the "unknown" content type.</summary>
        IEnumerable<IContentType> ContentTypes { get; }
    }
}
