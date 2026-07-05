//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Utilities
{
    using System.Collections.Generic;

    /// <summary>
    /// The content type of an object.
    /// </summary>
    /// <remarks>All content types are identified by a unique name. 
    /// The <see cref="IContentTypeRegistryService"></see> can return an <see cref="IContentType"></see> object to allow clients to access additional information.</remarks>
    public interface IContentType
    {
        /// <summary>
        /// The name of the content type.
        /// </summary>
        /// <value>This name must be unique, and must not be null.</value>
        /// <remarks>Comparisons performed on this name are case-insensitive.</remarks>
        string TypeName { get; }

        /// <summary>
        /// The display name of the content type.
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// Returns <c>true</c> if this <see cref="IContentType"></see>
        /// derives from the content type specified by <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The name of the base content type.</param>
        /// <returns><c>true</c> if this content type derives from the one specified by <paramref name="type"/>otherwise <c>false</c>.</returns>
        bool IsOfType(string type);

        /// <summary>
        /// The set of all content types from which the current <see cref="IContentType"></see> is derived.
        /// </summary>
        /// <value>This value is never null, though it may be the empty set.</value>
        IEnumerable<IContentType> BaseTypes { get; }
    }
}
