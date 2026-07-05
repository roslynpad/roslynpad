//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Editor
{
    using System;
    using System.Collections.ObjectModel;
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Formatting;

    /// <summary>
    /// Represents the selected text in an <see cref="ITextView"/>
    /// </summary>
    public interface ITextSelection
    {
        /// <summary>
        /// Gets the text view to which this selection belongs.
        /// </summary>
        ITextView TextView { get; }

        /// <summary>
        /// Selects the text in the specified <paramref name="selectionSpan"/>.
        /// </summary>
        /// <param name="selectionSpan">The <see cref="SnapshotSpan"/> of text to select in the 
        /// underlying text buffer.</param>
        /// <param name="isReversed"><c>true</c> if the selection was made in a reverse direction, otherwise <c>false</c>.</param>
        void Select(SnapshotSpan selectionSpan, bool isReversed);

        /// <summary>
        /// Select from the anchor point to the active point.
        /// </summary>
        /// <param name="anchorPoint">The anchor point</param>
        /// <param name="activePoint">The active point</param>
        void Select(VirtualSnapshotPoint anchorPoint, VirtualSnapshotPoint activePoint);
       
        /// <summary>
        /// The currently-selected spans.
        /// </summary>
        /// <remarks>
        /// <para>This span collection will never be empty.  However, the spans in
        /// this collection may be 0-length.</para>
        /// <para>This value can be very expensive to compute the first time after the selection has changed.</para>
        /// <para>Use GetSelectionOnTextViewLine() unless you need the entire selection.</para>
        /// </remarks>
        NormalizedSnapshotSpanCollection SelectedSpans { get; }

        /// <summary>
        /// The currently-selected spans, as <see cref="VirtualSnapshotSpan" /> objects.
        /// </summary>
        /// <remarks>
        /// <para>This span collection will never be empty.  However, the spans in
        /// this collection may be 0-length.</para>
        /// <para>This value can be very expensive to compute the first time after the selection has changed.</para>
        /// <para>Use GetSelectionOnTextViewLine() unless you need the entire selection.</para>
        /// </remarks>
        ReadOnlyCollection<VirtualSnapshotSpan> VirtualSelectedSpans { get; }

        /// <summary>
        /// Get the selection on a particular <see cref="ITextViewLine"/>.
        /// </summary>
        /// <param name="line">Line for which to get the selection.</param>
        /// <returns>The selection on <paramref name="line"/>.</returns>
        VirtualSnapshotSpan? GetSelectionOnTextViewLine(ITextViewLine line);

        /// <summary>
        /// Get the current selection as if it were a stream selection, regardless
        /// of the current selection mode.
        /// </summary>
        VirtualSnapshotSpan StreamSelectionSpan { get; }

        /// <summary>
        /// Gets or sets the selection mode.
        /// </summary>
        TextSelectionMode Mode { get; set; }

        /// <summary>
        /// Is <c>true</c> if the <see cref="ActivePoint"/> comes before the <see cref="AnchorPoint"/>.
        /// </summary>
        bool IsReversed { get; }

        /// <summary>
        /// Clears the selection.
        /// </summary>
        /// <remarks>
        /// After calling this method, <see cref="IsEmpty"/> will be <c>true</c>.
        /// </remarks>
        void Clear();

        /// <summary>
        /// Determines whether the selection is empty.
        /// </summary>
        /// <remarks>The selection is empty if the active and anchor points are
        /// the same point.</remarks>
        bool IsEmpty { get; }

        /// <summary>
        /// Whether or not the selection is active.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If <see cref="ActivationTracksFocus"/> is <c>true</c>, this property is automatically
        /// updated when the <see cref="ITextView"/> gains and loses aggregate focus.  You can still
        /// override it while <see cref="ActivationTracksFocus"/> is <c>false</c>, but the value will change
        /// whenever focus changes.
        /// </para>
        /// </remarks>
        bool IsActive { get; set; }

        /// <summary>
        /// Determines whether <see cref="IsActive"/> should track when the <see cref="ITextView"/> gains and
        /// loses aggregate focus.  The default is <c>true</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// While the value of this property is <c>true</c>, the value of <see cref="IsActive"/> will track
        /// <see cref="ITextView.HasAggregateFocus"/>.  When the value of this property changes to <c>true</c>,
        /// the value of <see cref="IsActive"/> will be immediately updated.
        /// </para>
        /// </remarks>
        bool ActivationTracksFocus { get; set; }

        /// <summary>
        /// Occurs when Select or Clear start to be called.  The sender of the event will be this <see cref="ITextSelection"/>.
        /// </summary>
        /// <remarks>
        /// This event is not raised if the selection shrinks or grows as a result of its associated span expanding or shrinking.
        /// </remarks>
        event EventHandler SelectionChanged;

        /// <summary>
        /// Gets the active point of the selection.
        /// </summary>
        /// <remarks><para>This point normally corresponds to the end of the selection that contains to the caret position.</para>
        /// <para>If the selection is reversed, then this point will come before the AnchorPoint.</para></remarks>
        VirtualSnapshotPoint ActivePoint { get; }

        /// <summary>
        /// Gets the anchor point of the selection.
        /// </summary>
        /// <remarks><para>This normally corresponds to the end of the selection that does not contain to the caret position.</para>
        /// <para>If the selection is reversed, then this point will come after the ActivePoint.</para></remarks>

        VirtualSnapshotPoint AnchorPoint { get; }

        /// <summary>
        /// Gets the start point of the selection.
        /// </summary>
        /// <remarks>This is either the active point or the anchor point, whichever comes first.</remarks>
        VirtualSnapshotPoint Start { get; }

        /// <summary>
        /// Gets the end point of the selection.
        /// </summary>
        /// <remarks>This is either the active point or the anchor point, whichever comes last.</remarks>
        VirtualSnapshotPoint End { get; }

    }
}
