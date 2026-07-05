//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
using System;

namespace Microsoft.VisualStudio.Text
{
    /// <summary>
    /// Provides information about an upcoming edit transaction on a <see cref="ITextBuffer"/>
    /// </summary>
    public class TextContentChangingEventArgs : EventArgs
    {
        private Action<TextContentChangingEventArgs> cancelAction;

        /// <summary>
        /// Determines whether the edit transaction has been canceled.
        /// </summary>
        public bool Canceled { get; private set; }

        /// <summary>
        /// The most recent <see cref="ITextSnapshot"/> before the change.
        /// </summary>
        public ITextSnapshot Before { get; private set; }

        /// <summary>
        /// Gets an arbitrary object provided by the initiator of the changes.
        /// </summary>
        public object EditTag { get; private set; }

        /// <summary>
        /// Initializes a new instance of <see cref="TextContentChangingEventArgs"/> to be passed during a Changing event.
        /// </summary>
        /// <param name="beforeSnapshot">The most recent <see cref="ITextSnapshot"/> before the change.</param>
        /// <param name="editTag">An arbitrary object associated with this change.</param>
        /// <param name="cancelAction">The action to execute when <see cref="Cancel"/> is called. Invoked at most once.</param>
        /// <exception cref="ArgumentNullException"><paramref name="beforeSnapshot"/> is null.</exception>
        public TextContentChangingEventArgs(ITextSnapshot beforeSnapshot, object editTag, Action<TextContentChangingEventArgs> cancelAction)
        {
            if (beforeSnapshot == null)
            {
                throw new ArgumentNullException(nameof(beforeSnapshot));
            }

            Canceled = false;
            Before = beforeSnapshot;
            EditTag = editTag;

            this.cancelAction = cancelAction;
        }

        /// <summary>
        /// Cancels the edit transaction.
        /// </summary>
        public void Cancel()
        {
            if (!Canceled)
            {
                Canceled = true;

                if (cancelAction != null)
                {
                    cancelAction(this);
                }
            }
        }

        /// <summary>
        /// Gets the <see cref="ITextVersion"/> associated with <see cref="Before"/>.
        /// </summary>
        public ITextVersion BeforeVersion
        {
            get
            {
                return Before.Version;
            }
        }
    }
}
