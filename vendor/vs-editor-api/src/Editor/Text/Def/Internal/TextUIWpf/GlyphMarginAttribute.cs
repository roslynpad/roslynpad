//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain internal APIs that are subject to change without notice.
// Use at your own risk.
//
using System;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.Text.Editor
{
    /// <summary>
    /// This attribute is used to specify target glyph margin by <see cref="IGlyphMouseProcessorProvider"/>s.
    /// </summary>
    public sealed class GlyphMarginAttribute : MultipleBaseMetadataAttribute
    {
        private readonly string _glyphMargins;

        /// <summary>
        /// Construct a new instance of the attribute.
        /// </summary>
        /// <param name="glyphMargin">The case-insensitive name of the target glyph margin.</param>
        /// <exception cref="ArgumentNullException"><paramref name="glyphMargin"/> is null or empty.</exception>
        public GlyphMarginAttribute(string glyphMargin)
        {
            if (string.IsNullOrEmpty(glyphMargin))
            {
                throw new ArgumentNullException(nameof(glyphMargin));
            }
            _glyphMargins = glyphMargin;
        }

        /// <summary>
        /// The glyph margin name.
        /// </summary>
        public string GlyphMargins
        {
            get { return _glyphMargins; }
        }
    }
}
