//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
using System.Collections.Generic;

namespace Microsoft.VisualStudio.Language.CodeLens.Remoting
{
    /// <summary>
    /// Represents a data model describing <see cref="IAsyncCodeLensDataPointProvider"/>s.
    /// </summary>
    /// <remarks>
    /// When requested, the remote CodeLens service returns an object of <see cref="CodeLensDataPointProviderDescriptor"/> for each provider it hosts.
    /// </remarks>
    public sealed class CodeLensDataPointProviderDescriptor
    {
        /// <summary>
        /// The uniquely-identifying name of the data point provider.
        /// </summary>
        public string ProviderUniqueId { get; set; }

        /// <summary>
        /// The localized name of the data point provider.
        /// </summary>
        public string LocalizedName { get; set; }

        /// <summary>
        /// List of supported content types.
        /// </summary>
        public IEnumerable<string> ContentTypes { get; set; }

        /// <summary>
        /// An <see cref="int" /> value indicating the order of the indicator.
        /// Lower value indicators will come first in the default ordering in indicator adornments in editor.
        /// </summary>
        public int Priority { get; set; } = int.MaxValue;

        /// <summary>
        /// Determines if the provider is visible in the tool's option setting.
        /// </summary>
        public bool OptionUserVisible { get; set; } = true;

        /// <summary>
        /// Determines if the provider can be modified in the tool's option setting.
        /// </summary>
        public bool OptionUserModifiable { get; set; } = true;

        /// <summary>
        /// What template to use for presenting the detail in the detail popup.
        /// Defaults to use a GridView to present detail data.
        /// </summary>
        public string DetailsTemplateName { get; set; }
    }
}
