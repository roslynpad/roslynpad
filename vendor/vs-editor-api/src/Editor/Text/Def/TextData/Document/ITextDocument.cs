//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text
{
    using System;
    using System.Text;
    using Microsoft.VisualStudio.Utilities;

    /// <summary>
    /// Represents a document in the file system that persists an <see cref="ITextBuffer"/>.
    /// </summary>
    public interface ITextDocument : IDisposable
    {
        /// <summary>
        /// The name and path of the file.
        /// </summary>
        string FilePath { get; }

        /// <summary>
        /// Gets the <see cref="ITextBuffer"/> containing the document. This value is always non-null.
        /// </summary>
        ITextBuffer TextBuffer { get; }

        /// <summary>
        /// Determines whether the <see cref="ITextBuffer"/> is dirty.  
        /// </summary>
        /// <remarks>If <c>true</c>, the contents of <see cref="ITextDocument.TextBuffer"/> have
        /// changed since the file was last loaded or saved. If <c>false</c>, the contents of <see cref="ITextDocument.TextBuffer"/> have
        /// not changed since the file was last loaded or saved.</remarks>  
        bool IsDirty { get; }

        /// <summary>
        /// Gets the last <see cref="DateTime"/> the file was saved.  This time exactly matches the last file written 
        /// time on the file system.
        /// </summary>
        DateTime LastSavedTime { get; }

        /// <summary>
        /// Gets the last <see cref="DateTime"/> a change was made to the contents of the document. If it has not been modified
        /// since it was loaded or reloaded from disk, this will be the last write time of the underlying file at the time the
        /// load or reload occurred; otherwise, it is the last time the contents of the text buffer were changed.
        /// </summary>
        DateTime LastContentModifiedTime { get; }

        /// <summary>
        /// Gets or sets the encoding of the document when saved to disk.
        /// </summary>
        Encoding Encoding { get; set; }

        /// <summary>
        /// Change the encoder fallback of <see cref="Encoding"/>.
        /// </summary>
        /// <param name="fallback">The new encoder fallback</param>
        void SetEncoderFallback(EncoderFallback fallback);

        /// <summary>
        /// Occurs when the <see cref="Encoding"/> property changes.
        /// </summary>
        event EventHandler<EncodingChangedEventArgs> EncodingChanged;

        /// <summary>
        /// Occurs when the document has been loaded from or saved to disk.  
        /// You may not call Reload/Save/SaveAs to perform another file action while handling this event.
        /// </summary>
        event EventHandler<TextDocumentFileActionEventArgs> FileActionOccurred;

        /// <summary>
        /// Occurs when the value of <see cref="ITextDocument.IsDirty"/> changes. 
        /// You may not call <see cref="ITextDocument.UpdateDirtyState"/> in order to change 
        /// the <see cref="ITextDocument.IsDirty"/> property while handling this event.
        /// </summary>
        event EventHandler DirtyStateChanged;

        /// <summary>
        /// Rename the document to the given new file path.
        /// </summary>
        /// <param name="newFilePath">The new file path for this document.</param>
        /// <exception cref="ObjectDisposedException">This object has been disposed.</exception>
        /// <exception cref="InvalidOperationException">This object is in the middle of raising events.</exception>
        void Rename(string newFilePath);

        /// <summary>
        /// Reloads the contents of <see cref="FilePath"/> into <see cref="TextBuffer"/>.  
        /// If the load fails, the contents of the <see cref="ITextBuffer"/> remains unchanged.
        /// </summary>
        /// <returns>Indicates whether the reload took place and whether the encoding was sufficient.</returns>
        /// <exception cref="System.IO.IOException">An I/O error occurred during file load.</exception>
        /// <exception cref="System.UnauthorizedAccessException">An access error occurred during file load.</exception>
        /// <exception cref="ObjectDisposedException">This object has been disposed.</exception>
        /// <exception cref="InvalidOperationException">This object is in the middle of raising events.</exception>
        ReloadResult Reload();

        /// <summary>
        /// Reloads the contents of <see cref="FilePath"/> into <see cref="TextBuffer"/>,
        /// using the given <see cref="EditOptions" />.  
        /// If the load fails, the contents of the <see cref="ITextBuffer"/> remains unchanged.
        /// </summary>
        /// <param name="options">The options to use for the text buffer edit.</param>
        /// <returns>Indicates whether the reload took place and whether the encoding was sufficient.</returns>
        /// <exception cref="System.IO.IOException">An I/O error occurred during file load.</exception>
        /// <exception cref="System.UnauthorizedAccessException">An access error occurred during file load.</exception>
        /// <exception cref="ObjectDisposedException">This object has been disposed.</exception>
        /// <exception cref="InvalidOperationException">This object is in the middle of raising events.</exception>
        ReloadResult Reload(EditOptions options);

        /// <summary>
        /// Determines whether the document is currently being reloaded.
        /// </summary>
        bool IsReloading { get; }

        /// <summary>
        /// Saves the contents of the <see cref="ITextDocument.TextBuffer"/> to <see cref="ITextDocument.FilePath"/>.  
        /// If the save operation fails, the value of <see cref="ITextDocument.IsDirty"/> remains unchanged.
        /// </summary>
        /// <exception cref="System.IO.IOException"> An I/O error occurred during file save.</exception>
        /// <exception cref="System.UnauthorizedAccessException">An access error occurred during file save.</exception>
        /// <exception cref="ObjectDisposedException">This object has been disposed.</exception>
        /// <exception cref="InvalidOperationException">This object is in the middle of raising events.</exception>
        void Save();

        /// <summary>
        /// Saves the contents of the <see cref="ITextDocument.TextBuffer"/> to the given <paramref name="filePath"/>.
        /// If the save operation is successful, <see cref="ITextDocument.FilePath"/> is set to <paramref name="filePath"/>, 
        /// and <see cref="ITextDocument.IsDirty"/> is set to <c>false</c>.  If the save operation fails,
        /// <see cref="ITextDocument.FilePath"/> and <see cref="ITextDocument.IsDirty"/> remains unchanged.
        /// </summary>
        /// <param name="filePath">The name of the new file.</param>
        /// <param name="overwrite"><c>true</c> if <paramref name="filePath"/> should be overwritten if it exists, otherwise <c>false</c>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="filePath"/> is null.</exception>
        /// <exception cref="System.IO.IOException">An I/O error occurred (including an error caused by attempting
        /// to overwrite an existing file when <paramref name="overwrite"/> is <c>false</c>).</exception>
        /// <exception cref="System.UnauthorizedAccessException">An access error occurred during file save.</exception>
        /// <exception cref="ObjectDisposedException">This object has been disposed.</exception>
        /// <exception cref="InvalidOperationException">This object is in the middle of raising events.</exception>
        void SaveAs(string filePath, bool overwrite);

        /// <summary>
        /// Saves the contents of the <see cref="ITextDocument.TextBuffer"/> to the given <paramref name="filePath"/>.
        /// If the save operation is successful, <see cref="ITextDocument.FilePath"/> is set to <paramref name="filePath"/>, 
        /// and <see cref="ITextDocument.IsDirty"/> is set to <c>false</c>.  If the save operation fails,
        /// <see cref="ITextDocument.FilePath"/> and <see cref="ITextDocument.IsDirty"/> remains unchanged.
        /// </summary>
        /// <param name="filePath">The name of the new file.</param>
        /// <param name="overwrite"><c>true</c> if <paramref name="filePath"/> should be overwritten if it exists, otherwise <c>false</c>.</param>
        /// <param name="createFolder"><c>true</c> if the folder containing <paramref name="filePath"/> should be created if it does not exist, otherwise <c>false</c>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="filePath"/> is null.</exception>
        /// <exception cref="System.IO.IOException">An I/O error occurred (including an error caused by attempting
        /// to overwrite an existing file when <paramref name="overwrite"/> is <c>false</c>).</exception>
        /// <exception cref="System.UnauthorizedAccessException">An access error occurred during file save.</exception>
        /// <exception cref="ObjectDisposedException">This object has been disposed.</exception>
        /// <exception cref="InvalidOperationException">This object is in the middle of raising events.</exception>
        void SaveAs(string filePath, bool overwrite, bool createFolder);

        /// <summary>
        /// Saves the contents of the <see cref="ITextDocument.TextBuffer"/> to the given <paramref name="filePath"/>.
        /// If the save is successful, <see cref="ITextDocument.FilePath"/> is set to <paramref name="filePath"/>, 
        /// and <see cref="ITextDocument.IsDirty"/> is set to <c>false</c>.  If the save fails,
        /// <see cref="ITextDocument.FilePath"/> and <see cref="ITextDocument.IsDirty"/> remains unchanged.
        /// </summary>
        /// <param name="filePath">The name of the new file.</param>
        /// <param name="overwrite"><c>true</c> if <paramref name="filePath"/> should be overwritten if it exists, otherwise <c>false</c>.</param>
        /// <param name="newContentType">The new <see cref="IContentType"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="filePath"/> or <paramref name="newContentType"/> is null.</exception>
        /// <exception cref="System.IO.IOException">An I/O error occurred (including an error caused by attempting
        /// to overwrite an existing file when <paramref name="overwrite"/> is false).</exception>
        /// <exception cref="System.UnauthorizedAccessException">An access error occurred during file save.</exception>
        /// <exception cref="ObjectDisposedException">This object has been disposed.</exception>
        /// <exception cref="InvalidOperationException">This object is in the middle of raising events.</exception>
        /// <remarks>
        /// The order of events raised as a result of a successful file SaveAs
        /// operation is <see cref="ITextDocument.FileActionOccurred"/> followed by <see cref="ITextBuffer.ContentTypeChanged"/>.
        /// </remarks>
        void SaveAs(string filePath, bool overwrite, IContentType newContentType);

        /// <summary>
        /// Saves the contents of the <see cref="ITextDocument.TextBuffer"/> to the given <paramref name="filePath"/>.
        /// If the save is successful, <see cref="ITextDocument.FilePath"/> is set to <paramref name="filePath"/>, 
        /// and <see cref="ITextDocument.IsDirty"/> is set to <c>false</c>.  If the save fails,
        /// <see cref="ITextDocument.FilePath"/> and <see cref="ITextDocument.IsDirty"/> remains unchanged.
        /// </summary>
        /// <param name="filePath">The name of the new file.</param>
        /// <param name="overwrite"><c>true</c> if <paramref name="filePath"/> should be overwritten if it exists, otherwise <c>false</c>.</param>
        /// <param name="createFolder"><c>true</c> if the folder containing <paramref name="filePath"/> should be created if it does not exist, otherwise <c>false</c>.</param>
        /// <param name="newContentType">The new <see cref="IContentType"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="filePath"/> or <paramref name="newContentType"/> is null.</exception>
        /// <exception cref="System.IO.IOException">An I/O error occurred (including an error caused by attempting
        /// to overwrite an existing file when <paramref name="overwrite"/> is false).</exception>
        /// <exception cref="System.UnauthorizedAccessException">An access error occurred during file save.</exception>
        /// <exception cref="ObjectDisposedException">This object has been disposed.</exception>
        /// <exception cref="InvalidOperationException">This object is in the middle of raising events.</exception>
        /// <remarks>
        /// The order of events raised as a result of a successful file SaveAs
        /// operation is <see cref="ITextDocument.FileActionOccurred"/> followed by <see cref="ITextBuffer.ContentTypeChanged"/>.
        /// </remarks>
        void SaveAs(string filePath, bool overwrite, bool createFolder, IContentType newContentType);

        /// <summary>
        /// Saves the contents of the <see cref="ITextDocument.TextBuffer"/> to the given <paramref name="filePath"/>.
        /// </summary>
        /// <param name="filePath">The name of the file.</param>
        /// <param name="overwrite"><c>true</c> if <paramref name="filePath"/> should be overwritten if it exists, otherwise <c>false</c>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="filePath"/> is null.</exception>
        /// <exception cref="System.IO.IOException">An I/O error occurred (including an error caused by attempting
        /// to overwrite an existing file when <paramref name="overwrite"/> is <c>false</c>).</exception>
        /// <exception cref="System.UnauthorizedAccessException">An access error occurred during file save.</exception>
        /// <exception cref="ObjectDisposedException">This object has been disposed.</exception>
        /// <remarks>This call does not affect the <see cref="IsDirty"/>, <see cref="LastSavedTime"/>, and <see cref="FilePath"/> properties.
        /// The <see cref="FileActionOccurred"/> event is not raised.</remarks>
        void SaveCopy(string filePath, bool overwrite);

        /// <summary>
        /// Saves the contents of the <see cref="ITextDocument.TextBuffer"/> to the given <paramref name="filePath"/>.
        /// </summary>
        /// <param name="filePath">The name of the file.</param>
        /// <param name="overwrite"><c>true</c> if <paramref name="filePath"/> should be overwritten if it exists, otherwise <c>false</c>.</param>
        /// <param name="createFolder"><c>true</c> if the folder containing <paramref name="filePath"/> should be created if it does not exist, otherwise <c>false</c>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="filePath"/> is null.</exception>
        /// <exception cref="System.IO.IOException">An I/O error occurred (including an error caused by attempting
        /// to overwrite an existing file when <paramref name="overwrite"/> is <c>false</c>).</exception>
        /// <exception cref="System.UnauthorizedAccessException">An access error occurred during file save.</exception>
        /// <exception cref="ObjectDisposedException">This object has been disposed.</exception>
        /// <remarks>This call does not affect the <see cref="IsDirty"/>, <see cref="LastSavedTime"/>, and <see cref="FilePath"/> properties.
        /// The <see cref="FileActionOccurred"/> event is not raised.</remarks>
        void SaveCopy(string filePath, bool overwrite, bool createFolder);

        /// <summary>
        /// Updates the <see cref="IsDirty"/> and <see cref="LastContentModifiedTime"/> properties.
        /// </summary>
        /// <param name="isDirty">The new value for <see cref="ITextDocument.IsDirty" />.</param>
        /// <param name="lastContentModifiedTime">The new value for <see cref="ITextDocument.LastContentModifiedTime"/>.</param>
        /// <exception cref="ObjectDisposedException">This object has been disposed.</exception>
        /// <exception cref="InvalidOperationException">This object is in the middle of raising events.</exception>
        void UpdateDirtyState(bool isDirty, DateTime lastContentModifiedTime);
    }
}
