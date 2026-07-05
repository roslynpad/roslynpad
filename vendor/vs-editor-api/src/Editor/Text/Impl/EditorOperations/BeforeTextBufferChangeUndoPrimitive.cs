//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
namespace Microsoft.VisualStudio.Text.Operations.Implementation
{
    using System;
    using System.Diagnostics;
    using Microsoft.VisualStudio.Text.Editor;

    /// <summary>
    /// The UndoPrimitive to take place on the Undo stack before a text buffer change. This is the simpler
    /// version of the primitive that handles most common cases.
    /// </summary>
    internal class BeforeTextBufferChangeUndoPrimitive : TextUndoPrimitive
    {
        // Think twice before adding any fields here! These objects are long-lived and consume considerable space.
        // Unusual cases should be handled by the GeneralAfterTextBufferChangedUndoPrimitive class below.
        private readonly ITextUndoHistory _undoHistory;
        public readonly SelectionState State;
        private bool _canUndo;

        /// <summary>
        /// Constructs a BeforeTextBufferChangeUndoPrimitive.
        /// </summary>
        /// <param name="textView">
        /// The text view that was responsible for causing this change.
        /// </param>
        /// <param name="undoHistory">
        /// The <see cref="ITextUndoHistory" /> this primitive will be added to.
        /// </param>
        /// <exception cref="ArgumentNullException"><paramref name="textView"/> is null.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="undoHistory"/> is null.</exception>
        public static BeforeTextBufferChangeUndoPrimitive Create(ITextView textView, ITextUndoHistory undoHistory)
        {
            if (textView == null)
            {
                throw new ArgumentNullException(nameof(textView));
            }
            if (undoHistory == null)
            {
                throw new ArgumentNullException(nameof(undoHistory));
            }

            return new BeforeTextBufferChangeUndoPrimitive(textView, undoHistory);
        }

        private BeforeTextBufferChangeUndoPrimitive(ITextView textView, ITextUndoHistory undoHistory)
        {
            _undoHistory = undoHistory;
            this.State = new SelectionState(textView);
            _canUndo = true;
        }

        // Internal empty constructor for unit testing.
        internal BeforeTextBufferChangeUndoPrimitive() { }

        /// <summary>
        /// The <see cref="ITextView"/> that this <see cref="BeforeTextBufferChangeUndoPrimitive"/> is bound to.
        /// </summary>
        internal ITextView GetTextView()
        {
            ITextView view = null;
            _undoHistory.Properties.TryGetProperty(typeof(ITextView), out view);
            return view;
        }

        #region UndoPrimitive Members

        /// <summary>
        /// Returns true if operation can be undone, false otherwise.
        /// </summary>
        public override bool CanUndo
        {
            get { return _canUndo; }
        }

        /// <summary>
        /// Returns true if operation can be redone, false otherwise.
        /// </summary>
        public override bool CanRedo
        {
            get { return !_canUndo; }
        }

        /// <summary>
        /// Do the action.
        /// </summary>
        /// <exception cref="InvalidOperationException">Operation cannot be redone.</exception>
        public override void Do()
        {
            // Validate, we shouldn't be allowed to undo
            if (!CanRedo)
            {
                throw new InvalidOperationException(Strings.CannotRedo);
            }

            // Currently, no action is done on the redo.  To set the caret and selection for after a TextBuffer change redo, there is the AfterTextBufferChangeUndoPrimitive.
            // This Redo should not do anything with the caret and selection, because we only want to reset them after the TextBuffer change has occurred.
            // Therefore, we need to add this UndoPrimitive to the undo stack before the UndoPrimitive for the TextBuffer change.  On an undo, the TextBuffer changed UndoPrimitive
            // will fire it's Undo first, and than the Undo for this UndoPrimitive will fire.
            // However, on a redo, the Redo for this UndoPrimitive will be fired, and then the Redo for the TextBuffer change UndoPrimitive.  If we had set any caret placement/selection here (ie the new caret placement/selection), 
            // we may crash because the TextBuffer change has not occurred yet (ie you try to set the caret to be at CharacterIndex 1 when the TextBuffer is still empty).

            _canUndo = true;
        }

        /// <summary>
        /// Undo the action.
        /// </summary>
        /// <exception cref="InvalidOperationException">Operation cannot be undone.</exception>
        public override void Undo()
        {
            // Validate that we can undo this change
            if (!CanUndo)
            {
                throw new InvalidOperationException(Strings.CannotUndo);
            }

            // Restore the old caret position and active selection
            var view = this.GetTextView();
            Debug.Assert(view == null || !view.IsClosed, "Attempt to undo/redo on a closed view?  This shouldn't happen.");
            if (view != null && !view.IsClosed)
            {
                this.State.Restore(view);
                view.Caret.EnsureVisible();
            }

            _canUndo = false;
        }

        public override bool CanMerge(ITextUndoPrimitive older)
        {
            if (older == null)
            {
                throw new ArgumentNullException(nameof(older));
            }

            AfterTextBufferChangeUndoPrimitive olderPrimitive = older as AfterTextBufferChangeUndoPrimitive;
            // We can only merge with IUndoPrimitives of AfterTextBufferChangeUndoPrimitive type
            if (olderPrimitive == null)
            {
                return false;
            }

            return olderPrimitive.State.Matches(this.State);
        }

        #endregion
    }
}
