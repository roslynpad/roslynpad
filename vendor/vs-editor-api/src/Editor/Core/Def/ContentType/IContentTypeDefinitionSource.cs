//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
using System.Collections.Generic;

namespace Microsoft.VisualStudio.Utilities
{
    /// <summary>
    /// Defines an alternate source for content type definitions that should be processed together
    /// with content types introduced statically using <see cref="ContentTypeDefinition"/>. This is intended
    /// primarily for legacy VS content types.
    /// This is a MEF contract type. There is no associated metadata.
    /// </summary>
    public interface IContentTypeDefinitionSource
    {
        /// <summary>
        /// Content type definitions.
        /// </summary>
        IEnumerable<IContentTypeDefinition> Definitions { get; }
    }
}