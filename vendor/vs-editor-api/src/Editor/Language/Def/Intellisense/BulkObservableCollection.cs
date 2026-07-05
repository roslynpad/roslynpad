////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Avalonia.Threading;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Represents an ObservableCollection that allows the AddRange operation.
    /// </summary>
    public class BulkObservableCollection<T> : ObservableCollection<T>
    {
        private int _rangeOperationCount = 0;
        private bool _collectionChangedDuringRangeOperation = false;
        private Dispatcher _dispatcher;
        private ReadOnlyObservableCollection<T> _readOnlyAccessor;

        private static readonly PropertyChangedEventArgs _countChanged = new PropertyChangedEventArgs("Count");
        private static readonly PropertyChangedEventArgs _indexerChanged = new PropertyChangedEventArgs("Item[]");
        private static readonly NotifyCollectionChangedEventArgs _resetChange = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);

        /// <summary>
        /// Initializes a new instance of a <see cref="BulkObservableCollection&lt;T&gt;"/>.
        /// </summary>
        public BulkObservableCollection()
        {
            // Morgania: Avalonia has a single UI-thread dispatcher rather than WPF's
            // per-thread dispatchers; collections are affinitized to the UI thread.
            _dispatcher = Dispatcher.UIThread;
        }

        /// <summary>
        /// Adds a list of items to the ObservableCollection without firing an event for each item.
        /// </summary>
        /// <param name="items">A list of items to add.</param>
        public void AddRange(IEnumerable<T> items)
        {
            // If we were given null, nothing to do.
            if ((items == null) || !items.Any())
            {
                return;
            }

            if (_dispatcher.CheckAccess())
            {
                try
                {
                    this.BeginBulkOperation();
                    _collectionChangedDuringRangeOperation = true;

                    foreach (T item in items)
                    {
                        // Call down to the underlying collection to ensure that no collection changed event is generated (we've marked ourselves dirty above so a reset event will be raised at the end of the bulk operation).
                        base.Items.Add(item);
                    }
                }
                finally
                {
                    this.EndBulkOperation();
                }
            }
            else
            {
                _dispatcher.Post(() => this.AddRange(items), DispatcherPriority.Send);
            }
        }

        /// <summary>
        /// Suspends change events on the collection in order to perform a bulk change operation.
        /// </summary>
        public void BeginBulkOperation()
        {
            _rangeOperationCount++;
            _collectionChangedDuringRangeOperation = false;
        }

        /// <summary>
        /// Restores change events on the collection after a bulk change operation has been completed.
        /// </summary>
        public void EndBulkOperation()
        {
            if ((_rangeOperationCount > 0) && (--_rangeOperationCount == 0) && _collectionChangedDuringRangeOperation)
            {
                // Assume that, as a result of a change, the count & indexer changes (which are the only two properties that could change).
                this.OnPropertyChanged(_countChanged);
                this.OnPropertyChanged(_indexerChanged);

                this.OnCollectionChanged(_resetChange);
            }
        }

        /// <summary>
        /// Gets a read-only version of the collection.
        /// </summary>
        /// <returns>A read-only version of the collection.</returns>
        public ReadOnlyObservableCollection<T> AsReadOnly()
        {
            if (_readOnlyAccessor == null)
            {
                _readOnlyAccessor = new ReadOnlyObservableCollection<T>(this);
            }

            return _readOnlyAccessor;
        }

        /// <summary>
        /// Occurs when a property on the collection has changed.
        /// </summary>
        /// <param name="e">The event arguments.</param>
        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if (_rangeOperationCount == 0)
            {
                base.OnPropertyChanged(e);
            }
            else
            {
                _collectionChangedDuringRangeOperation = true;
            }
        }

        /// <summary>
        /// Occurs when the collection has changed.
        /// </summary>
        /// <param name="e">The event arguments.</param>
        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (_rangeOperationCount == 0)
            {
                base.OnCollectionChanged(e);
            }
            else
            {
                _collectionChangedDuringRangeOperation = true;
            }
        }

        /// <summary>
        /// Replaces the item at the specified index.
        /// </summary>
        /// <param name="index">The place at which to replace the item.</param>
        /// <param name="item">The item to replace.</param>
        protected override void SetItem(int index, T item)
        {
            if (_dispatcher.CheckAccess())
            {
                base.SetItem(index, item);
            }
            else
            {
                _dispatcher.Post(() => this.SetItem(index, item), DispatcherPriority.Send);
            }
        }

        /// <summary>
        /// Inserts an item at the specified index.
        /// </summary>
        /// <param name="index">The location at which to insert the item.</param>
        /// <param name="item">The item to insert.</param>
        protected override void InsertItem(int index, T item)
        {
            if (_dispatcher.CheckAccess())
            {
                // Dev12 #619282. During range operation avoid calling base.InsertItem() because it would allocate
                // NotifyCollectionChangedEventArgs object for each item, but this.OnCollectionChanged would not fire
                // the event during range operation.
                if (_rangeOperationCount == 0)
                {
                    base.InsertItem(index, item);
                }
                else
                {
                    base.Items.Insert(index, item);
                    _collectionChangedDuringRangeOperation = true;
                }
            }
            else
            {
                _dispatcher.Post(() => this.InsertItem(index, item), DispatcherPriority.Send);
            }
        }

        /// <summary>
        /// Moves the item from one location to another.
        /// </summary>
        /// <param name="oldIndex">The original location.</param>
        /// <param name="newIndex">The new location.</param>
        protected override void MoveItem(int oldIndex, int newIndex)
        {
            if (_dispatcher.CheckAccess())
            {
                base.MoveItem(oldIndex, newIndex);
            }
            else
            {
                _dispatcher.Post(() => this.MoveItem(oldIndex, newIndex), DispatcherPriority.Send);
            }
        }

        /// <summary>
        /// Removes an item from the collection at the specified location.
        /// </summary>
        /// <param name="index">The location at which to remove the item.</param>
        protected override void RemoveItem(int index)
        {
            if (_dispatcher.CheckAccess())
            {
                base.RemoveItem(index);
            }
            else
            {
                _dispatcher.Post(() => this.RemoveItem(index), DispatcherPriority.Send);
            }
        }

        /// <summary>
        /// Removes all items from the collection.
        /// </summary>
        protected override void ClearItems()
        {
            if (_dispatcher.CheckAccess())
            {
                if (this.Count > 0)
                {
                    base.ClearItems();
                }
            }
            else
            {
                _dispatcher.Post(this.ClearItems, DispatcherPriority.Send);
            }
        }
    }
}
