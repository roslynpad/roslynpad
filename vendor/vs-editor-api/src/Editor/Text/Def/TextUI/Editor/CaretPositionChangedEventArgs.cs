//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Editor
{
    using System;

    /// <summary>
    /// Provides information for the <see cref="ITextCaret.PositionChanged"/> event.
    /// </summary>
    public class CaretPositionChangedEventArgs : EventArgs
    {
        #region Private Members
        ITextView _textView;
        CaretPosition _oldPosition, _newPosition;
        #endregion // Private Members

        /// <summary>
        /// Initializes a new instance of <see cref="CaretPositionChangedEventArgs"/>.
        /// </summary>
        /// <param name="textView">
        /// The <see cref="ITextView"/> that contains the caret.
        /// </param>
        /// <param name="oldPosition">
        /// The old <see cref="CaretPosition"/>.
        /// </param>
        /// <param name="newPosition">
        /// The new <see cref="CaretPosition"/>.
        /// </param>
        public CaretPositionChangedEventArgs(ITextView textView, CaretPosition oldPosition, CaretPosition newPosition)
        {
            _textView = textView;
            _oldPosition = oldPosition;
            _newPosition = newPosition;
        }

        #region Exposed Properties

        /// <summary>
        /// Gets the <see cref="ITextView"/> that contains the caret.
        /// </summary>
        public ITextView TextView
        {
            get
            {
                return _textView;
            }
        }

        /// <summary>
        /// Gets the old <see cref="CaretPosition"/>.
        /// </summary>
        public CaretPosition OldPosition
        {
            get
            {
                return _oldPosition;
            }
        }

        /// <summary>
        /// Gets the new <see cref="CaretPosition"/>.
        /// </summary>
        public CaretPosition NewPosition
        {
            get
            {
                return _newPosition;
            }
        }

        #endregion // Exposed Properties
    }
}