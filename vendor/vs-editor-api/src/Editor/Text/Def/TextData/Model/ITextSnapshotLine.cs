//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text
{
    /// <summary>
    /// Immutable information about a line of text from an ITextSnapshot.
    /// </summary>
    public interface ITextSnapshotLine
    {
        /// <summary>
        /// The <see cref="ITextSnapshot"/> in which the line appears.
        /// </summary>
        ITextSnapshot Snapshot { get; }

        /// <summary>
        /// The extent of the line, excluding any line break characters.
        /// </summary>
        SnapshotSpan Extent { get; }

        /// <summary>
        /// The extent of the line, including any line break characters.
        /// </summary>
        SnapshotSpan ExtentIncludingLineBreak { get; }

        /// <summary>
        /// The 0-origin line number of the line.
        /// </summary>
        int LineNumber { get; }

        /// <summary>
        /// The <see cref="SnapshotPoint"/> of the first character in the line.
        /// </summary>
        SnapshotPoint Start { get; }

        /// <summary>
        /// Length of the line, excluding any line break characters.
        /// </summary>
        int Length { get; }

        /// <summary>
        /// Length of the line, including any line break characters.
        /// </summary>
        int LengthIncludingLineBreak { get; }

        /// <summary>
        /// The <see cref="SnapshotPoint"/> of the first character past the end of the line, excluding any
        /// line break characters (thus will address a line break character, except 
        /// for the last line in the buffer, in which case it addresses a
        /// position past the end of the buffer).
        /// </summary>
        SnapshotPoint End { get; }

        /// <summary>
        /// The <see cref="SnapshotPoint"/> of the first character past the end of the line, including any
        /// line break characters (thus will address the first character in 
        /// the succeeding line, unless this is the last line, in which case it addresses a
        /// position past the end of the buffer).
        /// </summary>
        SnapshotPoint EndIncludingLineBreak { get; }

        /// <summary>
        /// Length of line break characters (always falls in the range [0..2]).
        /// </summary>
        int LineBreakLength { get; }

        /// <summary>
        /// The text of the line, excluding any line break characters.
        /// </summary>
        string GetText();

        /// <summary>
        /// The text of the line, including any line break characters.
        /// </summary>
        /// <returns></returns>
        string GetTextIncludingLineBreak();

        /// <summary>
        /// The string consisting of the line break characters (if any) at the
        /// end of the line. Has zero length for the last line in the buffer.
        /// </summary>
        /// <returns></returns>
        string GetLineBreakText();
    }
}
