//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Editor
{
    /// <summary>
    /// Sets a bitwise combination of enumeration values to specify the word wrap style of an <see cref="ITextView"/>. 
    /// </summary>
    /// <remarks>The VisibleGlyphs and AutoIndent bits will have no effect
    /// unless the WordWrap bit is also set.
    /// </remarks>
    [System.Flags]
    public enum WordWrapStyles
    {
        /// <summary>
        /// Word wrap is disabled.
        /// </summary>
        None = 0x00,
        /// <summary>
        /// Word wrap is enabled.
        /// </summary>
        WordWrap = 0x01,
        /// <summary>
        /// If word wrap is enabled, use visible glyphs.
        /// </summary>
        VisibleGlyphs = 0x02,
        /// <summary>
        /// If word wrap is enabled, use auto-indent.
        /// </summary>
        AutoIndent = 0x04
    }
}