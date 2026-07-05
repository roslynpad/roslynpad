using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace Microsoft.VisualStudio.Utilities
{
#pragma warning disable CA1063 // Implement IDisposable Correctly
    /// <summary>
    /// Abstract base implementation of the <see cref="IUIThreadOperationContext"/> interface.
    /// </summary>
    public abstract class AbstractUIThreadOperationContext : IUIThreadOperationContext
#pragma warning restore CA1063 // Implement IDisposable Correctly
    {
        private ImmutableList<IUIThreadOperationScope> _scopes;
        private bool _allowCancellation;
        private PropertyCollection _properties;
        private readonly string _defaultDescription;
        private int _completedItems;
        private int _totalItems;

        /// <summary>
        /// Creates a new instance of the <see cref="AbstractUIThreadOperationContext"/>.
        /// </summary>
        /// <param name="allowCancellation">Initial value of the <see cref="IUIThreadOperationContext.AllowCancellation"/>
        /// property, which can change as new scopes are added to the context.</param>
        /// <param name="defaultDescription">Default value of the <see cref="IUIThreadOperationContext.Description"/>
        /// property, which can change as new scopes are added to the context.</param>
        public AbstractUIThreadOperationContext(bool allowCancellation, string defaultDescription)
        {
            _defaultDescription = defaultDescription ?? throw new ArgumentNullException(nameof(defaultDescription));
            _allowCancellation = allowCancellation;
            _scopes = ImmutableList<IUIThreadOperationScope>.Empty;
        }

        /// <summary>
        /// Cancellation token for cancelling the operation.
        /// </summary>
        public virtual CancellationToken UserCancellationToken => CancellationToken.None;

        /// <summary>
        /// Gets whether the operation can be cancelled.
        /// </summary>
        /// <remarks>This value is composed of initial AllowCancellation value and
        /// <see cref="IUIThreadOperationScope.AllowCancellation"/> values of all currently added scopes.
        /// The value composition logic takes into acount disposed scopes too - if any of added scopes
        /// were disposed while its <see cref="IUIThreadOperationScope.AllowCancellation"/> was false,
        /// this property will stay false regardless of all other scopes' <see cref="IUIThreadOperationScope.AllowCancellation"/>
        /// values.
        /// </remarks>
        public virtual bool AllowCancellation
        {
            get
            {
                if (!_allowCancellation)
                {
                    return false;
                }

                ImmutableList<IUIThreadOperationScope> scopes = _scopes;

                if (scopes == null || scopes.Count == 0)
                {
                    return _allowCancellation;
                }

                return scopes.All((s) => s.AllowCancellation);
            }
        }

        /// <summary>
        /// Gets user readable operation description, composed of initial context description or
        /// descriptions of all currently added scopes.
        /// </summary>
        public virtual string Description
        {
            get
            {
                if (_scopes == null || _scopes.Count == 0)
                {
                    return _defaultDescription;
                }

                ImmutableList<IUIThreadOperationScope> scopes = _scopes;

                // Most common case
                if (scopes.Count == 1)
                {
                    return scopes[0].Description;
                }

                // Combine descriptions of all current scopes
                return string.Join(Environment.NewLine, scopes.Select((s) => s.Description));
            }
        }

        protected int CompletedItems => _completedItems;
        protected int TotalItems => _totalItems;

        /// <summary>
        /// Gets current list of <see cref="IUIThreadOperationScope"/>s in this context.
        /// </summary>
        public virtual IEnumerable<IUIThreadOperationScope> Scopes => _scopes;

        /// <summary>
        /// A collection of properties.
        /// </summary>
        public virtual PropertyCollection Properties
        {
            get
            {
                if (_properties == null)
                {
                    Interlocked.CompareExchange(ref _properties, new PropertyCollection(), null);
                }

                return _properties;
            }
        }

        /// <summary>
        /// Adds an UI thread operation scope with its own cancellability, description and progress tracker.
        /// The scope is removed from the context on dispose.
        /// </summary>
        public virtual IUIThreadOperationScope AddScope(bool allowCancellation, string description)
        {
            var scope = new UIThreadOperationScope(allowCancellation, description, this);

            while (true)
            {
                ImmutableList<IUIThreadOperationScope> oldScopes = _scopes;
                ImmutableList<IUIThreadOperationScope> newScopes = oldScopes == null ? ImmutableList.Create<IUIThreadOperationScope>(scope) : oldScopes.Add(scope);

                var currentScopes = Interlocked.CompareExchange(ref _scopes, newScopes, oldScopes);
                if (currentScopes == oldScopes)
                {
                    // No other thread preempted us, new scopes set successfully
                    break;
                }
            }

            this.OnScopesChanged();
            return scope;
        }

        protected virtual void OnScopeProgressChanged(IUIThreadOperationScope changedScope)
        {
            int completed = 0;
            int total = 0;

            ImmutableList<IUIThreadOperationScope> scopes = _scopes;
            if (scopes == null)
            {
                return;
            }

            foreach (UIThreadOperationScope scope in scopes)
            {
                completed += scope.CompletedItems;
                total += scope.TotalItems;
            }

            Interlocked.Exchange(ref _completedItems, completed);
            Interlocked.Exchange(ref _totalItems, total);
        }

        /// <summary>
        /// Invoked when new <see cref="IUIThreadOperationScope"/>s are added or disposed.
        /// </summary>
        protected virtual void OnScopesChanged() { }

        protected virtual void OnScopeChanged(IUIThreadOperationScope uiThreadOperationScope)
        {
        }

#pragma warning disable CA1063 // Implement IDisposable Correctly
        /// <summary>
        /// Disposes this instance.
        /// </summary>
        public virtual void Dispose()
#pragma warning restore CA1063 // Implement IDisposable Correctly
        {
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Allows a component to take full ownership over this UI thread operation, for example
        /// when it shows its own modal UI dialog and handles cancellability through that dialog instead.
        /// </summary>
        public virtual void TakeOwnership()
        {
        }

        protected virtual void OnScopeDisposed(IUIThreadOperationScope scope)
        {
            if (scope == null)
            {
                return;
            }

            _allowCancellation &= scope.AllowCancellation;

            if (_scopes == null)
            {
                return;
            }

            while (true) {
                ImmutableList<IUIThreadOperationScope> oldScopes = _scopes;
                ImmutableList<IUIThreadOperationScope> newScopes = oldScopes.Remove(scope);

                var currentScopes = Interlocked.CompareExchange(ref _scopes, newScopes, oldScopes);
                if (currentScopes == oldScopes)
                {
                    // No other thread preempted us, new scopes set successfully
                    break;
                }
            }

            OnScopesChanged();
        }

        private class UIThreadOperationScope : IUIThreadOperationScope, IProgress<ProgressInfo>
        {
            private bool _allowCancellation;
            private string _description;
            private readonly AbstractUIThreadOperationContext _context;
            private int _completedItems;
            private int _totalItems;

            public UIThreadOperationScope(bool allowCancellation, string description, AbstractUIThreadOperationContext context)
            {
                _context = context ?? throw new ArgumentNullException(nameof(context));
                this.AllowCancellation = allowCancellation;
                this.Description = description ?? "";
            }

            public bool AllowCancellation
            {
                get { return _allowCancellation; }
                set
                {
                    if (_allowCancellation != value)
                    {
                        _allowCancellation = value;
                        _context.OnScopeChanged(this);
                    }
                }
            }

            public string Description
            {
                get { return _description; }
                set
                {
                    if (!string.Equals(_description, value, StringComparison.Ordinal))
                    {
                        _description = value;
                        _context.OnScopeChanged(this);
                    }
                }
            }

            public IUIThreadOperationContext Context => _context;

            public IProgress<ProgressInfo> Progress => this;

            public int CompletedItems => _completedItems;

            public int TotalItems => _totalItems;

            public void Dispose()
            {
                _context.OnScopeDisposed(this);
            }

            void IProgress<ProgressInfo>.Report(ProgressInfo progressInfo)
            {
                Interlocked.Exchange(ref _completedItems, progressInfo.CompletedItems);
                Interlocked.Exchange(ref _totalItems, progressInfo.TotalItems);
                _context.OnScopeProgressChanged(this);
            }
        }
    }
}
