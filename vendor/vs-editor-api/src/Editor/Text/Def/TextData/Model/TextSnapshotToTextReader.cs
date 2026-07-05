//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text
{
    using System;
    using System.IO;

    /// <summary>
    /// Provides a <see cref="TextReader"/> facade over a text snapshot.
    /// </summary>
    public sealed class TextSnapshotToTextReader : TextReader
    {
        #region TextReader methods
        /// <summary>
        /// Closes the reader and releases any associated system resources.
        /// </summary>
        public override void Close()
        {
            _currentPosition = -1;
            base.Close();
        }

        /// <summary>
        /// Releases all resources used by the reader.
        /// </summary>
        /// <param name="disposing">Whether to release managed resources.</param>
        protected override void Dispose(bool disposing)
        {
            _currentPosition = -1;
            base.Dispose(disposing);
        }

        /// <summary>
        /// Returns the next character without changing the state of the reader or the
        /// character source.
        /// </summary>
        /// <returns>The next character to be read, or -1 if no more characters are available or the stream does not support seeking.</returns>
        /// <exception cref="ObjectDisposedException">The reader is closed.</exception>
        public override int Peek()
        {
            if (_currentPosition == -1)
                throw new ObjectDisposedException("TextSnapshotToTextReader");

            return (_currentPosition == _snapshot.Length) ? -1 : (int)(_snapshot[_currentPosition]);
        }

        /// <summary>
        /// Reads the next character from the input stream and advances the character
        /// position by one character.
        /// </summary>
        /// <returns>The next character from the input stream, or -1 if no more characters are available.</returns>
        /// <exception cref="ObjectDisposedException">The reader is closed.</exception>
        public override int Read()
        {
            if (_currentPosition == -1)
                throw new ObjectDisposedException("TextSnapshotToTextReader");

            return (_currentPosition == _snapshot.Length) ? -1 : (int)(_snapshot[_currentPosition++]);
        }

        /// <summary>
        /// Reads the specified number of characters from the current stream and writes the
        /// data to the buffer, beginning at the specified location.
        /// </summary>
        /// <param name="buffer">When this method returns, contains the specified character array from the current source.</param>
        /// <param name="index">The place in buffer at which to begin writing.</param>
        /// <param name="count">The maximum number of characters to read.</param>
        /// <returns>The number of characters that have been read. The number will be less than
        /// or equal to <paramref name="count"/>, depending on whether the data is available within the
        /// stream. This method returns zero if called when no more characters are left to read.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> or <paramref name="count"/> is negative, or
        /// the buffer length minus index is less than <paramref name="count"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="buffer"/> is null.</exception>
        /// <exception cref="ObjectDisposedException">The reader is closed.</exception>
        public override int Read(char[] buffer, int index, int count)
        {
            if (_currentPosition == -1)
                throw new ObjectDisposedException("TextSnapshotToTextReader");
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (((index + count) < 0) || ((index + count) > buffer.Length))
                throw new ArgumentOutOfRangeException(nameof(count));

            int charactersToRead = System.Math.Min(_snapshot.Length - _currentPosition, count);
            _snapshot.CopyTo(_currentPosition, buffer, index, charactersToRead);
            _currentPosition += charactersToRead;

            return charactersToRead;
        }

        /// <summary>
        /// Reads a maximum of <paramref name="count"/> characters from the current stream and writes the
        /// data to buffer, beginning at index.
        /// </summary>
        /// <param name="buffer">When this method returns, contains the specified character array from the current source.</param>
        /// <param name="index">The place in buffer at which to begin writing.</param>
        /// <param name="count">The maximum number of characters to read.</param>
        /// <returns>The number of characters that have been read. The number will be less than
        /// or equal to <paramref name="count"/>, depending on whether the data is available within the
        /// stream. This method returns zero if called when no more characters are left to read.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> or <paramref name="count"/> is negative, or
        /// the buffer length minus index is less than <paramref name="count"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="buffer"/> is null.</exception>
        /// <exception cref="ObjectDisposedException">The reader is closed.</exception>
        public override int ReadBlock(char[] buffer, int index, int count)
        {
            return Read(buffer, index, count);
        }

        /// <summary>Reads a line of characters from the current stream and returns the data as a string.</summary>
        /// <returns>The next line from the input stream, or null if all characters have been read.</returns>
        /// <exception cref="ObjectDisposedException">The <see cref="TextReader"/> is closed.</exception>
        public override string ReadLine() 
        {
            if (_currentPosition == -1)
                throw new ObjectDisposedException("TextSnapshotToTextReader");

            if (_currentPosition >= _snapshot.Length)
                return null;

            ITextSnapshotLine line = _snapshot.GetLineFromPosition(_currentPosition);

            //Handle the case where the current position is between a \r\n without crashing (but returning an empty string instead).
            string text = (line.End.Position > _currentPosition)
                          ? _snapshot.GetText(_currentPosition, line.End.Position - _currentPosition)
                          : string.Empty;

            _currentPosition = line.EndIncludingLineBreak.Position;

            return text;
        }

        /// <summary>Reads all the characters from the current position to the end of the reader and returns them as a string.</summary>
        /// <returns>A string containing all the characters from the current position to the end of the reader.</returns>
        /// <exception cref="ObjectDisposedException">The <see cref="TextReader"/> is closed.</exception>
        public override string ReadToEnd()
        {
            if (_currentPosition == -1)
                throw new ObjectDisposedException("TextSnapshotToTextReader");

            string text = _snapshot.GetText(_currentPosition, _snapshot.Length - _currentPosition);
            _currentPosition = _snapshot.Length;

            return text;
        }
        #endregion

        /// <summary>
        /// Initializes a new instance of <see cref="TextSnapshotToTextReader"/> with the specified text snapshot.
        /// </summary>
        /// <param name="textSnapshot">The <see cref="ITextSnapshot"/> to expose as a reader.</param>
        /// <exception cref="ArgumentNullException"><paramref name="textSnapshot"/> is null.</exception>
        public TextSnapshotToTextReader(ITextSnapshot textSnapshot)
        {
            if (textSnapshot == null)
                throw new ArgumentNullException(nameof(textSnapshot));

            _snapshot = textSnapshot;
        }

        ITextSnapshot _snapshot;
        int _currentPosition;
    }
}
