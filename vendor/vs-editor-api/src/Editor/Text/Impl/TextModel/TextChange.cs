//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
namespace Microsoft.VisualStudio.Text.Implementation
{
    using System;
    using Microsoft.VisualStudio.Text.Utilities;

    /// <summary>
    /// Describes a single contiguous atomic text change operation on the Text Buffer.
    /// 
    /// All text changes are modeled as the replacement of parameter oldText with parameter newText
    /// 
    /// Insertion: oldText == "" and newText != ""
    /// Deletion:  oldText != "" and newText == ""
    /// Replace:   oldText != "" and newText != ""
    /// </summary>
    internal partial class TextChange : ITextChange3
    {
        #region Private Members

        private int _oldPosition;
        private int _newPosition;
        internal StringRebuilder _oldText, _newText;
        private LineBreakBoundaryConditions _lineBreakBoundaryConditions;
        private bool _isOpaque;

        private int? _lineCountDelta = null;
        private int _masterChangeOffset = -1;

        #endregion // Private Members

        /// <summary>
        /// Constructs a Text Change object.
        /// </summary>
        /// <param name="oldPosition">
        /// The character position in the TextBuffer at which the text change happened.
        /// </param>
        /// <param name="oldText">
        /// The text in the buffer that was replaced.
        /// </param>
        /// <param name="newText">
        /// The text that replaces the old text.
        /// </param>
        /// <param name="boundaryConditions">
        /// Information about neighboring line break characters.
        /// </param>
        public TextChange(int oldPosition, StringRebuilder oldText, StringRebuilder newText, LineBreakBoundaryConditions boundaryConditions)
        {
            if (oldPosition < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(oldPosition));
            }

            _oldPosition = oldPosition;
            _newPosition = oldPosition;
            _oldText = oldText;
            _newText = newText;
            _lineBreakBoundaryConditions = boundaryConditions;
        }

        internal TextChange(int oldPosition, string oldText, string newText, LineBreakBoundaryConditions boundaryConditions)
            : this(oldPosition, StringRebuilder.Create(oldText), StringRebuilder.Create(newText), boundaryConditions)
        { }

        public static TextChange Create(int oldPosition, string oldText, string newText, ITextSnapshot currentSnapshot)
        {
            return new TextChange(oldPosition, StringRebuilder.Create(oldText), StringRebuilder.Create(newText), ComputeLineBreakBoundaryConditions(currentSnapshot, oldPosition, oldText.Length));
        }

        public static TextChange Create(int oldPosition, StringRebuilder oldText, string newText, ITextSnapshot currentSnapshot)
        {
            return new TextChange(oldPosition, oldText, StringRebuilder.Create(newText), ComputeLineBreakBoundaryConditions(currentSnapshot, oldPosition, oldText.Length));
        }

        public static TextChange Create(int oldPosition, string oldText, StringRebuilder newText, ITextSnapshot currentSnapshot)
        {
            return new TextChange(oldPosition, StringRebuilder.Create(oldText), newText, ComputeLineBreakBoundaryConditions(currentSnapshot, oldPosition, oldText.Length));
        }

        public static TextChange Create(int oldPosition, StringRebuilder oldText, StringRebuilder newText, ITextSnapshot currentSnapshot)
        {
            return new TextChange(oldPosition, oldText, newText, ComputeLineBreakBoundaryConditions(currentSnapshot, oldPosition, oldText.Length));
        }

        #region Public Properties

        public Span OldSpan
        {
            get { return new Span(_oldPosition, _oldText.Length); }
        }

        public Span NewSpan
        {
            get { return new Span(_newPosition, _newText.Length); }
        }

