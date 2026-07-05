//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text
{
    using System;

    /// <summary>
    /// Represents edit operations against a <see cref="ITextBuffer"/>.
    /// </summary>
    public interface ITextBufferEdit : IDisposable
    {
        /// <summary>
        /// A snapshot of the <see cref="ITextBuffer"/> at the time this ITextBufferEdit object was created.
        /// </summary>
        ITextSnapshot Snapshot { get; }

        /// <summary>
        /// Commits all the modifications made with this <see cref="ITextBufferEdit"/> object to the underlying <see cref="ITextBuffer"/>. 
        /// Depending on the type of edit, it may also cause the <see cref="ITextBuffer"/> to generate a new snapshot and raise its Changed 
        /// event if any modifications were made. This method may be called only 
        /// once. After it is called, any other calls on this object (other than Dispose) will result in an <see cref="InvalidOperationException"/>.
        /// </summary>
        /// <remarks>
        /// Canceled will be <c>true</c> after this method returns if a handler of the buffer's Changing event canceled the change.
        /// </remarks>
        /// <returns>
        /// A snapshot of the state of the <see cref="ITextBuffer"/> after the change is applied. 
        /// If there was no change, or edit was canceled, or the edit is of a type that does not generate snapshots, no new snapshot will be created,
        /// and the previous snapshot will be returned.
        /// </returns>
        /// <exception cref="InvalidOperationException">The <see cref="Apply"/> or <see cref="Cancel"/> or <see cref="IDisposable.Dispose"/> 
        /// method has previously been called on this object.</exception>
        ITextSnapshot Apply();

        /// <summary>
        /// Abandons all modifications started using this <see cref="ITextBufferEdit"/> object. Any further calls 
        /// on this object will result in an <see cref="InvalidOperationException"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">The <see cref="IDisposable.Dispose"/> 
        /// method has previously been called on this object, or the edit has already been applied.</exception>
        void Cancel();

        /// <summary>
        /// Determines whether this edit has been canceled.
        /// </summary>
        bool Canceled { get; }
    }
}