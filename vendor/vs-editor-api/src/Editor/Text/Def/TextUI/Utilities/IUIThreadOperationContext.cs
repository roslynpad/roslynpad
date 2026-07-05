using System;
using System.Collections.Generic;
using System.Threading;

namespace Microsoft.VisualStudio.Utilities
{
    /// <summary>
    /// Represents a context of executing potentially long running operation on the UI thread, which
    /// enables shared two way cancellability and wait indication.
    /// </summary>
    /// <remarks>
    /// Instances implementing this interface are produced by <see cref="IUIThreadOperationExecutor"/>
    /// MEF component.
    /// </remarks>
    public interface IUIThreadOperationContext : IPropertyOwner, IDisposable
    {
        /// <summary>
        /// Cancellation token that allows user to cancel the operation unless the operation
        /// is not cancellable.
        /// </summary>
        CancellationToken UserCancellationToken { get; }

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
        bool AllowCancellation { get; }

        /// <summary>
        /// Gets user readable operation description, composed of initial context description and
        /// descriptions of all currently added scopes.
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Gets current list of <see cref="IUIThreadOperationScope"/>s in this context.
        /// </summary>
        IEnumerable<IUIThreadOperationScope> Scopes { get; }

        /// <summary>
        /// Adds a UI thread operation scope with its own two way cancellability, description and progress tracker.
        /// The scope is removed from the context on dispose.
        /// </summary>
        IUIThreadOperationScope AddScope(bool allowCancellation, string description);

        /// <summary>
        /// Allows a component to take full ownership over this UI thread operation, for example
        /// when it shows its own modal UI dialog and handles cancellability through that dialog instead.
        /// </summary>
        void TakeOwnership();
    }
}
