//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text
{
    using System;
    using Microsoft.VisualStudio.Utilities;

    /// <summary>
    /// A mutable sequence of Unicode characters encoded using UTF-16.
    /// Positions within the buffer are treated as a sequence of characters (starting at character zero) or
    /// as a sequence of lines (starting at line zero). An empty buffer has a single line containing no characters.
    /// </summary>
    public interface ITextBuffer : IPropertyOwner
    {
        /// <summary>Gets the content type of the text in the buffer.
        /// </summary>
        IContentType ContentType { get; }

        /// <summary>
        /// Gets the current content of the buffer.
        /// </summary>
        /// <returns></returns>
        ITextSnapshot CurrentSnapshot { get; }

        /// <summary>
        /// Creates an <see cref="ITextEdit"/> object that handles compound edit operations on this buffer.
        /// </summary>
        /// <param name="options">Options to apply to the compound edit operation.</param>
        /// <param name="reiteratedVersionNumber">If not null, indicates that the version to be created by this edit operation is
        /// the product of an undo or redo operation.</param>
        /// <param name="editTag">An arbitrary object that will be associated with this edit transaction.</param>
        /// <returns>A new <see cref="ITextEdit"/> object.</returns>
        /// <exception cref="InvalidOperationException">Another <see cref="ITextBufferEdit"/> object is active for this text buffer, or 
        /// <see cref="CheckEditAccess"/> would return false.</exception>
        ITextEdit CreateEdit(EditOptions options, 
                             int? reiteratedVersionNumber, 
                             object editTag);

        /// <summary>
        /// Creates an <see cref="ITextEdit"/> object that handles compound edit operations on this buffer.
        /// </summary>
        /// <returns>A new <see cref="ITextEdit"/> object.</returns>
        /// <exception cref="InvalidOperationException">Another <see cref="ITextBufferEdit"/> object is active for this text buffer.</exception>
        /// <remarks>This method is equivalent to CreateEdit(EditOptions.None, null, null).</remarks>
        ITextEdit CreateEdit();

        /// <summary>
        /// Creates an <see cref="IReadOnlyRegionEdit"/> object that handles adding or removing read-only regions from this buffer.
        /// </summary>
        /// <returns>A new <see cref="IReadOnlyRegionEdit"/> object.</returns>
        /// <exception cref="InvalidOperationException">Another <see cref="ITextBufferEdit"/> object is active for this text buffer, or 
        /// <see cref="CheckEditAccess"/> would return false.</exception>
        IReadOnlyRegionEdit CreateReadOnlyRegionEdit();

        /// <summary>
        /// Determines whether an edit operation is currently in progress on the <see cref="ITextBuffer"/>.
        /// </summary>
        bool EditInProgress { get; }

        /// <summary>
        /// Claims ownership of this buffer for the current thread. All subsequent modifications of this <see cref="ITextBuffer"/>
        /// must be made from the current thread, or else an <see cref="InvalidOperationException"/> will be raised.
        /// </summary>
        /// <exception cref="InvalidOperationException">This method has been called previously from a different thread, or a
        /// <see cref="ITextBufferEdit"/> object is active for this text buffer.</exception>
        void TakeThreadOwnership();

        /// <summary>
        /// Determines whether edit operations on this text buffer are permitted on the calling thread. If <see cref="TakeThreadOwnership"/> has
        /// previously been called, edit operations are permitted only from the same thread that made that call.
        /// </summary>
        /// <returns><c>true</c> if the calling thread is allowed to perform edit operations, otherwise <c>false</c>.</returns>
        bool CheckEditAccess();

        /// <summary>
        /// Occurs when an <see cref="IReadOnlyRegionEdit"/> has created or removed read-only regions.
        /// </summary>
        event EventHandler<SnapshotSpanEventArgs> ReadOnlyRegionsChanged;

        /// <summary>
        /// Occurs when a non-empty <see cref="ITextEdit"/> is successfully applied. 
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is a performance critical event, whose handlers directly affect typing responsiveness.
        /// Unless it's required to handle this event synchronously on the UI thread, please consider listening
        /// to <see cref="ITextBuffer2.ChangedOnBackground"/> event instead.
        /// </para>
        /// <para>
        /// This event is raised after <see cref="ChangedHighPriority"/> events and before <see cref="ChangedLowPriority"/> events.
        ///</para>
        ///<para>
        /// In the case that a second edit is applied by a listener of the Changed event (or the
        /// ChangedLowPriority or ChangedHighPriority events), the Changed events for the second edit 
        /// won't be raised until all listeners have been notified of the first edit (via ChangedLowPriority, Changed, and 
        /// ChangedHighPriority events).  That is, the events for subsequent edits are queued.  This ensures listeners
        /// receive the Changed events in the order the edits were applied.
        /// </para>
        /// </remarks>
        event EventHandler<TextContentChangedEventArgs> Changed;

        /// <summary>
        /// Occurs when a non-empty <see cref="ITextEdit"/> is successfully applied. 
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is a performance critical event, whose handlers directly affect typing responsiveness.
        /// Unless it's required to handle this event synchronously on the UI thread, please consider listening
        /// to <see cref="ITextBuffer2.ChangedOnBackground"/> event instead.
        /// </para>
        /// <para>
        /// This event is raised after <see cref="ChangedHighPriority"/> and <see cref="Changed"/> events.
        /// </para>
        /// <para>
        /// Changed events for edits made within a ChangedLowPriority, <see cref="Changed"/>, or 
        /// <see cref="ChangedHighPriority" /> listener are queued. See <see cref="Changed"/> for more 
        /// information about event queuing.
        /// </para>
        /// </remarks>
        event EventHandler<TextContentChangedEventArgs> ChangedLowPriority;

        /// <summary>
        /// Occurs when a non-empty <see cref="ITextEdit"/> is successfully applied. 
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is a performance critical event, whose handlers directly affect typing responsiveness.
        /// Unless it's required to handle this event synchronously on the UI thread, please consider listening
        /// to <see cref="ITextBuffer2.ChangedOnBackground"/> event instead.
        /// </para>
        /// <para>
        /// This event is raised before <see cref="Changed"/> and <see cref="ChangedHighPriority"/> events.
        /// </para>
        /// <para>
        /// Changed events for edits made within a ChangedLowPriority, <see cref="Changed"/>, or 
        /// <see cref="ChangedHighPriority" /> listener are queued. See <see cref="Changed"/> for more 
        /// information about event queuing.
        /// </para>
        /// </remarks>
        event EventHandler<TextContentChangedEventArgs> ChangedHighPriority;

        /// <summary>
        /// Occurs just before a non-empty <see cref="ITextEdit"/> is applied.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If the edit operation is canceled in this event, another edit operation can be be started immediately in the event handler.
        /// For example, this event may be used to provide checkout on edit as an implicit, user-cancelable source control functionality.
        /// </para>
        /// </remarks>
        event EventHandler<TextContentChangingEventArgs> Changing;

        /// <summary>
        /// Occurs after the Changed event and any resulting edits.
        /// </summary>
        /// <remarks>
        /// Once <see cref="Changed"/> events have been raised for an edit as well as any resulting
        /// edits (i.e. when an edit is made within a Changed listener), the PostChanged event is
        /// raised.
        /// </remarks>
        event EventHandler PostChanged; 

        /// <summary>
        /// Occurs whenever the <see cref="IContentType"/> has been changed.
        /// </summary>
        event EventHandler<ContentTypeChangedEventArgs> ContentTypeChanged;

        /// <summary>
        /// Changes the <see cref="IContentType"/> for this <see cref="ITextBuffer"/>.
        /// </summary>
        /// <param name="newContentType">The new <see cref="IContentType"/>.</param>
        /// <param name="editTag">An arbitrary object that will be associated with this edit transaction.</param>
        /// <exception cref="ArgumentNullException"><paramref name="newContentType"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Another <see cref="ITextBufferEdit"/> object is active for this <see cref="ITextBuffer"/>, or 
        /// <see cref="CheckEditAccess"/> would return false.</exception>
        void ChangeContentType(IContentType newContentType, object editTag);

        #region Editing shortcuts
        /// <summary>
        /// Inserts the given <paramref name="text"/>at the specified <paramref name="position"/>in the <see cref="ITextBuffer"/>.
        /// </summary>
        /// <param name="position">The buffer position at which the first character of the text will appear.</param>
        /// <param name="text">The text to be inserted.</param>
        /// <remarks>
        /// This is a shortcut for creating a new <see cref="ITextEdit"/> object, using it to insert the text, and then applying it. If the insertion
        /// fails on account of a read-only region, the snapshot returned will be the same as the current snapshot of the buffer before
        /// the attempted insertion.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="position"/> is less than zero or greater than the length of the buffer.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is null.</exception>
        /// <exception cref="InvalidOperationException">A text edit is currently active, or 
        /// <see cref="CheckEditAccess"/> would return false.</exception>
        ITextSnapshot Insert(int position, string text);

        /// <summary>
        /// Deletes a sequence of characters from the buffer.
        /// </summary>
        /// <param name="deleteSpan">The span of characters to delete.</param>
        /// <remarks>
        /// This is a shortcut for creating a new <see cref="ITextEdit"/> object, using it to delete the text, and then applying it. If the deletion
        /// fails on account of a read-only region, the snapshot returned will be the same as the current snapshot of the buffer before
        /// the attempted deletion.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="deleteSpan"/>.End is greater than the length of the buffer.</exception>
        /// <exception cref="InvalidOperationException">A TextEdit is currently active, or 
        /// <see cref="CheckEditAccess"/> would return false.</exception>
        ITextSnapshot Delete(Span deleteSpan);

        /// <summary>
        /// Replaces a sequence of characters with different text. This is equivalent to first deleting the text to be replaced and then
        /// inserting the new text.
        /// </summary>
        /// <param name="replaceSpan">The span of characters to replace.</param>
        /// <param name="replaceWith">The new text to replace the old.</param>
        /// <remarks>
        /// This is a shortcut for creating a new <see cref="ITextEdit"/> object, using it to replace the text, and then applying it. If the replacement
        /// fails on account of a read-only region, the snapshot returned will be the same as the current snapshot of the buffer before
        /// the attempted replacement.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="replaceSpan"/>.End is greater than the length of the buffer.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="replaceWith"/>is null.</exception>
        /// <exception cref="InvalidOperationException">A text edit is currently active, or 
        /// <see cref="CheckEditAccess"/> would return false.</exception>
        ITextSnapshot Replace(Span replaceSpan, string replaceWith);
        #endregion

        #region Read Only Region Queries
        /// <summary>
        /// Determines whether a text insertion would be prohibited at <paramref name="position"/> due to an <see cref="IReadOnlyRegion"/>.
        /// </summary>
        /// <param name="position">The position of the proposed text insertion.</param>
        /// <returns>
        /// <c>true</c> if an <see cref="IReadOnlyRegion"/> would prohibit insertions at this position, otherwise <c>false</c>.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="position"/> is negative or greater than <see cref="CurrentSnapshot"/>.Length.</exception>
        /// <exception cref="InvalidOperationException"><see cref="TakeThreadOwnership"/> has previously been called, and this call is being made
        /// from a different thread.</exception>
        bool IsReadOnly(int position);

        /// <summary>
        /// Determines whether a text insertion would be prohibited at <paramref name="position"/> due to an <see cref="IReadOnlyRegion"/>.
        /// </summary>
        /// <param name="position">The position of the proposed text insertion.</param>
        /// <param name="isEdit"><c>true</c> if this check is part of an edit. <c>false</c> for a query without side effects.</param>
        /// <returns><c>true</c> if an <see cref="IReadOnlyRegion"/> would prohibit insertions at this position, otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="position"/> is negative or greater than <see cref="CurrentSnapshot"/>.Length.</exception>
        /// <exception cref="InvalidOperationException"><see cref="TakeThreadOwnership"/> has previously been called, and this call is being made
        /// from a different thread.</exception>
        bool IsReadOnly(int position, bool isEdit);

        /// <summary>
        /// Determines whether a text modification or deletion would be prohibited at <paramref name="span"/> due to an <see cref="IReadOnlyRegion"/>
        /// </summary>
        /// <param name="span">The span to check.</param>
        /// <returns>
        /// <c>true</c> if the entire span could be deleted or replaced, otherwise <c>false</c>.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="span"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The <see cref="Span.End"/> property of <paramref name="span"/> is greater than <see cref="CurrentSnapshot"/>.Length.</exception>
        /// <exception cref="InvalidOperationException"><see cref="TakeThreadOwnership"/> has previously been called, and this call is being made
        /// from a different thread.</exception>
        bool IsReadOnly(Span span);

        /// <summary>
        /// Determines whether a text modification or deletion would be prohibited at <paramref name="span"/> due to an <see cref="IReadOnlyRegion"/>
        /// </summary>
        /// <param name="span">The span to check.</param>
        /// <param name="isEdit"><c>true</c> if this check is part of an edit. <c>false</c> for a query without side effects.</param>
        /// <returns><c>true</c> if the entire span could be deleted or replaced, <c>false</c> otherwise.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="span"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The <see cref="Span.End"/> property of <paramref name="span"/> is greater than <see cref="CurrentSnapshot"/>.Length.</exception>
        /// <exception cref="InvalidOperationException"><see cref="TakeThreadOwnership"/> has previously been called, and this call is being made
        /// from a different thread.</exception>
        bool IsReadOnly(Span span, bool isEdit);

        /// <summary>
        /// Gets a list of read-only regions that overlap the given span.
        /// </summary>
        /// <param name="span">
        /// The span to check for read-only regions.
        /// </param>
        /// <returns>
        /// A <see cref="NormalizedSpanCollection"/> of read-only regions that intersect the given span.
        /// </returns>
        /// <remarks>
        /// This method returns an empty list if there are no read-only 
        /// regions intersecting the span, or if the span is zero-length.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="span"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="span"/> is past the end of the buffer.</exception>
        /// <exception cref="InvalidOperationException"><see cref="TakeThreadOwnership"/> has previously been called, and this call is being made
        /// from a different thread.</exception>
        NormalizedSpanCollection GetReadOnlyExtents(Span span);
        #endregion
    }    
}
