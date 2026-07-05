//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.Language.CodeLens.Remoting
{
    /// <summary>
    /// A MEF attribute specifying a template for presenting details of data points from a <see cref="IAsyncCodeLensDataPointProvider"/>.
    /// </summary>
    public sealed class DetailsTemplateNameAttribute : SingletonBaseMetadataAttribute
    {
        /// <summary>
        /// Initializes a new instance of <see cref="DetailsTemplateNameAttribute"/>.
        /// </summary>
        public DetailsTemplateNameAttribute(string detailsTemplateName)
        {
            this.DetailsTemplateName = detailsTemplateName;
        }

        /// <summary>
        /// The name of the template for presenting the data point's details.
        /// </summary>
        public string DetailsTemplateName { get; }
    }
}
