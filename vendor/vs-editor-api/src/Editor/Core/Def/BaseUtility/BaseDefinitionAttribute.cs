//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
using System;

namespace Microsoft.VisualStudio.Utilities
{
    /// <summary>
    /// Represents a base definition of the current definition.
    /// </summary>
    public sealed class BaseDefinitionAttribute : MultipleBaseMetadataAttribute
    {
        private string baseDefinition;

        /// <summary>
        /// Initializes a new instance of <see cref="BaseDefinitionAttribute"/>.
        /// </summary>
        /// <param name="name">The base definition name. Definition names are case-insensitive.</param>
        /// <exception cref="ArgumentNullException"><paramref name="name"/>is null or an empty string.</exception>
        public BaseDefinitionAttribute(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }
            baseDefinition = name;
        }

        /// <summary>
        /// Gets the base definition name.
        /// </summary>
        public string BaseDefinition
        {
            get
            {
                return baseDefinition;
            }
        }
    }
}
