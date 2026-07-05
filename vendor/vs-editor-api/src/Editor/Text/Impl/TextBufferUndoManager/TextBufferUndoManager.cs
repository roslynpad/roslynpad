//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
namespace Microsoft.VisualStudio.Text.BufferUndoManager.Implementation
{
    using System;
    using System.Diagnostics;
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Operations;

    internal sealed class TextBufferUndoManager : ITextBufferUndoManager, IDisposable
    {
        #region Private Members

        private ITextBuffer _textBuffer;
        private readonly ITextUndoHistoryRegistry _undoHistoryRegistry;
        private ITextUndoHistory _undoHistory;

        // The plan had been to add the IUndoMetadataEditTag to allow people to create simple edits
        // that would restore carets. That is being pushed back to 16.0 (maybe) but I didn't want to
        // abandon the work in progress.
#if false
        private readonly IEditorOperationsFactoryService _editorOperationsFactoryService;

        IEditorOperations _initiatingOperations = null;
#endif
        ITextUndoTransaction _createdTransaction = null;
#endregion

        public TextBufferUndoManager(ITextBuffer textBuffer, ITextUndoHistoryRegistry undoHistoryRegistry)
        {
            if (textBuffer == null)
            {
                throw new ArgumentNullException(nameof(textBuffer));
            }

            if (undoHistoryRegistry == null)
            {
                throw new ArgumentNullException(nameof(undoHistoryRegistry));
            }

            _textBuffer = textBuffer;
            _undoHistoryRegistry = undoHistoryRegistry;

#if false
            if (editorOperationsFactoryService == null)
            {
                throw new ArgumentNullException(nameof(editorOperationsFactoryService));
            }

            _editorOperationsFactoryService = editorOperationsFactoryService;
#endif

            // Register the undo history
            this.EnsureTextBufferUndoHistory();

            // Listen for the buffer changed events so that we can make them undo/redo-able
            _textBuffer.Changing += TextBufferChanging;
            _textBuffer.Changed += TextBufferChanged;
            _textBuffer.PostChanged += TextBufferPostChanged;
        }

#region Private Methods

        private void TextBufferChanged(object sender, TextContentChangedEventArgs e)
        {
            if (!(e.EditTag is IUndoEditTag))
            {
                if (this.TextBufferUndoHistory.State != TextUndoHistoryState.Idle)
                {
                    Debug.Fail("We are doing a normal edit in a non-idle undo state. This is explicitly prohibited as it would corrupt the undo stack!  Please fix your code.");
                }
                else
                {
                    // With projection, we sometimes get Changed events with no changes, or for "" -> "".
                    // We don't want to create undo actions for these.
                    bool nonNullChange = false;
                    foreach (ITextChange c in e.BeforeVersion.Changes)
                    {
                        if (c.OldLength != 0 || c.NewLength != 0)
                        {
                            nonNullChange = true;
                            break;
                        }
                    }

                    if (nonNullChange)
                    {
                        // If there's an open undo transaction, add our edit (turned into a primitive) to it. Otherwise, create and undo transaction.
                        var currentTransaction = _undoHistory.CurrentTransaction;
                        if (currentTransaction == null)
                        {
                            // TODO remove this
                            // Hack to allow Cascade's local undo to light up if using v15.7 but behave using the old -- non-local -- undo before if running on 15.6.
                            // Cascade should really be marking its edits with IInvisibleEditTag (and will once it can take a hard requirement of VS 15.7).
                            if ((e.EditTag is IInvisibleEditTag) || ((e.EditTag != null) && (string.Equals(e.EditTag.ToString(), "CascadeRemoteEdit", StringComparison.Ordinal))))
                            {
                                _createdTransaction = ((ITextUndoHistory2)_undoHistory).CreateInvisibleTransaction("<invisible>");
                            }
#if false
                            else if (e.EditTag is IUndoMetadataEditTag metadata)
                            {
                                _createdTransaction = _undoHistory.CreateTransaction(metadata.Description);
                                if (_initiatingOperations == null)
                                {
                                    var view = metadata.InitiatingView;
                                    if (view != null)
                                    {
                                        _initiatingOperations = _editorOperationsFactoryService.GetEditorOperations(view);
                                        _initiatingOperations.AddBeforeTextBufferChangePrimitive();
                                    }
                                }
                            }
#endif
                            else
                            {
                                _createdTransaction = _undoHistory.CreateTransaction(Strings.TextBufferChanged);
                            }

                            currentTransaction = _createdTransaction;
                        }

                        currentTransaction.AddUndo(new TextBufferChangeUndoPrimitive(_undoHistory, e.BeforeVersion));
                    }
                }
            }
        }

        void TextBufferChanging(object sender, TextContentChangingEventArgs e)
        {
            // Note that VB explicitly forces undo edits to happen while the history is idle so we need to allow this here
            // by always doing nothing for undo edits). This may be a bug in our code (e.g. not properly cleaning up when
            // an undo transaction is cancelled in mid-flight) but changing that will require coordination with Roslyn.
            if (!(e.EditTag is IUndoEditTag))
            {
                if (this.TextBufferUndoHistory.State != TextUndoHistoryState.Idle)
                {
                    Debug.Fail("We are doing a normal edit in a non-idle undo state. This is explicitly prohibited as it would corrupt the undo stack!  Please fix your code.");
                    e.Cancel();
                }
            }
        }

        private void TextBufferPostChanged(object sender, EventArgs e)
        {
            if (_createdTransaction != null)
            {
#if false
                if (_initiatingOperations != null)
                {
                    _initiatingOperations.AddAfterTextBufferChangePrimitive();
                }

                _initiatingOperations = null;
#endif

                _createdTransaction.Complete();
                _createdTransaction.Dispose();
                _createdTransaction = null;
            }
        }

#endregion

#region ITextBufferUndoManager Members

        public ITextBuffer TextBuffer
        {
            get { return _textBuffer; }
        }

        public ITextUndoHistory TextBufferUndoHistory
        {
            // Note, right now, there is no way for us to know if an ITextUndoHistory
            // has been unregistered (ie it can be unregistered by a third party)
            // An issue has been logged with the Undo team, but in the mean time, to ensure that
            // we are robust, always register the undo history.
            get
            {
                this.EnsureTextBufferUndoHistory();
                return _undoHistory;
            }
        }

        public void UnregisterUndoHistory()
        {
            // Unregister the undo history
            if (_undoHistory != null)
            {
                _undoHistoryRegistry.RemoveHistory(_undoHistory);
                _undoHistory = null;
            }
        }

#endregion

        private void EnsureTextBufferUndoHistory()
        {
            if (_textBuffer == null)
                throw new ObjectDisposedException("TextBufferUndoManager");

            // Note, right now, there is no way for us to know if an ITextUndoHistory
            // has been unregistered (ie it can be unregistered by a third party)
            // An issue has been logged with the Undo team, but in the mean time, to ensure that
            // we are robust, always register the undo history.
            _undoHistory = _undoHistoryRegistry.RegisterHistory(_textBuffer);
        }

#region IDisposable Members

        public void Dispose()
        {
            UnregisterUndoHistory();

            if (_textBuffer != null)
            {
                _textBuffer.PostChanged -= TextBufferPostChanged;
                _textBuffer.Changed -= TextBufferChanged;
                _textBuffer.Changing -= TextBufferChanging;
                _textBuffer = null;
            }

            GC.SuppressFinalize(this);
        }

#endregion
    }
}
