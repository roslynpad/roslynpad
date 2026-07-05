//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
using System.Collections.Generic;

namespace Microsoft.VisualStudio.Utilities
{
    /// <summary>
    /// Describes a content type that is being introduced using <see cref="IContentTypeDefinitionSource"/>.
    /// </summary>
    public interface IContentTypeDefinition
    {
        /// <summary>
        /// The case-insensitive name of the content type.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The case-insensitive names of the base types of the content type. May be of zero length.
        /// </summary>
        IEnumerable<string> BaseDefinitions { get; }
    }
}
