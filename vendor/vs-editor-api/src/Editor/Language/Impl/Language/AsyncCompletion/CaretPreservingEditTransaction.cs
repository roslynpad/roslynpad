// Taken from Roslyn's CaretPreservingEditTransaction.cs

using System;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;

namespace Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Implementation
{
    internal class CaretPreservingEditTransaction : IDisposable
    {
        private readonly IEditorOperations _editorOperations;
        private readonly ITextUndoHistory _undoHistory;
        private ITextUndoTransaction _transaction;
        private bool _active;

        public CaretPreservingEditTransaction(
            string description,
            ITextView textView,
            ITextUndoHistoryRegistry undoHistoryRegistry,
            IEditorOperationsFactoryService editorOperationsFactoryService)
        {
            _editorOperations = editorOperationsFactoryService.GetEditorOperations(textView);
            _undoHistory = undoHistoryRegistry.GetHistory(textView.TextBuffer);
            _active = true;

            if (_undoHistory != null)
            {
                _transaction = new HACK_TextUndoTransactionThatRollsBackProperly(_undoHistory.CreateTransaction(description));
                _editorOperations.AddBeforeTextBufferChangePrimitive();
            }
        }

        public static CaretPreservingEditTransaction TryCreate(string description,
            ITextView textView,
            ITextUndoHistoryRegistry undoHistoryRegistry,
            IEditorOperationsFactoryService editorOperationsFactoryService)
        {
            if (undoHistoryRegistry.TryGetHistory(textView.TextBuffer, out var unused))
            {
                return new CaretPreservingEditTransaction(description, textView, undoHistoryRegistry, editorOperationsFactoryService);
            }

            return null;
        }

        public void Complete()
        {
            if (!_active)
            {
                throw new InvalidOperationException("This transaction is already compelte");
            }

            _editorOperations.AddAfterTextBufferChangePrimitive();
            if (_transaction != null)
            {
                _transaction.Complete();
            }

            EndTransaction();
        }

        public void Cancel()
        {
            if (!_active)
            {
                throw new InvalidOperationException("This transaction is already compelte");
            }

            if (_transaction != null)
            {
                _transaction.Cancel();
            }

            EndTransaction();
        }

#pragma warning disable CA1063 // Dispose pattern
        public void Dispose()
        {
            if (_transaction != null)
            {
                // If the transaction is still pending, we'll cancel it
                Cancel();
            }
        }
#pragma warning restore CA1063

        public IMergeTextUndoTransactionPolicy MergePolicy
        {
            get
            {
                return _transaction != null ? _transaction.MergePolicy : null;
            }

            set
            {
                if (_transaction != null)
                {
                    _transaction.MergePolicy = value;
                }
            }
        }

        private void EndTransaction()
        {
            if (_transaction != null)
            {
                _transaction.Dispose();
                _transaction = null;
            }

            _active = false;
        }
    }
}
