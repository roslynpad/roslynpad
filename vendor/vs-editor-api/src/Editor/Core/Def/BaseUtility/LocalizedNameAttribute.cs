//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
using System;
using System.Globalization;
using System.Resources;

namespace Microsoft.VisualStudio.Utilities
{
    /// <summary>
    /// Represents an attribute which can provide a localized name as metadata for a MEF extension.
    /// </summary>
    public sealed class LocalizedNameAttribute : SingletonBaseMetadataAttribute
    {
        /// <summary>
        /// Note: the localized name is cached rather than the type to prevent
        /// MEF from referencing the type in its cache.  Types exposed as metadata
        /// cause MEF to load the assembly containing the type during composition.
        /// </summary>
        private readonly string localizedName;

        /// <summary>
        /// Creates an instance of this attribute, which caches the localized name represented
        /// by the given type and resource name.
        /// </summary>
        /// <param name="type">The type from which to load the localized resource.  This should
        /// be a type created by the resource designer.</param>
        /// <param name="resourceId">The name of the localized resource string contained the
        /// resource type.</param>
        public LocalizedNameAttribute(Type type, string resourceId)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }
            if (resourceId == null)
            {
                throw new ArgumentNullException(nameof(resourceId));
            }

            ResourceManager resourceManager = new ResourceManager(type);
            this.localizedName = resourceManager.GetString(resourceId, CultureInfo.CurrentUICulture);
        }

        /// <summary>
        /// Creates an instance of this attribute, which caches the localized name represented
        /// by the given type and resource name.
        /// </summary>
        /// <param name="type">The type from which to load the localized resource.</param>
        /// <param name="resourceStreamName">The base name of the resource stream containing the resource.</param>
        /// <param name="resourceId">The name of the localized resource string contained the
        /// resource type.</param>
        public LocalizedNameAttribute(Type type, string resourceStreamName, string resourceId)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }
            if (resourceStreamName == null)
            {
                throw new ArgumentNullException(nameof(resourceStreamName));
            }
            if (resourceId == null)
            {
                throw new ArgumentNullException(nameof(resourceId));
            }

            ResourceManager resourceManager = new ResourceManager(resourceStreamName, type.Assembly);
            this.localizedName = resourceManager.GetString(resourceId, CultureInfo.CurrentUICulture);
        }

        /// <summary>
        /// Gets the localized name specified by the constructor.
        /// </summary>
        public string LocalizedName => this.localizedName;
    }
}
