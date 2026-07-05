//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
using System;

namespace Microsoft.VisualStudio.Utilities
{
    /// <summary>
    /// Provides a display name for an editor component part.
    /// </summary>    
    /// <remarks>
    /// This attribute should be localized wherever it is used.
    /// </remarks>
    [Obsolete("Use " + nameof(LocalizedNameAttribute) + " instead.")]
    public sealed class DisplayNameAttribute : SingletonBaseMetadataAttribute
    {

        /// <summary>
        /// Initializes a new instance of <see cref="DisplayNameAttribute"/>.
        /// </summary>
        /// <param name="displayName">The display name of an editor component part.</param>
        public DisplayNameAttribute(string displayName)
        {
            this.DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
        }

        /// <summary>
        /// Gets the display name of an editor component part.
        /// </summary>
        public string DisplayName { get; }
    }
}