        public int OldPosition
        {
            get { return _oldPosition; }
            internal set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }
                _oldPosition = value;
            }
        }

        public int NewPosition
        {
            get { return _newPosition; }
            internal set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }
                _newPosition = value;
            }
        }

        public int Delta
        {
            get { return _newText.Length - _oldText.Length; }
        }

        public int OldEnd
        {
            get { return _oldPosition + _oldText.Length; }
        }

        public int NewEnd
        {
            get { return _newPosition + _newText.Length; }
        }

        public string OldText
        {
            get { return _oldText.GetText(new Span(0, _oldText.Length)); }
        }

        public string NewText
        {
            get { return _newText.GetText(new Span(0, _newText.Length)); }
        }

        public int NewLength
        {
            get { return _newText.Length; }
        }

        public int OldLength
        {
            get { return _oldText.Length; }
        }

        public int LineCountDelta
        {
            get
            {
                // we are lazy
                if (!_lineCountDelta.HasValue)
                {
                    _lineCountDelta = TextModelUtilities.ComputeLineCountDelta(_lineBreakBoundaryConditions, _oldText, _newText);
                }
                return _lineCountDelta.Value;
            }
        }

        public bool IsOpaque
        {
            get { return _isOpaque; }
            internal set { _isOpaque = value; }
        }
        #endregion // Public Properties

        #region Public Methods
        public string GetOldText(Span span)
        {
            return _oldText.GetText(span);
        }

        public string GetNewText(Span span)
        {
            return _newText.GetText(span);
        }

        public char GetOldTextAt(int position)
        {
            if (position > this.OldLength)
            {
                throw new ArgumentOutOfRangeException(nameof(position));
            }

            return _oldText[position];
        }

        public char GetNewTextAt(int position)
        {
            if (position > this.NewLength)
            {
                throw new ArgumentOutOfRangeException(nameof(position));
            }

            return _newText[position];
        }
        #endregion

        #region Internal Properties
        internal LineBreakBoundaryConditions LineBreakBoundaryConditions
        {
            get { return _lineBreakBoundaryConditions; }
            set
            {
                _lineBreakBoundaryConditions = value;
                _lineCountDelta = null;
            }
        }

        internal void RecordMasterChangeOffset(int masterChangeOffset)
        {
            if (masterChangeOffset < 0)
                throw new ArgumentOutOfRangeException(nameof(masterChangeOffset), "MasterChangeOffset should be non-negative.");
            if (_masterChangeOffset != -1)
                throw new InvalidOperationException("MasterChangeOffset has already been set.");

            _masterChangeOffset = masterChangeOffset;
        }

        internal int MasterChangeOffset { get { return _masterChangeOffset == -1 ? 0 : _masterChangeOffset; } }

        internal static int Compare(TextChange x, TextChange y)
        {
            int diff = x.OldPosition - y.OldPosition;
            if (diff != 0)
                return diff;
            else
                return x.MasterChangeOffset - y.MasterChangeOffset;
        }

        #endregion

        #region Private helper
        private static LineBreakBoundaryConditions ComputeLineBreakBoundaryConditions(ITextSnapshot currentSnapshot, int position, int oldLength)
        {
            LineBreakBoundaryConditions conditions = LineBreakBoundaryConditions.None;
            if (position > 0 && currentSnapshot[position - 1] == '\r')
            {
                conditions = LineBreakBoundaryConditions.PrecedingReturn;
            }
            int end = position + oldLength;
            if (end < currentSnapshot.Length && currentSnapshot[end] == '\n')
            {
                conditions = conditions | LineBreakBoundaryConditions.SucceedingNewline;
            }
            return conditions;
        }
        #endregion

        #region Overridden methods
        public string ToString(bool brief)
        {
            if (brief)
            {
                return string.Format(System.Globalization.CultureInfo.InvariantCulture, "old={0} new={1}", this.OldSpan, this.NewSpan);
            }
            else
            {
                return string.Format(System.Globalization.CultureInfo.InvariantCulture,
                                     "old={0}:'{1}' new={2}:'{3}'",
                                     this.OldSpan, TextUtilities.Escape(this.OldText), this.NewSpan, TextUtilities.Escape(this.NewText, 40));
            }
        }

        public override string ToString()
        {
            return ToString(false);
        }
        #endregion

        public static StringRebuilder OldStringRebuilder(ITextChange change)
        {
            var textChange = change as TextChange;
            return (textChange != null) ? textChange._oldText : StringRebuilder.Create(change.OldText);
        }

        public static StringRebuilder NewStringRebuilder(ITextChange change)
        {
            var textChange = change as TextChange;
            return (textChange != null) ? textChange._newText : StringRebuilder.Create(change.NewText);
        }

        public static StringRebuilder ChangeOldSubText(ITextChange change, int start, int length)
        {
            var textChange = change as TextChange;
            if (textChange != null)
                return textChange._oldText.GetSubText(new Span(start, length));

            var change3 = change as ITextChange3;
            if (change3 != null)
                return StringRebuilder.Create(change3.GetOldText(new Span(start, length)));

            return StringRebuilder.Create(change.OldText.Substring(start, length));
        }

        public static StringRebuilder ChangeNewSubText(ITextChange change, int start, int length)
        {
            var textChange = change as TextChange;
            if (textChange != null)
                return textChange._newText.GetSubText(new Span(start, length));

            var change3 = change as ITextChange3;
            if (change3 != null)
                return StringRebuilder.Create(change3.GetNewText(new Span(start, length)));

            return StringRebuilder.Create(change.NewText.Substring(start, length));
        }

        public static string ChangeOldSubstring(ITextChange change, int start, int length)
        {
            var textChange = change as TextChange;
            if (textChange != null)
                return textChange._oldText.GetText(new Span(start, length));

            var change3 = change as ITextChange3;
            if (change3 != null)
                return change3.GetOldText(new Span(start, length));

            return change.OldText.Substring(start, length);
        }

        public static string ChangeNewSubstring(ITextChange change, int start, int length)
        {
            var textChange = change as TextChange;
            if (textChange != null)
                return textChange._newText.GetText(new Span(start, length));

            var change3 = change as ITextChange3;
            if (change3 != null)
                return change3.GetNewText(new Span(start, length));

            return change.NewText.Substring(start, length);
        }
    }
}
