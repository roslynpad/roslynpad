//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
using System;

namespace Microsoft.VisualStudio.Text.Tagging
{
    /// <summary>
    /// An implementation of <see cref="IUrlTag" />.
    /// </summary>
    public class UrlTag : IUrlTag
    {
        public Uri Url { get; private set; }

        /// <summary>
        /// Create a new tag with the given URL.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="url" /> is <c>null</c></exception>
        public UrlTag(Uri url)
        {
            if (url == null)
                throw new ArgumentNullException(nameof(url));

            Url = url;
        }
    }
}
