//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text
{
    /// <summary>
    /// Describes a single contiguous text change operation on the Text Buffer.
    /// 
    /// All text changes are considered to be the replacement of <c>oldText</c> with <c>newText</c>.
    /// <para>
    /// Insertion is a text change in which <c>oldText</c> is an empty string and <c>newText</c> a non-empty string.
    /// </para>
    /// <para>
    /// Deletion is a text change in which  <c>oldText</c> is a non-empty string and <c>newText</c> is an empty string.
    /// </para>
    /// <para>
    /// Modification is a text change in which both <c>oldText</c> and <c>newText</c> are non-empty strings.
    /// </para>
    /// </summary>
    /// <remarks>
    /// <see cref="ITextChange"/> objects are immutable.
    /// </remarks>
    public interface ITextChange
    {
        /// <summary>
        /// The span of the text change in the snapshot immediately before the change. 
        /// </summary>
        /// <remarks>
        /// This span is empty for a pure insertion. Its start position may differ from NewSpan.Start only when there is more
        /// than one <see cref="ITextChange"/> included in moving from one snapshot to the next.
        /// </remarks>
        Span OldSpan { get; }

        /// <summary>
        /// The span of the <see cref="ITextChange"/> in the snapshot immediately after the change.
        /// </summary>
        /// <remarks>
        /// This span is empty for a pure deletion. Its start position may differ from OldSpan.Start only when there is more
        /// than one <see cref="ITextChange"/> included in moving from one snapshot to the next.
        /// </remarks>
        Span NewSpan { get; }

        /// <summary>
        /// The position of the text change in the snapshot immediately before the change. The position can differ from
        /// NewPosition only when there is more than one <see cref="ITextChange"/> included in moving from one snapshot to the next.
        /// </summary>
        /// <remarks>This is the equivalent of <c>OldSpan.Start</c>.</remarks>
        int OldPosition { get; }

        /// <summary>
        /// The position of the text change in the snapshot immediately after the change. The position can differ from
        /// OldPosition only when there is more than one <see cref="ITextChange"/> included in moving from one snapshot to the next.
        /// </summary>
        /// <remarks>This is the equivalent of <c>NewSpan.Start</c>.</remarks>
        int NewPosition { get; }

        /// <summary>
        /// The effect On the length of the buffer resulting from this change.
        /// </summary>
        int Delta { get; }

        /// <summary>
        /// The end position of the <see cref="OldText"/> in the snapshot immediately before the change.
        /// </summary>
        /// <remarks>Equivalent to <c>OldSpan.End</c>.</remarks>
        int OldEnd { get; }

        /// <summary>
        /// The end position of the <see cref="NewText"/> in the snapshot immediately after the text change.
        /// </summary>
        /// <remarks>Equivalent to <c>NewSpan.End</c>.</remarks>
        int NewEnd { get; }

        /// <summary>
        /// The text that was replaced.
        /// </summary>
        string OldText { get; }

        /// <summary>
        /// The text that replaced the old text.
        /// </summary>
        string NewText { get; }

        /// <summary>
        /// The length of <see cref="OldText"/>.
        /// </summary>
        /// <remarks>This is the equivalent of <c>OldSpan.Length</c>.</remarks>
        int OldLength { get; }

        /// <summary>
        /// The length of <see cref="NewText"/>.
        /// </summary>
        /// <remarks>This is the equivalent of <c>NewSpan.Length</c>.</remarks>
        int NewLength { get; }

        /// <summary>
        /// The effect of this change on the number of lines in the snapshot.
        /// </summary>
        int LineCountDelta { get; }
    }
}
