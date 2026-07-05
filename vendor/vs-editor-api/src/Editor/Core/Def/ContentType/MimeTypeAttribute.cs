//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Utilities
{
    using System;
    using System.Composition;

    /// <summary>
    /// Declares an association between an extension part and a particular content type.
    /// </summary>
    [MetadataAttribute]
    [System.ComponentModel.Composition.MetadataAttribute] // for MEF v1 parts composed via VS-MEF
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Field, AllowMultiple = false)]
    public sealed class MimeTypeAttribute : SingletonBaseMetadataAttribute
    {

        /// <summary>
        /// Initializes a new instance of <see cref="MimeTypeAttribute"/>.
        /// </summary>
        /// <param name="name">The Mime type to be associated with the content type.</param>
        /// <exception cref="ArgumentNullException"><paramref name="name"/>is null or an empty string.</exception>
        public MimeTypeAttribute(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            this.MimeType = name;
        }

        /// <summary>
        /// The MimeType for the content type definition
        /// </summary>
        public string MimeType
        {
            get;
        }
    }
}
