//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
using System;

namespace Microsoft.VisualStudio.Utilities
{
    /// <summary>
    /// Identifies a file name.
    /// </summary>
    public sealed class FileNameAttribute : SingletonBaseMetadataAttribute
    {
        /// <summary>
        /// Constructs a new instance of the attribute.
        /// </summary>
        /// <param name="fileName">The file extension.</param>
        /// <exception cref="ArgumentNullException"><paramref name="fileName"/> is null or empty.</exception>
        public FileNameAttribute(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentNullException(nameof(fileName));
            }

            this.FileName = fileName;
        }

        /// <summary>
        /// Gets the file name.
        /// </summary>
        public string FileName
        {
            get;
        }
    }
}
