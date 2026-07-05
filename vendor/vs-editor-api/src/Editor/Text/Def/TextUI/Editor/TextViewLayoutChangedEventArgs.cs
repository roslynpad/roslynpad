//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Editor
{
    using System;
    using Microsoft.VisualStudio.Text;
    using System.Collections.ObjectModel;
    using System.Collections.Generic;
    using Microsoft.VisualStudio.Text.Formatting;

    /// <summary>
    /// Provides information for a Layout Changed event of <see cref="ITextView"/>.
    /// </summary>
    public class TextViewLayoutChangedEventArgs : EventArgs
    {
        #region Private Members

        readonly ViewState _oldViewState;
        readonly ViewState _newViewState;

        readonly ReadOnlyCollection<ITextViewLine> _newOrReformattedLines;
        readonly ReadOnlyCollection<ITextViewLine> _translatedLines;

        NormalizedSnapshotSpanCollection _newOrReformattedSpans;
        NormalizedSnapshotSpanCollection _translatedSpans;

        private static NormalizedSnapshotSpanCollection GetSpans(IList<ITextViewLine> lines)
        {
            if (lines.Count > 0)
            {
                List<Span> spans = new List<Span>();

                foreach (ITextViewLine line in lines)
                {
                    spans.Add(line.ExtentIncludingLineBreak.Span);
                }

                return new NormalizedSnapshotSpanCollection(lines[0].Snapshot, spans);
            }
            else
            {
                return NormalizedSnapshotSpanCollection.Empty;
            }
        }
        #endregion // Private Members

        /// <summary>
        /// Initializes a new instance of a <see cref="TextViewLayoutChangedEventArgs"/>.
        /// </summary>
        /// <param name="oldState">
        /// State of the view prior to the layout.
        /// </param>
        /// <param name="newState">
        /// State of the view after the layout.
        /// </param>
        /// <param name="newOrReformattedLines">A list of the new or reformatted <see cref="ITextViewLine"/>.</param>
        /// <param name="translatedLines">A list of the translated <see cref="ITextViewLine"/>.</param>
        /// <exception name="ArgumentNullException"><paramref name="oldState"/>, <paramref name="newState"/>, <paramref name="translatedLines"/> or <paramref name="newOrReformattedLines"/> is null.</exception>
        public TextViewLayoutChangedEventArgs(ViewState oldState, ViewState newState,
                                              IList<ITextViewLine> newOrReformattedLines,
                                              IList<ITextViewLine> translatedLines)
        {
            if (oldState == null)
                throw new ArgumentNullException(nameof(oldState));
            if (newState == null)
                throw new ArgumentNullException(nameof(newState));
            if (translatedLines == null)
                throw new ArgumentNullException(nameof(translatedLines));
            if (newOrReformattedLines == null)
                throw new ArgumentNullException(nameof(newOrReformattedLines));

            _oldViewState = oldState;
            _newViewState = newState;

            _newOrReformattedLines = new ReadOnlyCollection<ITextViewLine>(newOrReformattedLines);
            _translatedLines = new ReadOnlyCollection<ITextViewLine>(translatedLines);
        }

        #region Exposed Properties
        /// <summary>
        /// State of the view prior to the layout.
        /// </summary>
        public ViewState OldViewState { get { return _oldViewState; } }

        /// <summary>
        /// State of the view after the layout.
        /// </summary>
        public ViewState NewViewState { get { return _newViewState; } }

        /// <summary>
        /// Has the view translated horizontally since the last layout?
        /// </summary>
        public bool HorizontalTranslation { get { return _oldViewState.ViewportLeft != _newViewState.ViewportLeft; } }

        /// <summary>
        /// Has the view translated vertically since the last layout?
        /// </summary>
        public bool VerticalTranslation { get { return _oldViewState.ViewportTop != _newViewState.ViewportTop; } }

        /// <summary>
        /// Gets the old snapshot of the view.
        /// </summary>
        /// <remarks>Deprecated. Use OldViewState.EditSnapshot instead.</remarks>
        public ITextSnapshot OldSnapshot { get { return _oldViewState.EditSnapshot; } }

        /// <summary>
        /// Gets the new snapshot produced by the changed layout.
        /// </summary>
        /// <remarks>Deprecated. Use NewViewState.EditSnapshot instead.</remarks>
        public ITextSnapshot NewSnapshot { get { return _newViewState.EditSnapshot; } }

        /// <summary>
        /// Gets a read-only collection of new or reformatted lines.
        /// </summary>
        public ReadOnlyCollection<ITextViewLine> NewOrReformattedLines { get { return _newOrReformattedLines; } }

        /// <summary>
        /// Gets a read-only collection of translated lines.
        /// </summary>
        public ReadOnlyCollection<ITextViewLine> TranslatedLines { get { return _translatedLines; } }
        
        /// <summary>
        /// Gets a collection the spans that are either new or have been reformatted.
        /// </summary>
        public NormalizedSnapshotSpanCollection NewOrReformattedSpans
        {
            get
            {
                if (_newOrReformattedSpans == null)
                    _newOrReformattedSpans = GetSpans(_newOrReformattedLines);
                return _newOrReformattedSpans; 
            }
        }

        /// <summary>
        /// Gets a collection spans that have been translated.
        /// </summary>
        public NormalizedSnapshotSpanCollection TranslatedSpans
        {
            get
            {
                if (_translatedSpans == null)
                    _translatedSpans = GetSpans(_translatedLines);
                return _translatedSpans; 
            }
        }
        #endregion // Exposed Properties
    }
}
