//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text
{
    using System;
    using System.Text;
    using Microsoft.VisualStudio.Utilities;

    /// <remarks>
    /// Represents a service that creates, load, and disposes text documents. This is a MEF component part, and should be imported as follows:
    /// [Import]
    /// ITextDocumentFactoryService factory = null;
    /// </remarks>
    public interface ITextDocumentFactoryService
    {
        /// <summary>
        /// Creates an <see cref="ITextDocument"/> that opens and loads the contents of <paramref name="filePath"/> into a new <see cref="ITextBuffer"/>.
        /// </summary>
        /// <param name="filePath">The full path to the file to be loaded.</param>
        /// <param name="contentType">The <see cref="IContentType"/> for the <see cref="ITextBuffer"/>.</param>
        /// <returns>An <see cref="ITextDocument"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="filePath"/> or <paramref name="contentType"/> is null.</exception>
        /// <remarks>This method is equivalent to CreateAndLoadTextDocument(filePath, contentType, true, out unusedBoolean).</remarks>
        ITextDocument CreateAndLoadTextDocument(string filePath, IContentType contentType);

        /// <summary>
        /// Creates an <see cref="ITextDocument"/> that opens and loads the contents of <paramref name="filePath"/> into a new <see cref="ITextBuffer"/>.
        /// </summary>
        /// <param name="filePath">The full path to the file to be loaded.</param>
        /// <param name="contentType">The <see cref="IContentType"/> for the <see cref="ITextBuffer"/>.</param>
        /// <param name="encoding">The encoding to use. The decoder part of the Encoding object won't be used.</param>
        /// <param name="characterSubstitutionsOccurred">Set to true if some of the file bytes could not be directly translated using the given encoding.</param>
        /// <returns>An <see cref="ITextDocument"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="filePath"/>, <paramref name="contentType"/>, or <paramref name="encoding"/> is null.</exception>
        ITextDocument CreateAndLoadTextDocument(string filePath, IContentType contentType, Encoding encoding, out bool characterSubstitutionsOccurred);

        /// <summary>
        /// Creates an <see cref="ITextDocument"/> that opens and loads the contents of <paramref name="filePath"/> into a new <see cref="ITextBuffer"/>.
        /// </summary>
        /// <param name="filePath">The full path to the file to be loaded.</param>
        /// <param name="contentType">The <see cref="IContentType"/> for the <see cref="ITextBuffer"/>.</param>
        /// <param name="attemptUtf8Detection">Whether to attempt to load the document as a UTF-8 file.</param>
        /// <param name="characterSubstitutionsOccurred">Set to true if some of the file bytes could not be directly translated using the given encoding.</param>
        /// <returns>An <see cref="ITextDocument"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="filePath"/> or <paramref name="contentType"/> is null.</exception>
        ITextDocument CreateAndLoadTextDocument(string filePath, IContentType contentType, bool attemptUtf8Detection, out bool characterSubstitutionsOccurred);

        /// <summary>
        /// Creates an <see cref="ITextDocument"/> with <paramref name="textBuffer"/>, which is to be saved to <paramref name="filePath"/>
        /// </summary>
        /// <param name="textBuffer">The <see cref="ITextBuffer"/> to be saved to <paramref name="filePath"/>.</param>
        /// <param name="filePath">The full path to the file.</param>
        /// <returns>An <see cref="ITextDocument"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="textBuffer"/> or <paramref name="filePath"/> is null.</exception>
        /// <remarks>This call does not save the contents of the buffer to the given path.</remarks>
        ITextDocument CreateTextDocument(ITextBuffer textBuffer, string filePath);

        /// <summary>
        /// Retrieve an <see cref="ITextDocument"/> for the given buffer, if one exists.
        /// </summary>
        /// <param name="textBuffer">The <see cref="ITextBuffer"/> to get a document for.</param>
        /// <param name="textDocument">The <see cref="ITextDocument"/> for this buffer, if one exists.</param>
        /// <returns><c>true</c> if a document exists for this buffer, <c>false</c> otherwise.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="textBuffer"/> is null.</exception>
        bool TryGetTextDocument(ITextBuffer textBuffer, out ITextDocument textDocument);

        /// <summary>
        /// Occurs when an <see cref="ITextDocument"/> is created.
        /// </summary>
        event EventHandler<TextDocumentEventArgs> TextDocumentCreated;

        /// <summary>
        /// Occurs when an <see cref="ITextDocument"/> is disposed.
        /// </summary>
        event EventHandler<TextDocumentEventArgs> TextDocumentDisposed;
    }
}
