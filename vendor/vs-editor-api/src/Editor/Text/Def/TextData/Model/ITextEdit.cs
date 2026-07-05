//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text
{
    using System;

    /// <summary>
    /// Represents a set of editing operations on an <see cref="ITextBuffer"/>. The positions of all edit operations are specified
    /// with respect to the state of the <see cref="ITextBuffer"/> at the time this object was created.
    /// </summary>
    /// <remarks>
    /// <para>At most one <see cref="ITextBufferEdit"/> object may be active at a given time for a particular <see cref="ITextBuffer"/>. 
    /// This object is considered
    /// active as long as it has been neither Applied nor Cancelled; calling Dispose on an unapplied object is equivalent to calling Cancel. </para>
    /// <para>The operations performed using this object are not reflected in the <see cref="ITextBuffer"/> until the <see cref="ITextBufferEdit.Apply"/> 
    /// method has been called.</para>
    /// </remarks>
    public interface ITextEdit : ITextBufferEdit
    {
        /// <summary>
        /// Inserts the given <paramref name="text"/> at the specified <paramref name="position"/>in the text buffer.
        /// </summary>
        /// <param name="position">The buffer position at which the first character of the text will appear.</param>
        /// <param name="text">The text to be inserted.</param>
        /// <returns><c>true</c> if the insertion succeeded, <c>false</c> if it failed due to a read-only region.</returns>
        /// <remarks>Inserting an empty string will succeed but will not generate a new snapshot or raise a
        /// <see cref="ITextBuffer.Changed"/> event.</remarks>
        /// <exception cref="InvalidOperationException">The <see cref="ITextBufferEdit.Apply"/> or <see cref="ITextBufferEdit.Cancel"/> or <see cref="IDisposable.Dispose"/> 
        /// method has previously been called on this object.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="position"/> is less than zero or greater than the length of the buffer.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is null.</exception>
        bool Insert(int position, string text);

        /// <summary>
        /// Inserts an array of characters at the specified <paramref name="position"/> in the <see cref="ITextBuffer"/>.
        /// </summary>
        /// <param name="position">The buffer position at which the first character of the text will appear.</param>
        /// <param name="characterBuffer">The character array from which characters will be inserted.</param>
        /// <param name="startIndex">The index in <paramref name="characterBuffer"/> of the first character to insert.</param>
        /// <param name="length">The number of characters to insert from <paramref name="characterBuffer"/>.</param>
        /// <returns><c>true</c> if the insertion succeeded, <c>false</c> if it was prevented by a read-only region.</returns>
        /// <remarks>Inserting zero characters will succeed but will not generate a new snapshot or raise a
        /// <see cref="ITextBuffer.Changed"/> event.</remarks>
        /// <exception cref="InvalidOperationException">The <see cref="ITextBufferEdit.Apply"/> or <see cref="ITextBufferEdit.Cancel"/> or <see cref="IDisposable.Dispose"/> 
        /// method has previously been called on this object.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="characterBuffer"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="position"/> is less than zero or greater than the length of the buffer, or
        /// <paramref name="startIndex"/> is less than zero, or <paramref name="length"/> is less than zero, or <paramref name="startIndex"/> + <paramref name="length"/> is 
        /// greater than the length of <paramref name="characterBuffer"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">.</exception>
        bool Insert(int position, char[] characterBuffer, int startIndex, int length);

        /// <summary>
        /// Deletes a sequence of characters from the buffer.
        /// </summary>
        /// <param name="deleteSpan">The span of characters to delete.</param>
        /// <returns><c>true</c> if the deletion succeeded, <c>false</c> if it was prevented by a read-only region.</returns>
        /// <remarks>Deleting an empty span will succeed but will not generate a new snapshot or raise a
        /// <see cref="ITextBuffer.Changed"/> event.</remarks>
        /// <exception cref="InvalidOperationException">The <see cref="ITextBufferEdit.Apply"/> or <see cref="ITextBufferEdit.Cancel"/> or <see cref="IDisposable.Dispose"/> 
        /// method has previously been called on this object.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="deleteSpan"/>.End is greater than the length of the buffer.</exception>
        bool Delete(Span deleteSpan);

        /// <summary>
        /// Deletes a equence of characters from the buffer.
        /// </summary>
        /// <param name="startPosition">The position of the first character to delete.</param>
        /// <param name="charsToDelete">The number of characters to delete.</param>
        /// <returns><c>true</c> if the deletion succeeded; <c>false</c> if it was prevented by a read-only region.</returns>
        /// <remarks>Deleting zero characters will succeed but will not generate a new snapshot or raise a
        /// <see cref="ITextBuffer.Changed"/> event.</remarks>
        /// <exception cref="InvalidOperationException">The <see cref="ITextBufferEdit.Apply"/> or <see cref="ITextBufferEdit.Cancel"/> or <see cref="IDisposable.Dispose"/> 
        /// method has previously been called on this object.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="startPosition"/> is less than zero or greater than the length of the buffer, or
        /// <paramref name="charsToDelete"/> is less than zero, or <paramref name="startPosition"/> + <paramref name="charsToDelete"/>
        /// is greater than the length of the buffer.</exception>
        bool Delete(int startPosition, int charsToDelete);

        /// <summary>
        /// Replaces a sequence of characters with different text. This method has the same effect as first deleting the characters in
        /// <paramref name="replaceSpan"/> and then inserting <paramref name="replaceWith"/>.
        /// </summary>
        /// <param name="replaceSpan">The span of characters to replace.</param>
        /// <param name="replaceWith">The new text.</param>
        /// <returns><c>true</c> if the replacement succeeded, <c>false</c> if it was prevented by a read-only region.</returns>
        /// <remarks>Replacing an empty span with an empty string will succeed but will not generate a new snapshot or raise a
        /// <see cref="ITextBuffer.Changed"/> event.</remarks>
        /// <exception cref="InvalidOperationException">The <see cref="ITextBufferEdit.Apply"/> or <see cref="ITextBufferEdit.Cancel"/> or <see cref="IDisposable.Dispose"/> 
        /// method has previously been called on this object.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="replaceSpan"/>.End is greater than the length of the buffer.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="replaceWith"/>is null.</exception>
        bool Replace(Span replaceSpan, string replaceWith);

        /// <summary>
        /// Replaces a sequence of characters with different text. This method has the same effect as first deleting the 
        /// <paramref name="charsToReplace"/> and then inserting <paramref name="replaceWith"/>.
        /// </summary>
        /// <param name="startPosition">The buffer position at which to start replacing.</param>
        /// <param name="charsToReplace">The number of characters to replace.</param>
        /// <param name="replaceWith">The new text.</param>
        /// <returns><c>true</c> if the replacement succeeded; <c>false</c> if it was prevented by a read-only region.</returns>
        /// <remarks>Replacing zero characters with an empty string will succeed but will not generate a new snapshot or raise a
        /// <see cref="ITextBuffer.Changed"/> event.</remarks>
        /// <exception cref="InvalidOperationException">The <see cref="ITextBufferEdit.Apply"/> or <see cref="ITextBufferEdit.Cancel"/> or <see cref="IDisposable.Dispose"/> 
        /// method has previously been called on this object.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="startPosition"/> is less than zero or greater than the length of the buffer, or
        /// <paramref name="charsToReplace"/> is less than zero, or <paramref name="startPosition"/> + <paramref name="charsToReplace"/>
        /// is greater than the length of the buffer.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="replaceWith"/>is null.</exception>
        bool Replace(int startPosition, int charsToReplace, string replaceWith);

        /// <summary>
        /// Determines whether the edit has changes in non-read-only regions.
        /// </summary>
        bool HasEffectiveChanges { get; }

        /// <summary>
        /// Determines whether any changes failed to be added to this edit due to read-only regions.
        /// </summary>
        bool HasFailedChanges { get; }
    }
}
