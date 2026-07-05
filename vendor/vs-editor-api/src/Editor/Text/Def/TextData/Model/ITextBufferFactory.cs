//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text
{
    using System;
    using System.IO;

    using Microsoft.VisualStudio.Utilities;

    /// <summary>
    /// The factory service for ordinary TextBuffers.
    /// </summary>
    /// <remarks>This is a MEF Component, and should be imported as follows:
    /// [Import]
    /// ITextBufferFactoryService factory = null;
    /// </remarks>
    public interface ITextBufferFactoryService
    {
        /// <summary>
        /// Predefined default content type. This is the base type for most content types.
        /// </summary>
        IContentType TextContentType { get; }

        /// <summary>
        /// Predefined content type for plain text files.
        /// </summary>
        IContentType PlaintextContentType { get; }

        /// <summary>
        /// A content type for which no associated artifacts are automatically created.
        /// </summary>
        IContentType InertContentType { get; }

        /// <summary>
        /// Creates an empty <see cref="ITextBuffer"/> with <see cref="IContentType"/> "text".
        /// </summary>
        /// <returns>
        /// An empty <see cref="ITextBuffer"/> object.
        /// </returns>
        ITextBuffer CreateTextBuffer();

        /// <summary>
        /// Creates an empty <see cref="ITextBuffer"/> with the specified <see cref="IContentType"/>.
        /// </summary>
        /// <param name="contentType">The <see cref="IContentType"/> for the new <see cref="ITextBuffer"/>.</param>
        /// <returns>
        /// An empty <see cref="ITextBuffer"/> with the given ContentType.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="contentType"/> is null.</exception>
        ITextBuffer CreateTextBuffer(IContentType contentType);
        
        /// <summary>
        /// Creates an <see cref="ITextBuffer"/> with the specified <see cref="IContentType"/> and populates it 
        /// with the given text.
        /// </summary>
        /// <param name="text">The initial text to add.</param>
        /// <param name="contentType">The <see cref="IContentType"/> for the new <see cref="ITextBuffer"/>.</param>
        /// <returns>
        /// A <see cref="ITextBuffer"/> object with the given text and <see cref="IContentType"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">Either <paramref name="text"/> or <paramref name="contentType"/> is null.</exception>
        ITextBuffer CreateTextBuffer(string text, IContentType contentType);
        
        /// <summary>
        /// Creates an <see cref="ITextBuffer"/> with the given <paramref name="contentType"/> and populates it by 
        /// reading data from the specified TextReader.
        /// </summary>
        /// <param name="reader">The TextReader from which to read.</param>
        /// <param name="contentType">The <paramref name="contentType"/> for the text contained in the new <see cref="ITextBuffer"/></param>
        /// <returns>
        /// An <see cref="ITextBuffer"/> object with the given TextReader and <paramref name="contentType"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="reader"/> is null.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="contentType"/> is null.</exception>
        /// <remarks>The <paramref name="reader"/> is not closed by this operation.</remarks>
        ITextBuffer CreateTextBuffer(TextReader reader, IContentType contentType);

        /// <summary>
        /// Raised when any <see cref="ITextBuffer"/> is created.
        /// </summary>
        event EventHandler<TextBufferCreatedEventArgs> TextBufferCreated;
    }
}
