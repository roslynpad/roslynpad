//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
using System;

namespace Microsoft.VisualStudio.Text.Tagging
{
    /// <summary>
    /// Represents the text marker tag, which is used to place text marker adornments on a view.
    /// </summary>
    public class TextMarkerTag : ITextMarkerTag
    {
        /// <summary>
        /// Initializes a new instance of a <see cref="TextMarkerTag"/> of the given type.
        /// </summary>
        /// <param name="type">The type of text marker to use.</param>
        /// <exception cref="ArgumentNullException"><paramref name="type"/> is null.</exception>
        public TextMarkerTag(string type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            
            Type = type;
        }

        /// <summary>
        /// Gets the type of adornment to use.
        /// </summary>
        public string Type { get; private set; }
    }
}
