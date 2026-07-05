// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace Microsoft.VisualStudio.Text.PatternMatching.Implementation
{
    /// <summary>
    /// Case-insensitive operations (mostly comparison) on unicode strings.
    /// </summary>
    public static class CaseInsensitiveComparison
    {
        // PERF: Cache a TextInfo for Unicode ToLower since this will be accessed very frequently
        private static readonly TextInfo s_unicodeCultureTextInfo = GetUnicodeCulture().TextInfo;

        private static CultureInfo GetUnicodeCulture()
        {
            try
            {
                // We use the "en" culture to get the Unicode ToLower mapping, as it implements
                // a much more recent Unicode version (6.0+) than the invariant culture (1.0),
                // and it matches the Unicode version used for character categorization.
                return new CultureInfo("en");
            }
            catch (ArgumentException) // System.Globalization.CultureNotFoundException not on all platforms
            {
                // If "en" is not available, fall back to the invariant culture. Although it has bugs
                // specific to the invariant culture (e.g. being version-locked to Unicode 1.0), at least
                // we can rely on it being present on all platforms.
                return CultureInfo.InvariantCulture;
            }
        }

        /// <summary>
        /// ToLower implements the Unicode lowercase mapping
        /// as described in ftp://ftp.unicode.org/Public/UNIDATA/UnicodeData.txt.
        /// VB uses these mappings for case-insensitive comparison.
        /// </summary>
        /// <param name="c"></param>
        /// <returns>If <paramref name="c"/> is upper case, then this returns its Unicode lower case equivalent. Otherwise, <paramref name="c"/> is returned unmodified.</returns>
        public static char ToLower(char c)
        {
            // PERF: This is a very hot code path in VB, optimize for ASCII

            // Perform a range check with a single compare by using unsigned arithmetic
            if (unchecked((uint)(c - 'A')) <= ('Z' - 'A'))
            {
                return (char)(c | 0x20);
            }

            if (c < 0xC0) // Covers ASCII (U+0000 - U+007F) and up to the next upper-case codepoint (Latin Capital Letter A with Grave)
            {
                return c;
            }

            return ToLowerNonAscii(c);
        }

        private static char ToLowerNonAscii(char c)
        {
            if (c == '\u0130')
            {
                // Special case Turkish I (LATIN CAPITAL LETTER I WITH DOT ABOVE)
                // This corrects for the fact that the invariant culture only supports Unicode 1.0
                // and therefore does not "know about" this character.
                return 'i';
            }

            return s_unicodeCultureTextInfo.ToLower(c);
        }
    }
}
