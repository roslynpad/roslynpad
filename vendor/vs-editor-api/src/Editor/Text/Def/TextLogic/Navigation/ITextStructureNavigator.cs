//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Operations
{
    using Microsoft.VisualStudio.Utilities;

    /// <summary>
    /// Provides methods to navigate text, such as getting word extents.
    /// </summary>
    public interface ITextStructureNavigator
    {
        /// <summary>
        /// Gets the extent of the word at the given position.
        /// </summary>
        /// <remarks><see cref="TextExtent.IsSignificant"/> should be set to <c>false</c> for words 
        /// consisting only of whitespace, unless the whitespace is a significant part of the document. If the 
        /// returned extent consists only of insignificant whitespace, it should include all of the adjacent whitespace, 
        /// including newline characters, spaces, and tabs.</remarks>
        /// <param name="currentPosition">
        /// The text position anywhere in the word for which a <see cref="TextExtent"/> is needed.
        /// </param>
        /// <returns>
        /// A <see cref="TextExtent" /> that represents the word. The <see cref="TextExtent.IsSignificant"/> field is set to <c>false</c> for whitespace or other 
        /// insignificant characters that should be ignored during navigation.
        /// </returns>
        TextExtent GetExtentOfWord(SnapshotPoint currentPosition);

        /// <summary>
        /// Gets the span of the enclosing syntactic element of the specified snapshot span.
        /// </summary>
        /// <param name="activeSpan">
        /// The <see cref="SnapshotSpan"/> from which to get the enclosing syntactic element.
        /// </param>
        /// <returns>
        /// A <see cref="SnapshotSpan"/> that represents the enclosing syntactic element. If the specified snapshot
        /// span covers multiple syntactic elements, then the method returns the least common ancestor of the elements.
        /// If the snapshot span covers the root element (in other words, the whole document),
        /// then the method returns <see cref="SnapshotSpan"/> of the whole document.
        /// </returns>
        SnapshotSpan GetSpanOfEnclosing(SnapshotSpan activeSpan);

        /// <summary>
        /// Gets the span of the first child syntactic element of the specified snapshot span. 
        /// If the snapshot span has zero length, then the behavior is the same as that of 
        /// <see cref="GetSpanOfEnclosing"/>.
        /// </summary>
        /// <param name="activeSpan">
        /// The <see cref="SnapshotSpan"/> from which to get the span of the first child syntactic element.
        /// </param>
        /// <returns>
        /// A <see cref="SnapshotSpan" /> that represents the first child syntactic element. If the specified snapshot 
        /// span covers multiple syntactic elements, then this method returns the span of the least common ancestor of 
        /// the elements. If the specified snapshot span covers the child element, then the 
        /// behavior is the same as that of <see cref="GetSpanOfEnclosing"/>.
        /// </returns>
        SnapshotSpan GetSpanOfFirstChild(SnapshotSpan activeSpan);

        /// <summary>
        /// Gets the span of the next sibling syntactic element of the specified snapshot span. If the
        /// snapshot span has zero length, then the behavior is the same as that of 
        /// <see cref="GetSpanOfEnclosing"/>.
        /// </summary>
        /// <param name="activeSpan">
        /// The <see cref="SnapshotSpan"/> from which to get the span of the next sibling syntactic element.
        /// </param>
        /// <returns>
        /// A <see cref="SnapshotSpan"/> that represents the next sibling syntactic element. If the given active
        /// span covers multiple syntactic elements, then this method returns the span of the next sibling element.
        /// If the specified snapshot span covers a syntactic element that does not have a sibling element, then the 
        /// behavior is the same as that of <see cref="GetSpanOfEnclosing"/>.
        /// </returns>
        SnapshotSpan GetSpanOfNextSibling(SnapshotSpan activeSpan);

        /// <summary>
        /// Gets the span of the previous sibling syntactic element of the specified snapshot span. 
        /// If the specified span has zero length, then the behavior is the same as that of 
        /// <see cref="GetSpanOfEnclosing"/>.
        /// </summary>
        /// <param name="activeSpan">
        /// The <see cref="SnapshotSpan"/> from which to get the span of the previous sibling syntactic element.
        /// </param>
        /// <returns>
        /// A <see cref="SnapshotSpan"/> that represents the previous sibling syntactic element. If the specified snapshot
        /// span covers multiple syntactic elements, then this method returns the span of the previous element. 
        /// If the specified snapshot span covers a syntactic element that does not have a sibling element, then the 
        /// behavior is the same as that of <see cref="GetSpanOfEnclosing"/>.
        /// </returns>
        SnapshotSpan GetSpanOfPreviousSibling(SnapshotSpan activeSpan);
 
        /// <summary>
        /// Gets the content type that this navigator supports.
        /// </summary>
        IContentType ContentType
        {
            get;
        }
    }
}