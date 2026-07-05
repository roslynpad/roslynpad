//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
using Microsoft.VisualStudio.Core.Imaging;

namespace Microsoft.VisualStudio.Language.CodeLens.Remoting
{
    /// <summary>
    /// Represents a descriptor for a CodeLens data point.
    /// </summary>
    /// <remarks>
    /// This type is used for the object representing a data point returned from the remote data point provider.
    /// </remarks>
    public sealed class CodeLensDataPointDescriptor
    {
        /// <summary>
        /// The description text that displays in the UI indicator of the data point.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// The tooltip text for the UI indicator of the data point.
        /// </summary>
        public string TooltipText { get; set; }

        /// <summary>
        /// The image content of the data point.
        /// </summary>
        public ImageId? ImageId { get; set; }

        /// <summary>
        /// The integer content of the data point.
        /// </summary>
        public int? IntValue { get; set; }
    }
}
