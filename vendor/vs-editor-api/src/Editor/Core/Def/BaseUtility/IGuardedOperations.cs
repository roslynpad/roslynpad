//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.Utilities
{
    /// <summary>
    /// Operations that guard calls to extensions code, track performance and log errors.
    /// </summary>
    /// <remarks>This class supports the Visual Studio 
    /// infrastructure and in general is not intended to be used directly from your code.</remarks>
    public interface IGuardedOperations
    {
        /// <summary>
        /// Makes a guarded call to an extension point.
        /// </summary>
        /// <param name="call">Delegate that calls the extension point.</param>
        /// <remarks>This class supports the Visual Studio 
        /// infrastructure and in general is not intended to be used directly from your code.</remarks>
        void CallExtensionPoint(Action call);

        /// <summary>
        /// Makes a guarded call to an extension point.
        /// </summary>
        /// <param name="errorSource">Reference to the extension object or event handler that may throw an exception.
        /// Used for tracking performance and errors.</param>
        /// <param name="call">Delegate that calls the extension point.</param>
        /// <remarks>This class supports the Visual Studio 
        /// infrastructure and in general is not intended to be used directly from your code.</remarks>
        void CallExtensionPoint(object errorSource, Action call);

        /// <summary>
        /// Makes a guarded call to an extension point.
        /// </summary>
        /// <param name="errorSource">Reference to the extension object or event handler that may throw an exception.
        /// Used for tracking performance and errors.</param>
        /// <param name="call">Delegate that calls the extension point.</param>
        /// <param name="exceptionGuardFilter">Determines which exceptions should be guarded against. 
        /// An exception gets handled only if <paramref name="exceptionGuardFilter"/> returns <c>true</c>.</param>
        /// <remarks>This class supports the Visual Studio 
        /// infrastructure and in general is not intended to be used directly from your code.</remarks>
        void CallExtensionPoint(object errorSource, Action call, Predicate<Exception> exceptionGuardFilter);

        /// <summary>
        /// Makes a guarded call to an extension point.
        /// </summary>
        /// <param name="call">Delegate that calls the extension point.</param>
        /// <param name="valueOnThrow">The value returned if the delegate call failed.</param>
        /// <returns>The result of the <paramref name="call"/> or <paramref name="valueOnThrow"/>.</returns>
        /// <remarks>This class supports the Visual Studio 
        /// infrastructure and in general is not intended to be used directly from your code.</remarks>
        T CallExtensionPoint<T>(Func<T> call, T valueOnThrow);

        /// <summary>
        /// Makes a guarded call to an extension point.
        /// </summary>
        /// <param name="errorSource">Reference to the extension object or event handler that may throw an exception.
        /// Used for tracking performance and errors.</param>
        /// <param name="call">Delegate that calls the extension point.</param>
        /// <param name="valueOnThrow">The value returned if the delegate call failed.</param>
        /// <returns>The result of the <paramref name="call"/> or <paramref name="valueOnThrow"/>.</returns>
        /// <remarks>This class supports the Visual Studio 
        /// infrastructure and in general is not intended to be used directly from your code.</remarks>
        T CallExtensionPoint<T>(object errorSource, Func<T> call, T valueOnThrow);

        /// <summary>
        /// Makes a guarded call to an async extension point.
        /// </summary>
        /// <param name="asyncCall">Delegate that calls the extension point.</param>
        /// <returns>A <see cref="Task"/> that asynchronously executes the <paramref name="asyncAction"/>.</returns>
        /// <remarks>This class supports the Visual Studio 
        /// infrastructure and in general is not intended to be used directly from your code.</remarks>
        Task CallExtensionPointAsync(Func<Task> asyncAction);

        /// <summary>
        /// Makes a guarded call to an async extension point.
        /// </summary>
        /// <param name="errorSource">Reference to the extension object or event handler that may throw an exception.
        /// Used for tracking performance and errors.</param>
        /// <param name="asyncCall">Delegate that calls the extension point.</param>
        /// <returns>A <see cref="Task"/> that asynchronously executes the <paramref name="asyncAction"/>.</returns>
        /// <remarks>This class supports the Visual Studio 
        /// infrastructure and in general is not intended to be used directly from your code.</remarks>
        Task CallExtensionPointAsync(object errorSource, Func<Task> asyncAction);

        /// <summary>
        /// Makes a guarded call to an async extension point.
        /// </summary>
        /// <typeparam name="T">The type of the value returned from the <paramref name="asyncCall"/>.</typeparam>
        /// <param name="asyncCall">Delegate that calls the extension point.</param>
        /// <param name="valueOnThrow">The value returned if the delegate call failed.</param>
        /// <returns>A <see cref="Task{T}"/> that asynchronously executes the <paramref name="asyncCall"/> or provides <paramref name="valueOnThrow"/>.</returns>
        /// <remarks>This class supports the Visual Studio 
        /// infrastructure and in general is not intended to be used directly from your code.</remarks>
        Task<T> CallExtensionPointAsync<T>(Func<Task<T>> asyncCall, T valueOnThrow);

        /// <summary>
        /// Makes a guarded call to an async extension point.
        /// </summary>
        /// <typeparam name="T">The type of the value returned from the <paramref name="asyncCall"/>.</typeparam>
        /// <param name="errorSource">Reference to the extension object or event handler that may throw an exception.
        /// Used for tracking performance and errors.</param>
        /// <param name="asyncCall">Delegate that calls the extension point.</param>
        /// <param name="valueOnThrow">The value returned if the delegate call failed.</param>
        /// <returns>A <see cref="Task{T}"/> that asynchronously executes the <paramref name="asyncCall"/> or provides <paramref name="valueOnThrow"/>.</returns>
        /// <remarks>This class supports the Visual Studio 
        /// infrastructure and in general is not intended to be used directly from your code.</remarks>
        Task<T> CallExtensionPointAsync<T>(object errorSource, Func<Task<T>> asyncCall, T valueOnThrow);

        /// <summary>
        /// Selects extension factories whose declared content type metadata
        /// matches the provided target content type, taking into account that extension factory
        /// may be disabled by a Replace attribute on another factory.
        /// </summary>
        /// <param name="lazyFactories">Lazy references that will be evaluated.</param>
        /// <param name="dataContentType">Target content type.</param>
        /// <param name="contentTypeRegistryService">Instance of <see cref="IContentTypeRegistryService"/> which orders content types.</param>
        /// <remarks>This class supports the Visual Studio 
        /// infrastructure and in general is not intended to be used directly from your code.</remarks>
        IEnumerable<Lazy<TExtensionFactory, TMetadataView>> FindEligibleFactories<TExtensionFactory, TMetadataView>(IEnumerable<Lazy<TExtensionFactory, TMetadataView>> lazyFactories, IContentType dataContentType, IContentTypeRegistryService contentTypeRegistryService)
            where TExtensionFactory : class
            where TMetadataView : INamedContentTypeMetadata;

        /// <summary>
        /// Handles an exception occured in a call to an extension point.
        /// </summary>
        /// <param name="errorSource">Reference to the extension object or event handler that threw the exception</param>
        /// <param name="e">Exception to handle</param>
        /// <remarks>This class supports the Visual Studio 
        /// infrastructure and in general is not intended to be used directly from your code.
        /// In Visual Studio, this method logs the exception to ActivityLogs and the telemetry, and displays an error message to the user if possible.
        /// This method can be invoked from any thread.</remarks>
        void HandleException(object errorSource, Exception e);

        /// <summary>
        /// Safely instantiates an extension point.
        /// </summary>
        /// <param name="errorSource">Reference to the object that will be blamed for potential exceptions.</param>
        /// <param name="provider">Lazy reference that will be initialized.</param>
        /// <returns>Initialized instance stored in <paramref name="provider"/>.</returns>
        /// <remarks>This class supports the Visual Studio 
        /// infrastructure and in general is not intended to be used directly from your code.</remarks>
        TExtension InstantiateExtension<TExtension>(object errorSource, Lazy<TExtension> provider);

        /// <summary>
        /// Safely instantiates an extension point.
        /// </summary>
        /// <param name="errorSource">Reference to the object that will be blamed for potential exceptions.</param>
        /// <param name="provider">Lazy reference that will be initialized.</param>
        /// <returns>Initialized instance stored in <paramref name="provider"/>.</returns>
        /// <remarks>This class supports the Visual Studio 
        /// infrastructure and in general is not intended to be used directly from your code.</remarks>
        TExtension InstantiateExtension<TExtension, TMetadata>(object errorSource, Lazy<TExtension, TMetadata> provider);

        /// <summary>
        /// Safely invokes a delegate on the extension point.
        /// </summary>
        /// <param name="errorSource">Reference to the object that will be blamed for potential exceptions.</param>
        /// <param name="provider">Lazy reference that will be initialized.</param>
        /// <param name="getter">Delegate which constructs an instance of the extension from its <paramref name="provider"/>.</param>
        /// <returns>The result of <paramref name="getter"/>.</returns>
        /// <remarks>This class supports the Visual Studio 
        /// infrastructure and in general is not intended to be used directly from your code.</remarks>
        TExtensionInstance InstantiateExtension<TExtension, TMetadata, TExtensionInstance>(object errorSource, Lazy<TExtension, TMetadata> provider, Func<TExtension, TExtensionInstance> getter);

        /// <summary>
        /// Safely instantiates an extension point whose declared content type metadata
        /// is the closest match to the provided target content type.
        /// </summary>
        /// <param name="providerHandles">Lazy references that will be evaluated.</param>
        /// <param name="dataContentType">Target content type.</param>
        /// <param name="contentTypeRegistryService">Instance of <see cref="IContentTypeRegistryService"/> which orders content types.</param>
        /// <param name="errorSource">Reference to the object that will be blamed for potential exceptions.</param>
        /// <returns>The selected element of <paramref name="providerHandles"/>.</returns>
        /// <remarks>This class supports the Visual Studio 
        /// infrastructure and in general is not intended to be used directly from your code.</remarks>
        TExtension InvokeBestMatchingFactory<TExtension, TMetadataView>(IList<Lazy<TExtension, TMetadataView>> providerHandles, IContentType dataContentType, IContentTypeRegistryService contentTypeRegistryService, object errorSource) where TMetadataView : IContentTypeMetadata;

        /// <summary>
        /// Safely invokes a delegate on the extension factory whose declared content type metadata
        /// is the best match to the provided target content type.
        /// </summary>
        /// <param name="providerHandles">Lazy references that will be evaluated.</param>
        /// <param name="dataContentType">Target content type.</param>
        /// <param name="getter">Delegate which constructs an instance of the extension from the best matching element of <paramref name="providerHandles"/>.</param>
        /// <param name="contentTypeRegistryService">Instance of <see cref="IContentTypeRegistryService"/> which orders content types.</param>
        /// <param name="errorSource">Reference to the object that will be blamed for potential exceptions.</param>
        /// <returns>The result of <paramref name="getter"/>.</returns>
        /// <remarks>This class supports the Visual Studio 
        /// infrastructure and in general is not intended to be used directly from your code.</remarks>
        TExtensionInstance InvokeBestMatchingFactory<TExtensionFactory, TExtensionInstance, TMetadataView>(IList<Lazy<TExtensionFactory, TMetadataView>> providerHandles, IContentType dataContentType, Func<TExtensionFactory, TExtensionInstance> getter, IContentTypeRegistryService contentTypeRegistryService, object errorSource)
            where TExtensionFactory : class
            where TMetadataView : IContentTypeMetadata;

        /// <summary>
        /// Safely invokes a delegate on all extension factories whose declared content type metadata
        /// matches the provided target content type, taking into account that extension factory
        /// may be disabled by a Replace attribute on another factory.
        /// </summary>
        /// <param name="lazyFactories">Lazy references that will be evaluated.</param>
        /// <param name="getter">Delegate which constructs an instance of the extension from each element of <paramref name="lazyFactories"/>.</param>
        /// <param name="dataContentType">Target content type.</param>
        /// <param name="contentTypeRegistryService">Instance of <see cref="IContentTypeRegistryService"/> which orders content types.</param>
        /// <param name="errorSource">Reference to the object that will be blamed for potential exceptions.</param>
        /// <returns>The list of results of <paramref name="getter"/>.</returns>
        /// <remarks>This class supports the Visual Studio 
        /// infrastructure and in general is not intended to be used directly from your code.</remarks>
        List<TExtensionInstance> InvokeEligibleFactories<TExtensionInstance, TExtensionFactory, TMetadataView>(IEnumerable<Lazy<TExtensionFactory, TMetadataView>> lazyFactories, Func<TExtensionFactory, TExtensionInstance> getter, IContentType dataContentType, IContentTypeRegistryService contentTypeRegistryService, object errorSource)
            where TExtensionInstance : class
            where TExtensionFactory : class
            where TMetadataView : INamedContentTypeMetadata;

        /// <summary>
        /// Safely invokes a delegate on all extension factories whose declared content type metadata
        /// matches the provided target content type.
        /// </summary>
        /// <param name="lazyFactories">Lazy references that will be evaluated.</param>
        /// <param name="getter">Delegate which constructs an instance of the extension from each element of <paramref name="lazyFactories"/>.</param>
        /// <param name="dataContentType">Target content type.</param>
        /// <param name="errorSource">Reference to the object that will be blamed for potential exceptions.</param>
        /// <returns>The list of results of <paramref name="getter"/>.</returns>
        /// <remarks>This class supports the Visual Studio 
        /// infrastructure and in general is not intended to be used directly from your code.</remarks>
        List<TExtensionInstance> InvokeMatchingFactories<TExtensionInstance, TExtensionFactory, TMetadataView>(IEnumerable<Lazy<TExtensionFactory, TMetadataView>> lazyFactories, Func<TExtensionFactory, TExtensionInstance> getter, IContentType dataContentType, object errorSource)
            where TExtensionInstance : class
            where TExtensionFactory : class
            where TMetadataView : IContentTypeMetadata;

#pragma warning disable CA1030 // Use events where appropriate
        /// <summary>
        /// Safely raises an event with empty <see cref="EventArgs"/>.
        /// Errors are tracked per sender, performance is tracked per handler.
        /// </summary>
        /// <param name="sender">Reference to the sender of the event. Tracks errors.</param>
        /// <param name="eventHandlers">Event to raise. Each handler tracks performance.</param>
        /// <remarks>This class supports the Visual Studio 
        /// infrastructure and in general is not intended to be used directly from your code.</remarks>
        void RaiseEvent(object sender, EventHandler eventHandlers);

        /// <summary>
        /// Safely raises an event with specified <paramref name="args"/>.
        /// Errors are tracked per sender, performance is tracked per handler.
        /// </summary>
        /// <param name="sender">Reference to the sender of the event. Tracks errors.</param>
        /// <param name="eventHandlers">Event to raise. Each handler tracks performance.</param>
        /// <param name="args">Event data.</param>
        /// <remarks>This class supports the Visual Studio 
        /// infrastructure and in general is not intended to be used directly from your code.</remarks>
        void RaiseEvent<TArgs>(object sender, EventHandler<TArgs> eventHandlers, TArgs args) where TArgs : EventArgs;

        /// <summary>
        /// Safely raises an event on a background thread with specified <paramref name="args"/>.
        /// Errors are tracked per sender, performance is tracked per handler.
        /// </summary>
        /// <param name="sender">Reference to the sender of the event. Tracks errors.</param>
        /// <param name="eventHandlers">Event to raise. Each handler tracks performance.</param>
        /// <param name="args">Event data.</param>
        /// <returns>A <see cref="Task"/> that asynchronously executes the <paramref name="eventHandlers"/>.</returns>
        /// <remarks>This class supports the Visual Studio 
        /// infrastructure and in general is not intended to be used directly from your code.</remarks>
        Task RaiseEventOnBackgroundAsync<TArgs>(object sender, AsyncEventHandler<TArgs> eventHandlers, TArgs args) where TArgs : EventArgs;

        /// <summary>
        /// Safely attempts to cast the given object to the given type.
        /// </summary>
        /// <typeparam name="TArgs">The type that should be casted to.</typeparam>
        /// <param name="toCast">The object that should be casted.</param>
        /// <param name="casted">Returns out the casted object or default(TArgs) if the cast failed.</param>
        /// <returns>True if successful in casting, false otherwise.</returns>
        bool TryCastToType<TArgs>(object toCast, out TArgs casted);
#pragma warning restore CA1030 // Use events where appropriate
    }
}
