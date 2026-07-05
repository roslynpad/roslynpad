//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//

namespace Microsoft.VisualStudio.Text.Editor
{
    /// <summary>
    /// This is a service that supports smart indentation in a file.
    /// </summary>
    /// <remarks>
    /// <remarks>This is a MEF component part, and implementations should use the following to import it:
    /// <code>
    /// [Import]
    /// IIndentationManagerService IndentationManagerService = null;
    /// </code>
    /// </remarks>
    public interface IIndentationManagerService
    {
        /// <summary>
        /// Get the desired indentation behavior for the specified <paramref name="buffer"/>.
        /// </summary>
        /// <param name="explicitFormat">true if the format is due to an explicit user request (e.g. format selection); false if the format is a side-effect of some user action (e.g. typing a newline).</param>
        /// <param name="convertTabsToSpaces">True if tabs should be converted to spaces.</param>
        /// <param name="tabSize">Desired tab size.</param>
        /// <param name="indentSize">Desired indentation.</param>
        void GetIndentation(ITextBuffer buffer, bool explicitFormat, out bool convertTabsToSpaces, out int tabSize, out int indentSize);

        /// <summary>
        /// Determines whether spaces or tab should be used for <paramref name="buffer"/> when formatting.
        /// </summary>
        /// <param name="buffer">A position on the line of text being formatted.</param>
        /// <param name="explicitFormat">true if the format is due to an explicit user request (e.g. format selection); false if the format is a side-effect of some user action (e.g. typing a newline).</param>
        /// <returns>true if spaces should be used.</returns>
        bool UseSpacesForWhitespace(ITextBuffer buffer, bool explicitFormat);

        /// <summary>
        /// Determines the appropriate tab size for <paramref name="buffer"/> when formatting.
        /// </summary>
        int GetTabSize(ITextBuffer buffer, bool explicitFormat);

        /// <summary>
        /// Determines the appropriate indentation size for <paramref name="buffer"/> when formatting.
        /// </summary>
        int GetIndentSize(ITextBuffer buffer, bool explicitFormat);

    }
}
