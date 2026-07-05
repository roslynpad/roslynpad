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
    /// LeadingWhitespaceState contains counts of lines starting with space, tab, or neither. It can perform basic logic based on
    /// those counts. One of these is created for each open document, and is kept up-to-date by watching edits through the
    /// lifetime of the document.
    /// </summary>
    public class LeadingWhitespaceState
    {
        /// <summary>
        /// Describes the supported types of leading characters.
        /// </summary>
        public enum LineLeadingCharacter
        {
            Tab,
            Space,
            Printable,
            Empty
        }

        public int LinesBeginningWithSpaces => _space;
        public int LinesBeginningWithTabs => _tab;

        // Counts for the various kinds of leading characters. Internal for testing
        internal int _tab;
        internal int _space;
        internal int _printable;
        internal int _empty;

        /// <summary>
        /// Increments the count for a leading character by the provided number
        /// </summary>
        /// <param name="leadingCharacter">Leading character to be adjusted</param>
        /// <param name="count">May be any positive or negative value</param>
        public void Increment(LineLeadingCharacter leadingCharacter, int count)
        {
            switch (leadingCharacter)
            {
                case LineLeadingCharacter.Tab:
                    _tab += count;
                    break;
                case LineLeadingCharacter.Space:
                    _space += count;
                    break;
                case LineLeadingCharacter.Printable:
                    _printable += count;
                    break;
                case LineLeadingCharacter.Empty:
                    _empty += count;
                    break;
                default:
                    throw new ArgumentException($"Unknown leading whitespace value {leadingCharacter}", nameof(leadingCharacter));
            }
        }

        /// <summary>
        /// Gets whether the leading WHITESPACE characters are in a consistent state,
        /// (either 0 or 1 kind of leading whites[ace in the document). Empty lines and
        /// lines starting with printable characters are ignored.
        /// </summary>
        public bool HasConsistentLeadingWhitespace
        {
            get
            {
                int distinctCount = _tab == 0 ? 0 : 1;
                distinctCount += _space == 0 ? 0 : 1;

                return distinctCount <= 1;
            }
        }

        /// <summary>
        /// Returns the kind of leading character that appears most in the document between tabs and spaces.
        /// If they are exactly tied, this will pick the default value.
        /// </summary>
        public LineLeadingCharacter GetLeadingWhitespaceCharacter(LineLeadingCharacter defaultValue)
        {
            if (_tab > _space)
            {
                return LineLeadingCharacter.Tab;
            }
            else if (_space > _tab)
            {
                return LineLeadingCharacter.Space;
            }
            else
            {
                return defaultValue;
            }
        }
    }
}
