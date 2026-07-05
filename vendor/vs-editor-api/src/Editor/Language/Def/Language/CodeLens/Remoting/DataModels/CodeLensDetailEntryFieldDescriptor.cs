//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
using Microsoft.VisualStudio.Core.Imaging;

namespace Microsoft.VisualStudio.Language.CodeLens.Remoting
{
    /// <summary>
    /// Defines a field of a <see cref="CodeLensDetailEntryDescriptor"/>.
    /// </summary>
    public sealed class CodeLensDetailEntryField
    {
        /// <summary>
        /// The text string content of the field.
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// The image content of the field.
        /// </summary>
        public ImageId? ImageId { get; set; }
    }
}
