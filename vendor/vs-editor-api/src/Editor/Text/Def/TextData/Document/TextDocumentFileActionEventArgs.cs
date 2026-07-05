//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text
{
    using System;

    /// <summary>
    /// Describes the type of file action.
    /// </summary>
    [Flags]
    public enum FileActionTypes
    {
        /// <summary>
        /// The content was saved to disk.
        /// </summary>
        ContentSavedToDisk = 1,

        /// <summary>
        /// The content was loaded from disk.
        /// </summary>
        ContentLoadedFromDisk = 2,

        /// <summary>
        /// The document was renamed.
        /// </summary>
        DocumentRenamed = 4
    }

    /// <summary>
    /// Provides information for events that are raised when an <see cref="ITextDocument"/> has loaded from or saved to disk.
    /// </summary>
    public class TextDocumentFileActionEventArgs : EventArgs
    {
        #region Private Members

        string _filePath;
        DateTime _time;
        FileActionTypes _fileActionType;

        #endregion

        /// <summary>
        /// Initializes a new instance of a <see cref="TextDocumentFileActionEventArgs"/> for a file action event.
        /// </summary>
        /// <param name="filePath">The path to the file.</param>
        /// <param name="time">The <see cref="DateTime"/> when the file action occurred.</param>
        /// <param name="fileActionType">The <see cref="FileActionTypes"/> that occurred.</param>
        /// <exception cref="ArgumentNullException"><paramref name="filePath"/> is null.</exception>
        public TextDocumentFileActionEventArgs(string filePath, DateTime time, FileActionTypes fileActionType)
        {
            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            _filePath = filePath;
            _time = time;
            _fileActionType = fileActionType;
        }

        #region Public Properties

        /// <summary>
        /// Gets the path to the file.
        /// </summary>
        public string FilePath
        {
            get
            {
                return _filePath;
            }
        }

        /// <summary>
        /// Gets the <see cref="DateTime"/> when the file action occurred.
        /// </summary>
        public DateTime Time
        {
            get
            {
                return _time;
            }
        }

        /// <summary>
        /// Gets the <see cref="FileActionTypes"/> that occurred.
        /// </summary>
        public FileActionTypes FileActionType
        {
            get
            {
                return _fileActionType;
            }
        }

        #endregion
    }
}
