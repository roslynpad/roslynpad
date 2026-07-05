//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//

using System;
using Microsoft.VisualStudio.Text.Formatting;

namespace Microsoft.VisualStudio.Text.Editor
{
    /// <summary>
    /// Extensions to <see cref="ITextView"/>, augmenting functionality. For every member here
    /// there should also be an extension method in <see cref="TextViewExtensions"/>.
    /// </summary>
    public interface ITextView2 : ITextView
    {
        /// <summary>
        /// Determines whether the view is in the process of being laid out or is preparing to be laid out.
        /// </summary>
        /// <remarks>
        /// As opposed to <see cref="ITextView.InLayout"/>, it is safe to get the <see cref="ITextView.TextViewLines"/>
        /// but attempting to queue another layout will cause a reentrant layout exception.
        /// </remarks>
        bool InOuterLayout
        {
            get;
        }

        /// <summary>
        /// Gets an object for managing selections within the view.
        /// </summary>
        IMultiSelectionBroker MultiSelectionBroker
        {
            get;
        }

        /// <summary>
        /// Raised whenever the view's MaxTextRightCoordinate is changed.
        /// </summary>
        /// <remarks>
        /// This event will only be rasied if the MaxTextRightCoordinate is changed by changing the MinMaxTextRightCoordinate property
        /// (it will not be raised as a side-effect of a layout even if the layout does change the MaxTextRightCoordinate).
        /// </remarks>
        event EventHandler MaxTextRightCoordinateChanged;

        /// <summary>
        /// Adds an action to be performed after any layouts are complete. If there is not a layout in progress, the action will
        /// be performed immediately. This must be called on the UI thread, and actions will be performed on the UI thread.
        /// </summary>
        /// <param name="action">The action to be performed.</param>
        void QueuePostLayoutAction(Action action);

        /// <summary>
        /// Attempts to get a read-only list of the <see cref="ITextViewLine"/> objects rendered in this view.
        /// </summary>
        /// <remarks>
        /// This list will be dense. That is, all characters between the first character of the first <see cref="ITextViewLine"/> through
        /// the last character of the last <see cref="ITextViewLine"/> will be represented in one of the <see cref="ITextViewLine"/> objects,
        /// except when the layout of the <see cref="ITextViewLine"/> objects is in progress.
        /// <para>
        /// <see cref="ITextViewLine"/> objects are disjoint. That is, a given character is part of only one <see cref="ITextViewLine"/>.
        /// </para>
        /// <para>
        /// The <see cref="ITextViewLine"/> objects are sorted by the index of their first character.
        /// </para>
        /// <para>Some of the <see cref="ITextViewLine"/> objects may not be visible, 
        /// and all <see cref="ITextViewLine"/> objects will be disposed of when the view
        /// recomputes its layout.</para>
        /// <para>This list is occasionally not available due to layouts or other events, and callers should be prepared to handle
        /// a failure.</para>
        /// </remarks>
        /// <param name="textViewLines">Returns out the <see cref="ITextViewLineCollection"/> requested.</param>
        /// <returns>True if succeeded, false otherwise.</returns>
        bool TryGetTextViewLines(out ITextViewLineCollection textViewLines);

        /// <summary>
        /// Attempts to get the <see cref="ITextViewLine"/> that contains the specified text buffer position.
        /// </summary>
        /// <param name="bufferPosition">
        /// The text buffer position used to search for a text line.
        /// </param>
        /// <returns>
        /// True if succeeded, false otherwise.
        /// </returns>
        /// <remarks>
        /// <para>This method returns an <see cref="ITextViewLine"/> if it exists in the view.</para>
        /// <para>If the line does not exist in the cache of formatted lines, it will be formatted and added to the cache.</para>
        /// <para>The returned <see cref="ITextViewLine"/> could be invalidated by either a layout by the view or by subsequent calls to this method.</para>
        /// <para>It is occasionally invalid to retrieve an <see cref="ITextViewLine"/> due to layouts or other events. Callers should be prepared to handle
        /// a failure.</para>
        /// </remarks>
        /// <param name="textViewLine">Returns out the <see cref="ITextViewLine"/> requested.</param>
        bool TryGetTextViewLineContainingBufferPosition(SnapshotPoint bufferPosition, out ITextViewLine textViewLine);
    }
}