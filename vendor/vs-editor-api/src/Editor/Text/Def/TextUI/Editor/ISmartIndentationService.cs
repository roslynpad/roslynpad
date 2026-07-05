//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Editor
{
    /// <summary>
    /// Provides a Visual Studio service that determines automatic indentation when the enter key is pressed or
    /// when navigating to an empty line.
    /// </summary>
    /// <remarks>This is a MEF component part, and should be imported as follows:
    /// [Import]
    /// ISmartIndentationService selector = null;
    /// </remarks>
    public interface ISmartIndentationService
    {
        /// <summary>
        /// Gets the desired indentation of an <see cref="ITextSnapshotLine"/> as displayed in <see cref="ITextView"/>.
        /// </summary>
        /// <param name="textView">The text view in which the line is displayed.</param>
        /// <param name="line">The line for which to compute the indentation.</param>
        /// <returns>The number of spaces to place at the start of the line, or null if there is no desired indentation.</returns>
        /// <remarks>
        /// This service consumes <see cref="ISmartIndentProvider"/>s to determine how to perform the indentation.
        /// </remarks>
        int? GetDesiredIndentation(ITextView textView, ITextSnapshotLine line);
    }
}
