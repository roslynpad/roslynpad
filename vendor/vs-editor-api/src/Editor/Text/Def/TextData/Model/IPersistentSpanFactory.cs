//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text
{
    /// <summary>
    /// A factory for creating <see cref="IPersistentSpan"/>s.
    /// </summary>
    /// <remarks>
    /// <para>PersistentSpans are similar to <see cref="ITrackingSpan"/>s, except they can be created on closed documents and will continue to track even when a document is closed and reopened.</para>
    /// <para>These spans only track changes made while the document is open. They will not track changes made to the document while the document is closed (through Notepad or something similar).</para>
    /// </remarks>
    public interface IPersistentSpanFactory
    {
        /// <summary>
        /// Can an <see cref="IPersistentSpan"/> be created on the specified buffer.
        /// </summary>
        /// <param name="buffer">Buffer in question.</param>
        /// <returns>True if a span can be created.</returns>
        /// <remarks>
        /// Buffers that are not associated with <see cref="ITextDocument"/>s can not be used to create <see cref="IPersistentSpan"/>.
        /// </remarks>
        bool CanCreate(ITextBuffer buffer);

        /// <summary>
        /// Create an <see cref="IPersistentSpan"/> for a snapshot span on a document that is currently open.
        /// </summary>
        /// <param name="span">The span of text in this snapshot.</param>
        /// <param name="trackingMode">How the tracking span will react to changes at its boundaries.</param>
        /// <returns>The newly created span or null if one can not be created.</returns>
        IPersistentSpan Create(SnapshotSpan span, SpanTrackingMode trackingMode);

        /// <summary>
        /// Create an <see cref="IPersistentSpan"/> for a snapshot span on a document that is currently open.
        /// </summary>
        /// <param name="snapshot">The snapshot of the currently open document to create the span on.</param>
        /// <param name="startLine">Line number of the start point.</param>
        /// <param name="startIndex">Character offset from the start of the line containing the start point.</param>
        /// <param name="endLine">Line number of the end point.</param>
        /// <param name="endIndex">Character offset from the start of the line containing the end point.</param>
        /// <param name="trackingMode">How the tracking span will react to changes at its boundaries.</param>
        /// <returns>The newly created span.</returns>
        IPersistentSpan Create(ITextSnapshot snapshot, int startLine, int startIndex, int endLine, int endIndex, SpanTrackingMode trackingMode);

        /// <summary>
        /// Create an <see cref="IPersistentSpan"/> for a snapshot span on a document that is currently closed.
        /// </summary>
        /// <param name="filePath">Name of the file that contains the span.</param>
        /// <param name="startLine">Line number of the start point.</param>
        /// <param name="startIndex">Character offset from the start of the line containing the start point.</param>
        /// <param name="endLine">Line number of the end point.</param>
        /// <param name="endIndex">Character offset from the start of the line containing the end point.</param>
        /// <param name="trackingMode">How the tracking span will react to changes at its boundaries.</param>
        /// <returns>The newly created span.</returns>
        /// <remarks>
        /// Using this method to create an <see cref="IPersistentSpan"/> on a document that is already open is
        /// an error (and will prevent the span from tracking properly).
        /// </remarks>
        IPersistentSpan Create(string filePath, int startLine, int startIndex, int endLine, int endIndex, SpanTrackingMode trackingMode);

        /// <summary>
        /// Create an <see cref="IPersistentSpan"/> for a snapshot span on a document that is currently closed.
        /// </summary>
        /// <param name="filePath">Name of the file that contains the span.</param>
        /// <param name="span">Span as character offsets from the start of the file.</param>
        /// <param name="trackingMode">How the tracking span will react to changes at its boundaries.</param>
        /// <returns>The newly created span.</returns>
        /// <remarks>
        /// Using this method to create an <see cref="IPersistentSpan"/> on a document that is already open is
        /// an error (and will prevent the span from tracking properly).
        /// </remarks>
        IPersistentSpan Create(string filePath, Span span, SpanTrackingMode trackingMode);
    }
}
