//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//

using System;

namespace Microsoft.VisualStudio.Utilities
{
    /// <summary>
    /// Operations that guard calls to extensions code, track performance and log errors.
    /// This interface contains method signatures which will be moved to <see cref="IGuardedOperations" />
    /// in future releases of Visual Studio. Microsoft reserves the right to modify this interface.
    /// </summary>
    /// <remarks>This class supports the Visual Studio 
    /// infrastructure and in general is not intended to be used directly from your code.</remarks>
    public interface IGuardedOperationsInternal : IGuardedOperations
    {
        /// <summary>
        /// Makes a guarded call to an extension point.
        /// </summary>
        /// <param name="errorSource">Reference to the extension object or event handler that may throw an exception.
        /// Used for tracking performance and errors.</param>
        /// <param name="call">Delegate that calls the extension point.</param>
        /// <param name="valueOnThrow">The value returned if the delegate call failed.</param>
        /// <param name="exceptionToIgnore">Determines which exceptions should be ignored. This predicate is evaluated first</param>
        /// <param name="exceptionToHandle">Determines which exceptions should be logged. This predicate is evaluated second.
        /// If both predicates return <c>false</c>, then the exceptions remains unhandled and may be caught in the calling code.</param>
        /// <returns>The result of the <paramref name="call"/> or <paramref name="valueOnThrow"/>.</returns>
        /// <remarks>This class supports the Visual Studio 
        /// infrastructure and in general is not intended to be used directly from your code.</remarks>
        /// <example>
        /// The following code will synchronously call <c>extension.GetData()</c>.
        /// exceptionToIgnore predicate prevents logging the <c>OperationCanceledException</c> when relevant cancellation token is canceled.
        /// exceptionToHandle predicate causes all other exceptions to be logged.
        /// If both predicates returned false, the exception would remain unhandled and may be caught in the calling code.
        /// <code>
        /// var result = GuardedOperations.CallExtensionPoint(
        ///                  errorSource: extension,
        ///                  call: () => extension.GetData(token),
        ///                  valueOnThrow: string.Empty,
        ///                  exceptionToIgnore: (e) => e is OperationCanceledException && token.IsCancellationRequested,
        ///                  exceptionToHandle: (e) => true);
        /// </code>
        /// </example>
        T CallExtensionPoint<T>(object errorSource, Func<T> call, T valueOnThrow, Predicate<Exception> exceptionToIgnore, Predicate<Exception> exceptionToHandle);

    }
}
