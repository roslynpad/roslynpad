//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.VisualStudio.Text
{
    /// <summary>
    /// NewlineState contains counts of each kind of supported newline, and can perform basic logic based on those counts. One of
    /// these is created for each open document, and is kept up-to-date by watching edits through the lifetime of the document.
    /// </summary>
    public class NewlineState
    {
        /// <summary>
        /// Describes the supported types of newlines.
        /// </summary>
        public enum LineEnding
        {
            CRLF,
            CR,
            LF,
            NEL,  // unicode Next Line 0085
            LS,   // unicode Line Separator 2028
            PS,   // unicode Paragraph Separator 2029
        }

        // Counts for the various kinds of newlines. Internal for testing
        internal int _cr;
        internal int _lf;
        internal int _crlf;
        internal int _nel;
        internal int _ls;
        internal int _ps;

        /// <summary>
        /// Increments the count for a specifica line ending by the provided number
        /// </summary>
        /// <param name="lineEnding">Line ending to be adjusted</param>
        /// <param name="count">May be any positive or negative value</param>
        public void Increment(LineEnding lineEnding, int count)
        {
            switch (lineEnding)
            {
                case LineEnding.CRLF:
                    _crlf += count;
                    break;
                case LineEnding.CR:
                    _cr += count;
                    break;
                case LineEnding.LF:
                    _lf += count;
                    break;
                case LineEnding.NEL:
                    _nel += count;
                    break;
                case LineEnding.LS:
                    _ls += count;
                    break;
                case LineEnding.PS:
                    _ps += count;
                    break;
                default:
                    throw new ArgumentException($"Unknown line ending value {lineEnding}", nameof(lineEnding));
            }
        }

        /// <summary>
        /// Gets whether the line endings are in a consistent state (either 0 or 1 kind of newline in the document).
        /// </summary>
        public bool HasConsistentLineEndings
        {
            get
            {
                int numDistinctLineEndings = _cr == 0 ? 0 : 1;
                numDistinctLineEndings += _lf == 0 ? 0 : 1;
                numDistinctLineEndings += _crlf == 0 ? 0 : 1;
                numDistinctLineEndings += _nel == 0 ? 0 : 1;
                numDistinctLineEndings += _ls == 0 ? 0 : 1;
                numDistinctLineEndings += _ps == 0 ? 0 : 1;

                return numDistinctLineEndings <= 1;
            }
        }

        /// <summary>
        /// If <see cref="HasConsistentLineEndings"/> is true, and there is at least one newline in the document,
        /// this will return the kind of line ending found in the document. To determine what kind of newline
        /// the document will use if there are no newlines, callers must inspect editor options.
        /// </summary>
        public LineEnding? InferredLineEnding
        {
            get
            {
                if (!HasConsistentLineEndings)
                {
                    return null;
                }

                // Checks below are roughly sorted in order of expected occurrances.
                if (_crlf != 0)
                {
                    return LineEnding.CRLF;
                }

                if (_lf != 0)
                {
                    return LineEnding.LF;
                }

                if (_cr != 0)
                {
                    return LineEnding.CR;
                }

                if (_nel != 0)
                {
                    return LineEnding.NEL;
                }

                if (_ls != 0)
                {
                    return LineEnding.LS;
                }

                if (_ps != 0)
                {
                    return LineEnding.PS;
                }

                return null;
            }
        }
    }
}
