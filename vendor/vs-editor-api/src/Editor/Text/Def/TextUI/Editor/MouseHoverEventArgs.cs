//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
#pragma warning disable 1634, 1691

namespace Microsoft.VisualStudio.Text.Editor
{
    using System;
    using Microsoft.VisualStudio.Text;

    /// <summary>
    /// Provides information for a MouseHover event of <see cref="ITextView"/>.
    /// </summary>
    public class MouseHoverEventArgs : EventArgs
    {
        #region Private Members

        ITextView _view;
        int _position;
        IMappingPoint _textPosition;

        #endregion // Private Members

        /// <summary>
        /// Initializes a new instance of a <see cref="MouseHoverEventArgs"/>.
        /// </summary>
        /// <param name="view">The view in which the hover event is being generated.</param>
        /// <param name="position">The position of the character under the mouse in the snapshot span of the view.</param>
        /// <param name="textPosition">The position mapped to the buffer graph of the character under the mouse.</param>
        /// <exception cref="ArgumentNullException"><paramref name="view"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="position"/> is negative or greater than the length of the view's buffer.</exception>
        public MouseHoverEventArgs(ITextView view, int position, IMappingPoint textPosition)
        {
            if (view == null)
                throw new ArgumentNullException(nameof(view));
#pragma warning suppress 56506 // ToDo: Add a comment on why it is not necessary to check view.TextSnapshot
            if ((position < 0) || (position > view.TextSnapshot.Length)) // Allow positions at the end of the file
                throw new ArgumentOutOfRangeException(nameof(position));
            if (textPosition == null)
                throw new ArgumentNullException(nameof(textPosition));
            // we could be very paranoid and check:
            //if (textPosition.AnchorBuffer != view.TextBuffer)
            //    throw new ArgumentException();

            _view = view;
            _position = position;
            _textPosition = textPosition;
        }

        #region Exposed Properties

        /// <summary>
        /// The view for which the hover event is being generated.
        /// </summary>
        public ITextView View
        {
            get { return _view; }
        }

        /// <summary>
        /// The position in the SnapshotSpan of the character under the mouse at the time of the hover.
        /// </summary>
        public int Position 
        { 
            get { return _position; } 
        }

        /// <summary>
        /// The position mapped to the buffer graph of the character under the mouse at the time of the hover.
        /// </summary>
        public IMappingPoint TextPosition
        {
            get { return _textPosition; }
        }

        #endregion // Exposed Properties
    }
}
