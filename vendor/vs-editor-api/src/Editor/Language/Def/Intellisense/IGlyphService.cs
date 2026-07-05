////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using Avalonia.Media;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Gets a standard set of glyphs.
    /// </summary>
    /// <remarks>
    /// This is a MEF component part, and should be exported with the following attribute:
    /// [Export(typeof(IGlyphService))]
    /// </remarks>
    public interface IGlyphService
    {
        /// <summary>
        /// Gets a glyph in the form of a WPF <see cref="IImage" />.
        /// </summary>
        /// <param name="group">The group description for this glyph.</param>
        /// <param name="item">The item description for this glyph.</param>
        /// <returns>A valid WPF <see cref="IImage" /> that contains the requested glyph.</returns>
        IImage GetGlyph ( StandardGlyphGroup group, StandardGlyphItem item );
    }
}
