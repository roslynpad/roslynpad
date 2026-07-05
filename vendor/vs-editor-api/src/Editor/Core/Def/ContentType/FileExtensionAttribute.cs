//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
using System;

namespace Microsoft.VisualStudio.Utilities
{
    /// <summary>
    /// Identifies a file extension.
    /// </summary>
    public sealed class FileExtensionAttribute : SingletonBaseMetadataAttribute
    {

        /// <summary>
        /// Constructs a new instance of the attribute.
        /// </summary>
        /// <param fileExtension="fileExtension">The file extension.</param>
        /// <exception cref="ArgumentNullException"><paramref name="fileExtension"/> is null or empty.</exception>
        public FileExtensionAttribute(string fileExtension)
        {
            if (string.IsNullOrWhiteSpace(fileExtension))
            {
                throw new ArgumentNullException(nameof(fileExtension));
            }
            this.FileExtension = fileExtension;
        }

        /// <summary>
        /// Gets the file extension.
        /// </summary>
        public string FileExtension { get; }
    }
}
