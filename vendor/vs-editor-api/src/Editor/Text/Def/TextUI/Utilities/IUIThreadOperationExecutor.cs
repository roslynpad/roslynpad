using System;

namespace Microsoft.VisualStudio.Utilities
{
    /// <summary>
    /// Executes potentially long running operation on the UI thread and provides shared two way cancellability
    /// and wait indication.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Visual Studio implementation of this service measures operation execution duration
    /// and displays a modal wait dialog if it takes too long. The wait dialog describes operation to the user,
    /// optionally provides progress information and allows user to cancel the operation if
    /// it can be cancelled.
    /// Components running in the operation can affect the wait dialog via <see cref="IUIThreadOperationContext"/>
    /// provided by this service.
    /// </para>
    /// <para>
    /// This is a MEF component and should be imported for consumptions as follows:
    /// 
    /// [Import]
    /// private IUIThreadOperationExecutor uiThreadOperationExecutor = null;
    /// </para>
    /// <para>
    /// Host specific implementations of this service should be exported as follows:
    ///
    /// [ExportImplementation(typeof(IUIThreadOperationExecutor))]
    ///  [Name("Visual Studio UI thread operation executor")]
    ///  [Order(Before = "default")]
    ///  internal sealed class VSUIThreadOperationExecutor : IUIThreadOperationExecutor
    /// </para>
    /// <para>
    /// All methods of this interface should only be called on the UI thread.
    /// </para>
    /// </remarks>
    /// <example>
    /// A typical usage of <see cref="IUIThreadOperationExecutor"/> to execute a potentially
    /// long running operation on the UI thread is as follows:
    /// 
    /// [Import]
    /// private IUIThreadOperationExecutor uiThreadOperationExecutor = null;
    /// ...
    /// UIThreadOperationStatus status = _uiThreadOperationExecutor.Execute("Format document",
    ///     "Please wait for document formatting...", allowCancel: true, showProgress: false,
    ///     action: (context) => Format(context.UserCancellationToken));
    /// if (status == UIThreadOperationStatus.Completed)...
    ///
    /// Or alternatively
    ///
    /// using (var context = _uiThreadOperationExecutor.BeginExecute("Format document",
    ///     "Please wait for document formatting...", allowCancel: true, showProgress: false))
    /// {
    ///     Format(context);
    /// }
    ///
    /// private void Format(IUIThreadOperationContext context)
    /// {
    ///     using (context.AddScope(allowCancellation: true, description: "Acquiring user preferences..."))
    ///     {...}
    /// }
    /// </example>
    public interface IUIThreadOperationExecutor
    {
        /// <summary>
        /// Executes the action synchronously and waits for it to complete.
        /// </summary>
        /// <param name="title">Operation's title. Can be null to indicate that the wait dialog should use the application's title.</param>
        /// <param name="defaultDescription">Default operation's description, which is displayed on the wait dialog unless
        /// one or more <see cref="IUIThreadOperationScope"/>s with more specific descriptions were added to
        /// the <see cref="IUIThreadOperationContext"/>.</param>
        /// <param name="allowCancellation">Whether to allow cancellability.</param>
        /// <param name="showProgress">Whether to show progress indication.</param>
        /// <param name="action">An action to execute.</param>
        /// <returns>A status of action execution.</returns>
        UIThreadOperationStatus Execute(string title, string defaultDescription, bool allowCancellation, bool showProgress,
            Action<IUIThreadOperationContext> action);

        /// <summary>
        /// Executes the action synchronously and waits for it to complete.
        /// </summary>
        /// <param name="executionOptions">Options that control action execution behavior.</param>
        /// <param name="action">An action to execute.</param>
        /// <returns>A status of action execution.</returns>
        UIThreadOperationStatus Execute(UIThreadOperationExecutionOptions executionOptions, Action<IUIThreadOperationContext> action);

        /// <summary>
        /// Begins executing potentially long running operation on the caller thread and provides a context object that provides access to shared
        /// cancellability and wait indication.
        /// </summary>
        /// <param name="title">Operation's title. Can be null to indicate that the wait dialog should use the application's title.</param>
        /// <param name="defaultDescription">Default operation's description, which is displayed on the wait dialog unless
        /// one or more <see cref="IUIThreadOperationScope"/>s with more specific descriptions were added to
        /// the <see cref="IUIThreadOperationContext"/>.</param>
        /// <param name="allowCancellation">Whether to allow cancellability.</param>
        /// <param name="showProgress">Whether to show progress indication.</param>
        /// <returns><see cref="IUIThreadOperationContext"/> instance that provides access to shared two way
        /// cancellability and wait indication for the given operation. The operation is considered executed
        /// when this <see cref="IUIThreadOperationContext"/> instance is disposed.</returns>
        IUIThreadOperationContext BeginExecute(string title, string defaultDescription, bool allowCancellation, bool showProgress);

        /// <summary>
        /// Begins executing potentially long running operation on the caller thread and provides a context object that provides access to shared
        /// cancellability and wait indication.
        /// </summary>
        /// <param name="executionOptions">Options that control execution behavior.</param>
        /// <returns><see cref="IUIThreadOperationContext"/> instance that provides access to shared two way
        /// cancellability and wait indication for the given operation. The operation is considered executed
        /// when this <see cref="IUIThreadOperationContext"/> instance is disposed.</returns>
        IUIThreadOperationContext BeginExecute(UIThreadOperationExecutionOptions executionOptions);
    }
}
