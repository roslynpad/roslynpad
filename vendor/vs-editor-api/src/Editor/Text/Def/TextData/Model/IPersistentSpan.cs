//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
using System;
namespace Microsoft.VisualStudio.Text
{
    /// <summary>
    /// Describes span in a document that remains valid even when the document is closed, opened or modified (while it is open).
    /// </summary>
    /// <remarks>
    /// <para>PersistentSpans are similar to <see cref="ITrackingSpan"/>s, except they can be created on closed documents and will continue to track even when a document is closed and reopened.</para>
    /// <para>These spans only track changes made while the document is open. They will not track changes made to the document while the document is closed (through Notepad or something similar).</para>
    /// </remarks>
    public interface IPersistentSpan : IDisposable
    {
        /// <summary>
        /// Returns true if the document associated with the <see cref="IPersistentSpan"/> is currently open.
        /// </summary>
        bool IsDocumentOpen { get; }

        /// <summary>
        /// Returns the span's <see cref="ITextDocument"/> if the underlying document is open (and null if the document is closed).
        /// </summary>
        ITextDocument Document { get; }

        /// <summary>
        /// Returns the span's <see cref="ITrackingSpan"/> if the underlying document is open (and null if the document is closed).
        /// </summary>
        ITrackingSpan Span { get; }

        /// <summary>
        /// Returns the file path of the document.
        /// </summary>
        /// <remarks>
        /// This property is valid whether or not the document is open.
        /// </remarks>
        string FilePath { get; }

        /// <summary>
        /// Get the starting point of the span as a line number and offset from the start of the line.
        /// </summary>
        /// <param name="startLine">Line number of the start point.</param>
        /// <param name="startIndex">Character offset from the start of the line containing the start point.</param>
        /// <returns>true if the the out parameters are valid; false otherwise (in which case TryGetSpan will work).</returns>
        /// <remarks>
        /// <para>This method can be used on any span if the underlying document is open.</para>
        /// <para>If the underlying document is closed, then this method can be used if either the document was created on the closed document using the line/index method
        /// of the factory or the document had ever been opened after the span was created.</para>
        /// <para>In general, you should try this method (and <see cref="TryGetEndLineIndex"/>) before using <see cref="TryGetSpan"/>.</para>
        /// </remarks>
        bool TryGetStartLineIndex(out int startLine, out int startIndex);

        /// <summary>
        /// Get the ending point of the span as a line number and offset from the start of the line.
        /// </summary>
        /// <param name="endLine">Line number of the end point.</param>
        /// <param name="endIndex">Character offset from the start of the line containing the end point.</param>
        /// <returns>true if the the out parameters are valid; false otherwise (in which case TryGetSpan will work).</returns>
        /// <remarks>
        /// <para>This method can be used on any span if the underlying document is open.</para>
        /// <para>If the underlying document is closed, then this method can be used if either the document was created on the closed document using the line/index method
        /// of the factory or the document had ever been opened after the span was created.</para>
        /// <para>In general, you should try this method (and <see cref="TryGetStartLineIndex"/>) before using <see cref="TryGetSpan"/>.</para>
        /// </remarks>
        bool TryGetEndLineIndex(out int endLine, out int endIndex);

        /// <summary>
        /// Get the span as a character offset from the start of the buffer.
        /// </summary>
        /// <param name="span">Corresponding span in the buffer.</param>
        /// <returns>true if the the out parameters are valid; false otherwise (in which case TryGetStartLineIndex/TryGetEndLineIndex will work).</returns>
        /// <remarks>
        /// <para>This method can be used on any span if the underlying document is open.</para>
        /// <para>If the underlying document is closed, then this method only be used if the document was created using the span method of the factory and the
        /// document has never been opened after the span was created.</para>
        /// <para>In general, you should try to use <see cref="TryGetStartLineIndex"/> and <see cref="TryGetEndLineIndex"/> before using this method.</para>
        /// </remarks>
        bool TryGetSpan(out Span span);
    }
}