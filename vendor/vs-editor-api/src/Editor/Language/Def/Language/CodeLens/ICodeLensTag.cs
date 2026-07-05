//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
using System;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.VisualStudio.Language.CodeLens
{
    /// <summary>
    /// An <see cref="ITag"/> indicating the place where CodeLens indicators should be created.
    /// </summary>
    public interface ICodeLensTag : ITag
    {
        /// <summary>
        /// The descriptor for this tag.
        /// </summary>
        ICodeLensDescriptor Descriptor { get; }

        /// <summary>
        /// Raised when this tag has been disconnected and is no longer used as part of the editor. 
        /// </summary>
        event EventHandler Disconnected;
    }
}
