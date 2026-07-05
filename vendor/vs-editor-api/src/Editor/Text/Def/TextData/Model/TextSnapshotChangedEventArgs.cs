//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text
{
    using System;

    /// <summary>
    /// Provides information about a transaction on a <see cref="ITextBuffer"/> 
    /// that causes a new <see cref="ITextSnapshot"/> to be generated.
    /// </summary>
    public abstract class TextSnapshotChangedEventArgs : EventArgs
    {
        #region Private Members and Construction

        private ITextSnapshot before;
        private ITextSnapshot after;
        private Object editTag;

        /// <summary>
        /// Initializes a new instance of a <see cref="TextSnapshotChangedEventArgs"/> for a Change event.
        /// </summary>
        /// <param name="beforeSnapshot">The most recent <see cref="ITextSnapshot"/> before the change occurred.</param>
        /// <param name="afterSnapshot">The <see cref="ITextSnapshot"/> immediately after the change occurred.</param>
        /// <param name="editTag">An arbitrary object associated with this change.</param>
        /// <exception cref="ArgumentNullException"><paramref name="beforeSnapshot"/> or <paramref name="afterSnapshot"/> is null.</exception>
        protected TextSnapshotChangedEventArgs(ITextSnapshot beforeSnapshot,
                                               ITextSnapshot afterSnapshot,
                                               object editTag)
        {
            if (beforeSnapshot == null)
            {
                throw new ArgumentNullException(nameof(beforeSnapshot));
            }
            if (afterSnapshot == null)
            {
                throw new ArgumentNullException(nameof(afterSnapshot));
            }
            this.before = beforeSnapshot;
            this.after = afterSnapshot;
            this.editTag = editTag;
        }
        #endregion // Private Members and Construction

        #region Public Properties

        /// <summary>
        /// Gets the state of the <see cref="ITextBuffer"/> before the change occurred.
        /// </summary>
        public ITextSnapshot Before
        {
            get { return this.before; }
        }

        /// <summary>
        /// Gets the state of the <see cref="ITextBuffer"/> after the change.
        /// </summary>
        public ITextSnapshot After
        {
            get { return this.after; }
        }

        /// <summary>
        /// Gets the <see cref="ITextVersion"/> associated with <see cref="Before"/>.
        /// </summary>
        public ITextVersion BeforeVersion
        {
            get { return this.before.Version; }
        }

        /// <summary>
        /// Gets the <see cref="ITextVersion"/>n associated with <see cref="After"/>.
        /// </summary>
        public ITextVersion AfterVersion
        {
            get { return this.after.Version; }
        }

        /// <summary>
        /// Gets an arbitrary object provided by the initiator of the changes.
        /// </summary>
        public Object EditTag
        {
            get { return this.editTag; }
        }
        #endregion
    }
}
