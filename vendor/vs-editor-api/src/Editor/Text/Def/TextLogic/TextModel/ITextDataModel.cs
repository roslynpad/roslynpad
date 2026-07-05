//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text
{
    using System;
    using Microsoft.VisualStudio.Text.Projection;
    using Microsoft.VisualStudio.Utilities;

    /// <summary>
    /// Prepares the <see cref="ITextBuffer"/> for presentation in the editor. Typically the <see cref="ITextDataModel"/>
    /// comprises a single <see cref="ITextBuffer"/> that is exposed as both the <see cref="DocumentBuffer"/> and
    /// the <see cref="DataBuffer"/>. However, in some cases, a graph of <see cref="IProjectionBuffer"/>s is useful to
    /// present as it if were a single document. In that case, the <see cref="DataBuffer"/> will be an <see cref="IProjectionBuffer"/>
    /// that uses the <see cref="DocumentBuffer"/> as a source buffer, directly or indirectly. 
    /// </summary>
    /// <remarks>
    /// The <see cref="ContentType"/> usually is the same as that of the <see cref="DocumentBuffer"/>
    /// </remarks>
    public interface ITextDataModel
    {
        /// <summary>
        /// The <see cref="IContentType"/> of the text data model. Usually this is the same as the <see cref="IContentType"/>
        /// of the <see cref="DocumentBuffer"/> but it need not be.
        /// </summary>
        IContentType ContentType { get; }
        
        /// <summary>
        /// Raised when the <see cref="ContentType"/> of this text data model changes.
        /// </summary>
        event EventHandler<TextDataModelContentTypeChangedEventArgs> ContentTypeChanged;

        /// <summary>
        /// Gets the <see cref="ITextBuffer"/> corresponding to a document in the file system.
        /// </summary>
        ITextBuffer DocumentBuffer { get; }

        /// <summary>
        /// Gets the <see cref="ITextBuffer"/> that should be presented in the editor.
        /// </summary>
        /// <remarks>
        /// This text buffer may be the same as the <see cref="DocumentBuffer"/>, or it may be a projection buffer 
        /// whose ultimate source is the <see cref="DocumentBuffer"/>. The data buffer is the highest buffer that
        /// is shared among different views.
        /// </remarks>
        ITextBuffer DataBuffer { get; }
    }
}
