//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Classification
{
    using System;
    using Microsoft.VisualStudio.Utilities;

    /// <summary>
    /// Used to declare the name for a logical classification
    /// type and the name of a classification type from which it is derived.
    /// </summary>
    /// <remarks>
    /// <para>This attribute is used to provide metadata for the <see cref="ClassificationTypeDefinition" /> MEF export.
    /// The <see cref="IClassificationTypeRegistryService" /> service uses this to construct <see cref="IClassificationType"></see> objects.
    /// </para>
    /// <para>
    /// This attribute can be stacked, so that a <see cref="IClassificationType"/> can multiply inherit from different base types.
    /// </para>
    /// </remarks>
    /// <seealso cref="IClassificationType"/>
    /// <seealso cref="IClassificationTypeRegistryService"/>
    /// <seealso cref="ClassificationTypeDefinition"/>
    public sealed class ClassificationTypeAttribute : MultipleBaseMetadataAttribute
    {
        private string _name;

        /// <summary>
        /// Gets or sets the name of this classification type.
        /// </summary>
        /// <remarks>
        /// The name must be unique across all classification types.  It cannot be null or
        /// an empty string. Classification type names are case insensitive.
        /// </remarks>
        /// <exception cref="ArgumentNullException">The value is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The value is an empty string.</exception>
        public string ClassificationTypeNames
        {
            get
            {
                return _name;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                _name = value;
            }
        }
    }
}
