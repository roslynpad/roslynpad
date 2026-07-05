//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
using System;

namespace Microsoft.VisualStudio.Utilities
{
    /// <summary>
    /// Associates a name with an editor extension part.
    /// </summary>
    public sealed class NameAttribute : SingletonBaseMetadataAttribute
    {

        /// <summary>
        /// Constructs a new instance of the attribute.
        /// </summary>
        /// <param name="name">The name of the editor extension part.</param>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="name"/> is an empty string.</exception>
        public NameAttribute(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }
            if (name.Length == 0)
            {
                throw new ArgumentException("name must not be empty", nameof(name));
            }
            this.Name = name;
        }

        /// <summary>
        /// The name of the editor extension part.
        /// </summary>
        public string Name { get; }
    }
}
