//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
using System;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.Text.Editor
{
    /// <summary>
    /// Specifies the name(s) of an asset that will be replaced by this asset.
    /// </summary>
    /// <remarks>
    /// <para>You can specify multiple Replaces attributes if you want to replace multiple assets.</para>
    /// <para>An asset must have a different Name attribute than its Replaces attribute (otherwise it would "replace" itself, preventing it from being created).</para>
    /// <para>An asset is not created if its Name attribute matches the Replaces attribute of any other asset that would -- excluding this check -- be created. For
    /// margin providers, the means that a provider must match the view's ContentType and TextViewRole before it can replace another provider.</para>
    /// </remarks>
    public sealed class ReplacesAttribute : MultipleBaseMetadataAttribute
    {
        private readonly string replaces;

        /// <summary>
        /// The name of the asset replaced by this asset.
        /// </summary>
        /// <param name="replaces">The name of the replaced asset.</param>
        /// <exception cref="ArgumentNullException"><paramref name="replaces"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="replaces"/> is an empty string.</exception>
        public ReplacesAttribute(string replaces)
        {
            if (replaces == null)
                throw new ArgumentNullException(nameof(replaces));
            if (replaces.Length == 0)
                throw new ArgumentException("replaces is an empty string.");

            this.replaces = replaces;
        }

        /// <summary>
        /// The name of the replaced margin.
        /// </summary>
        public string Replaces
        {
            get 
            {
                return this.replaces; 
            }
        }
    }
}
