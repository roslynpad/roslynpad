//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Composition;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.Threading;
    using Microsoft.VisualStudio.Utilities;

    /// <summary>
    /// Operations that guard calls to suspicious code and log errors to registered extension error handlers.
    /// </summary>
    [Export]
    [Export(typeof(IGuardedOperations))]
    [Export(typeof(IGuardedOperations2))]
    [Export(typeof(IGuardedOperationsInternal))]
    [Shared]
    public sealed class GuardedOperations : IGuardedOperations, IGuardedOperations2, IGuardedOperationsInternal
    {
        [ImportMany]
        public Lazy<IExtensionErrorHandler>[] _errorHandlerExports { get; set; } = null;

        [ImportMany]
        public Lazy<IExtensionPerformanceTracker>[] _perfTrackerExports { get; set; } = null;

        [Import]
        public JoinableTaskContext _joinableTaskContext { get; set; }

        [Import(AllowDefault = true)]
        public INonJoinableTaskTrackerInternal NonJoinableTaskTracker { get; set; } // Optional in scenarios other than in VS process.

        private FrugalList<IExtensionErrorHandler> _errorHandlers;
        private FrugalList<IExtensionPerformanceTracker> _perfTrackers;

        private static Exception LastHandledException = null;
        private static string LastHandleExceptionStackTrace = null;

        public GuardedOperations()
        {
        }

        /// <summary>
        /// For unit testing.
        /// </summary>
        internal GuardedOperations(JoinableTaskContext jtContext)
        {
            _joinableTaskContext = jtContext;
        }

        /// <summary>
        /// For unit testing.
        /// </summary>
        public GuardedOperations(params IExtensionErrorHandler[] extensionErrorHandler)
        {
            _errorHandlers = new FrugalList<IExtensionErrorHandler>(extensionErrorHandler);
            _perfTrackers = new FrugalList<IExtensionPerformanceTracker>();
        }

        internal static bool ReThrowIfNoHandlers { get; set; } // For unit testing.

        private FrugalList<IExtensionErrorHandler> ErrorHandlers
        {
            get
            {
                if (_errorHandlers == null)
                {
                    _errorHandlers = new FrugalList<IExtensionErrorHandler>();
                    if (_errorHandlerExports != null)       // can be null during unit testing
                    {
                        foreach (var export in _errorHandlerExports)
                        {
                            try
                            {
                                var handler = export.Value;
                                if (handler != null)
                                {
                                    _errorHandlers.Add(handler);
                                }
                            }
                            catch (Exception)
                            {
                                GuardedOperations.Fail("Exception instantiating error handler!");
                            }
                        }
                    }
                }
                return _errorHandlers;
            }
        }

        private FrugalList<IExtensionPerformanceTracker> PerfTrackers
        {
            get
            {
                if (_perfTrackers == null)
                {
                    _perfTrackers = new FrugalList<IExtensionPerformanceTracker>();
                    if (_perfTrackerExports != null)       // can be null during unit testing
                    {
                        for (int i = 0; i < _perfTrackerExports.Length; i++)
                        {
                            try
                            {
                                var perfTracker = _perfTrackerExports[i].Value;
                                if (perfTracker != null)
                                {
                                    _perfTrackers.Add(perfTracker);
                                }
                            }
                            catch (Exception)
                            {
                                GuardedOperations.Fail("Exception instantiating perf tracker");
                            }
                        }
                    }
                }
                return _perfTrackers;
            }
        }

        public TExtensionInstance InvokeBestMatchingFactory<TExtensionFactory, TExtensionInstance, TMetadataView>
                (IList<Lazy<TExtensionFactory, TMetadataView>> providerHandles,
                 IContentType dataContentType,
                 Func<TExtensionFactory, TExtensionInstance> getter,
                 IContentTypeRegistryService contentTypeRegistryService,
                 object errorSource)
            where TMetadataView : IContentTypeMetadata
            where TExtensionFactory : class
        {
            var factories = GetOrderedMatchingExtensions(providerHandles, dataContentType, contentTypeRegistryService);
            foreach (var factoryExport in factories)
            {
                TExtensionFactory factory = InstantiateExtension(errorSource, factoryExport);
                if (factory != null)
                {
                    TExtensionInstance extensionInstance = default(TExtensionInstance);
                    this.CallExtensionPoint(errorSource, () => extensionInstance = getter(factory));
                    if (extensionInstance != null)
                        return extensionInstance;
                }
            }

            return default(TExtensionInstance);
        }

        public TExtension InvokeBestMatchingFactory<TExtension, TMetadataView>
                (IList<Lazy<TExtension, TMetadataView>> providerHandles,
                 IContentType dataContentType,
                 IContentTypeRegistryService contentTypeRegistryService,
                 object errorSource)
            where TMetadataView : IContentTypeMetadata
        {
            var extensions = GetOrderedMatchingExtensions(providerHandles, dataContentType, contentTypeRegistryService);
            foreach (var extension in extensions)
            {
                TExtension factory = InstantiateExtension(errorSource, extension);
                if (factory != null)
                {
                    return factory;
                }
            }

            // no suitable provider found
            return default(TExtension);
        }

        /// <summary>
        /// Return a list of uninstantiated extensions sorted by the specificity of the content type (assets with more specific content types come first).
        /// </summary>
        private static IEnumerable<Lazy<TExtension, TMetadataView>> GetOrderedMatchingExtensions<TExtension, TMetadataView>
                (IList<Lazy<TExtension, TMetadataView>> providerHandles,
                 IContentType dataContentType,
                 IContentTypeRegistryService contentTypeRegistryService)
                    where TMetadataView : IContentTypeMetadata
        {
            var candidates = new List<Lazy<TExtension, TMetadataView>>();
            for (int i = 0; (i < providerHandles.Count); ++i)
            {
                var providerHandle = providerHandles[i];
                foreach (string contentTypeName in providerHandle.Metadata.ContentTypes)
                {
                    if (dataContentType.IsOfType(contentTypeName))
                    {
                        candidates.Add(providerHandle);
                        break;
                    }
                }
            }

            SortCandidates(candidates, dataContentType, contentTypeRegistryService);

            return candidates;
        }

        public List<TExtensionInstance> InvokeMatchingFactories<TExtensionInstance, TExtensionFactory, TMetadataView>
            (IEnumerable<Lazy<TExtensionFactory, TMetadataView>> lazyFactories,
             Func<TExtensionFactory, TExtensionInstance> getter,
             IContentType dataContentType,
             object errorSource)
                where TMetadataView : IContentTypeMetadata          // content type is required
                where TExtensionFactory : class
                where TExtensionInstance : class
        {
            var result = new List<TExtensionInstance>();
            foreach (var lazyFactory in lazyFactories)
            {
                if (ExtensionSelector.ContentTypeMatch(dataContentType, lazyFactory.Metadata.ContentTypes))
                {
                    try
                    {
                        TExtensionFactory factory = lazyFactory.Value;
                        if (factory != null)
                        {
                            TExtensionInstance instance = getter(factory);
                            if (instance != null)
                            {
                                result.Add(instance);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        HandleException(errorSource, e);
                    }
                }
            }
            return result;
        }

        // The algorithm here is that assets can have a Name attribute and one or more Replaces attribute.
        // Assets without names are treated normally (they are always considered eligible).
        // Named assets are considered ineligible if:
        //  There is a "better" asset with the same name (better means a more specific content type).
        //  There is another assert with a Replaces attribute that matches the name of the asset.
        public IEnumerable<Lazy<TExtensionFactory, TMetadataView>> FindEligibleFactories<TExtensionFactory, TMetadataView>
                                            (IEnumerable<Lazy<TExtensionFactory, TMetadataView>> lazyFactories,
                                            IContentType dataContentType,
                                            IContentTypeRegistryService contentTypeRegistryService)
                                                where TMetadataView : INamedContentTypeMetadata          // content type is required
                                                where TExtensionFactory : class
        {
            Dictionary<string, List<Lazy<TExtensionFactory, TMetadataView>>> namedFactories = null;
            HashSet<string> replaced = null;
            foreach (var lazyFactory in lazyFactories)
            {
                if (ExtensionSelector.ContentTypeMatch(dataContentType, lazyFactory.Metadata.ContentTypes))
                {
                    if (string.IsNullOrEmpty(lazyFactory.Metadata.Name))
                    {
                        yield return lazyFactory;
                    }
                    else
                    {
                        if (namedFactories == null)
                        {
                            namedFactories = new Dictionary<string, List<Lazy<TExtensionFactory, TMetadataView>>>(StringComparer.OrdinalIgnoreCase);
                        }

                        List<Lazy<TExtensionFactory, TMetadataView>> factories;
                        if (!namedFactories.TryGetValue(lazyFactory.Metadata.Name, out factories))
                        {
                            factories = new List<Lazy<TExtensionFactory, TMetadataView>>();
                            namedFactories.Add(lazyFactory.Metadata.Name, factories);
                        }

                        factories.Add(lazyFactory);

                        if (lazyFactory.Metadata.Replaces != null)
                        {
                            if (replaced == null)
                            {
                                replaced = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                            }

                            foreach (var s in lazyFactory.Metadata.Replaces)
                            {
                                replaced.Add(s);
                            }
                        }
                    }
                }
            }

            if (namedFactories != null)
            {
                foreach (var candidates in namedFactories.Values)
                {
                    var candidate = candidates[0];
                    if ((replaced == null) || !replaced.Contains(candidate.Metadata.Name))
                    {
                        SortCandidates(candidates, dataContentType, contentTypeRegistryService);
                        yield return candidates[0];
                    }
                }
            }
        }


        /// <summary>
        /// Given a list of factory extensions that provide content types, filter the list, instantiate that
        /// subset which matches the given content type, and invoke the factory method. Return the non-null results.
        /// </summary>
        public List<TExtensionInstance> InvokeEligibleFactories<TExtensionInstance, TExtensionFactory, TMetadataView>
                    (IEnumerable<Lazy<TExtensionFactory, TMetadataView>> lazyFactories,
                     Func<TExtensionFactory, TExtensionInstance> getter,
                     IContentType dataContentType,
                     IContentTypeRegistryService contentTypeRegistryService,
                     object errorSource)
            where TMetadataView : INamedContentTypeMetadata          // content type is required
            where TExtensionFactory : class
            where TExtensionInstance : class
        {
            var result = new List<TExtensionInstance>();
            foreach (var lazyFactory in FindEligibleFactories(lazyFactories, dataContentType, contentTypeRegistryService))
            {
                try
                {
                    TExtensionFactory factory = lazyFactory.Value;
                    if (factory != null)
                    {
                        TExtensionInstance instance = getter(factory);
                        if (instance != null)
                        {
                            result.Add(instance);
                        }
                    }
                }
                catch (Exception e)
                {
                    HandleException(errorSource, e);
                }
            }

            return result;
        }

        public TExtension InstantiateExtension<TExtension>(object errorSource, Lazy<TExtension> provider)
        {
            try
            {
                return provider.Value;
            }
            catch (Exception e)
            {
                HandleException(errorSource, e);
                return default(TExtension);
            }
        }

        public TExtension InstantiateExtension<TExtension, TMetadata>(object errorSource, Lazy<TExtension, TMetadata> provider)
        {
            try
            {
                return provider.Value;
            }
            catch (Exception e)
            {
                HandleException(errorSource, e);
                return default(TExtension);
            }
        }

        public TExtensionInstance InstantiateExtension<TExtension, TMetadata, TExtensionInstance>(
            object errorSource, Lazy<TExtension, TMetadata> provider, Func<TExtension, TExtensionInstance> getter)
        {
            try
            {
                return getter(provider.Value);
            }
            catch (Exception e)
            {
                HandleException(errorSource, e);
                return default(TExtensionInstance);
            }
        }

        public void CallExtensionPoint(object errorSource, Action call)
        {
            try
            {
                BeforeCallingExtensionPoint(errorSource ?? call);
                call();
            }
            catch (Exception e)
            {
                HandleException(errorSource, e);
            }
            finally
            {
                AfterCallingExtensionPoint(errorSource ?? call);
            }
        }

        public void CallExtensionPoint(object errorSource, Action call, Predicate<Exception> exceptionFilter)
        {
            try
            {
                BeforeCallingExtensionPoint(errorSource ?? call);
                call();
            }
            catch (Exception e) when (exceptionFilter(e))
            {
                HandleException(errorSource, e);
            }
            finally
            {
                AfterCallingExtensionPoint(errorSource ?? call);
            }
        }

        public T CallExtensionPoint<T>(object errorSource, Func<T> call, T valueOnThrow)
        {
            try
            {
                BeforeCallingExtensionPoint(errorSource ?? call);
                return call();
            }
            catch (Exception e)
            {
                HandleException(errorSource, e);

                return valueOnThrow;
            }
            finally
            {
                AfterCallingExtensionPoint(errorSource ?? call);
            }
        }

        public T CallExtensionPoint<T>(object errorSource, Func<T> call, T valueOnThrow, Predicate<Exception> exceptionToIgnore, Predicate<Exception> exceptionToHandle)
        {
            try
            {
                BeforeCallingExtensionPoint(errorSource ?? call);
                return call();
            }
            catch (Exception e) when (exceptionToIgnore(e))
            {
                return valueOnThrow;
            }
            catch (Exception e) when (exceptionToHandle(e))
            {
                HandleException(errorSource, e);
                return valueOnThrow;
            }
            finally
            {
                AfterCallingExtensionPoint(errorSource ?? call);
            }
        }

        public void CallExtensionPoint(Action call)
        {
            this.CallExtensionPoint(errorSource: null, call: call);
        }

        public T CallExtensionPoint<T>(Func<T> call, T valueOnThrow)
        {
            return this.CallExtensionPoint(errorSource: null, call: call, valueOnThrow: valueOnThrow);
        }

        public async Task CallExtensionPointAsync(object errorSource, Func<Task> asyncAction)
        {
            try
            {
                await asyncAction().ConfigureAwait(true);
            }
            catch (OperationCanceledException) { } // swallow OperationCanceledException in async method calls
            catch (Exception e)
            {
                HandleException(errorSource, e);
            }
        }

        public Task CallExtensionPointAsync(Func<Task> asyncAction) => CallExtensionPointAsync(errorSource: null, asyncAction: asyncAction);

        public async Task<T> CallExtensionPointAsync<T>(object errorSource, Func<Task<T>> asyncCall, T valueOnThrow)
        {
            try
            {
                return await asyncCall().ConfigureAwait(true);
            }
            catch (OperationCanceledException)
            {
                // swallow OperationCanceledException in async method calls
                return valueOnThrow;
            }
            catch (Exception e)
            {
                HandleException(errorSource, e);
                return valueOnThrow;
            }
        }

        public Task<T> CallExtensionPointAsync<T>(Func<Task<T>> asyncCall, T valueOnThrow)
            => CallExtensionPointAsync<T>(errorSource: null, asyncCall: asyncCall, valueOnThrow: valueOnThrow);

        public void RaiseEvent(object sender, EventHandler eventHandlers)
        {
            if (eventHandlers == null)
            {
                return;
            }

            var handlers = eventHandlers.GetInvocationList();

            for (int i = 0; (i < handlers.Length); ++i)
            {
                var handler = (EventHandler)(handlers[i]);
                try
                {
                    BeforeCallingEventHandler(handler);
                    handler(sender, EventArgs.Empty);
                }
                catch (Exception e)
                {
                    HandleException(sender, e);
                }
                finally
                {
                    AfterCallingEventHandler(handler);
                }
            }
        }

        public void RaiseEvent<TArgs>(object sender, EventHandler<TArgs> eventHandlers, TArgs args) where TArgs : EventArgs
        {
            if (eventHandlers == null)
            {
                return;
            }
            var handlers = eventHandlers.GetInvocationList();

            for (int i = 0; (i < handlers.Length); ++i)
            {
                var handler = (EventHandler<TArgs>)(handlers[i]);
                try
                {
                    BeforeCallingEventHandler(handler);
                    handler(sender, args);
                }
                catch (Exception e)
                {
                    HandleException(sender, e);
                }
                finally
                {
                    AfterCallingEventHandler(handler);
                }
            }
        }

        private void AfterCallingEventHandler(Delegate handler)
        {
            if (PerfTrackers.Count == 0)
            {
                return;
            }

            for (int i = 0; i < PerfTrackers.Count; i++)
            {
                var perfTracker = PerfTrackers[i];
                try
                {
                    PerfTrackers[i].AfterCallingEventHandler(handler);
                }
                catch (Exception e)
                {
                    HandleException(PerfTrackers[i], e);
                }
            }
        }

        private void AfterCallingExtensionPoint(object extensionPoint)
        {
            if (PerfTrackers.Count == 0)
            {
                return;
            }

            for (int i = 0; i < PerfTrackers.Count; i++)
            {
                var perfTracker = PerfTrackers[i];
                try
                {
                    PerfTrackers[i].AfterCallingExtension(extensionPoint);
                }
                catch (Exception e)
                {
                    HandleException(PerfTrackers[i], e);
                }
            }
        }

        private void BeforeCallingEventHandler(Delegate handler)
        {
            if (PerfTrackers.Count == 0)
            {
                return;
            }

            for (int i = 0; i < PerfTrackers.Count; i++)
            {
                var perfTracker = PerfTrackers[i];
                try
                {
                    PerfTrackers[i].BeforeCallingEventHandler(handler);
                }
                catch (Exception e)
                {
                    HandleException(PerfTrackers[i], e);
                }
            }
        }

        private void BeforeCallingExtensionPoint(object extensionPoint)
        {
            if (PerfTrackers.Count == 0)
            {
                return;
            }

            for (int i = 0; i < PerfTrackers.Count; i++)
            {
                var perfTracker = PerfTrackers[i];
                try
                {
                    PerfTrackers[i].BeforeCallingExtension(extensionPoint);
                }
                catch (Exception e)
                {
                    HandleException(PerfTrackers[i], e);
                }
            }
        }

        public void LogException(object errorSource, Exception e)
        {
            var logged = false;
            for (int i = 0; (i < ErrorHandlers.Count); ++i)
            {
                var errorHandler = ErrorHandlers[i] as IExtensionErrorHandler2;
                if (errorHandler != null)
                {
                    try
                    {
                        GuardedOperations.LastHandledException = e;
                        GuardedOperations.LastHandleExceptionStackTrace = e.StackTrace;

                        errorHandler.LogError(errorSource, e);
                        logged = true;
                    }
                    catch (Exception doubleFaultException)
                    {
                        // TODO: What is the right behavior here?
                        GuardedOperations.Fail(doubleFaultException.ToString());
                    }
                }
            }

            if (!logged)
            {
                Debug.WriteLine(e.Message);
            }
        }

        public void HandleException(object errorSource, Exception e)
        {
            bool handled = false;
            for (int i = 0; (i < ErrorHandlers.Count); ++i)
            {
                var errorHandler = ErrorHandlers[i];
                try
                {
                    GuardedOperations.LastHandledException = e;
                    GuardedOperations.LastHandleExceptionStackTrace = e.StackTrace;

                    errorHandler.HandleError(errorSource, e);
                    handled = true;
                }
                catch (Exception doubleFaultException)
                {
                    // TODO: What is the right behavior here?
                    GuardedOperations.Fail(doubleFaultException.ToString());
                }
            }
            if (!handled)
            {
                // TODO: What is the right behavior here?
                GuardedOperations.Fail(e.ToString());

                if (GuardedOperations.ReThrowIfNoHandlers)
                    throw new Exception("Unhandled exception.", e);
            }
        }

        private static void SortCandidates<TExtension, TMetadataView>(List<Lazy<TExtension, TMetadataView>> candidates, IContentType dataContentType, IContentTypeRegistryService contentTypeRegistryService)
                        where TMetadataView : IContentTypeMetadata
        {
            if (candidates.Count > 1)
            {
                var contentTypes = new List<IContentType>();
                foreach (var c in candidates)
                {
                    foreach (string contentTypeName in c.Metadata.ContentTypes)
                    {
                        if (dataContentType.IsOfType(contentTypeName))
                        {
                            var type = contentTypeRegistryService.GetContentType(contentTypeName);
                            if (!contentTypes.Contains(type))
                            {
                                contentTypes.Add(type);
                            }
                        }
                    }
                }

                contentTypes.Sort(CompareContentTypes);
                candidates.Sort((left, right) =>
                {
                    int leftIndex = BestContentTypeScore(left.Metadata.ContentTypes, contentTypes);
                    int rightIndex = BestContentTypeScore(right.Metadata.ContentTypes, contentTypes);

                    return leftIndex - rightIndex;  // Sort these in ascending order.
                });
            }
        }

        private static int BestContentTypeScore(IEnumerable<string> contentTypes, List<IContentType> sortedContentTypes)
        {
            return contentTypes.Min(s => ContentTypeScore(s, sortedContentTypes));
        }

        private static int ContentTypeScore(string contentTypeName, List<IContentType> sortedContentTypes)
        {
            for (int i = 0; (i < sortedContentTypes.Count); ++i)
            {
                if (string.Compare(sortedContentTypes[i].TypeName, contentTypeName, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    return i;
                }
            }

            return sortedContentTypes.Count;
        }

        private static int CompareContentTypes(IContentType left, IContentType right)
        {
            if (left == right)
            {
                return 0;
            }
            else
            {
                if (left.IsOfType(right.TypeName))
                {
                    return -1;
                }
                else if (right.IsOfType(left.TypeName))
                {
                    return +1;
                }
                else
                {
                    // the content types are unrelated, use alpha order of their names
                    return string.Compare(left.TypeName, right.TypeName, StringComparison.OrdinalIgnoreCase);
                }
            }
        }

        public Task RaiseEventOnBackgroundAsync<TArgs>(object sender, AsyncEventHandler<TArgs> eventHandlers, TArgs args) where TArgs : EventArgs
        {
            return _joinableTaskContext.Factory.RunAsync(async () =>
            {
                await TaskScheduler.Default;

                if (eventHandlers == null)
                {
                    return;
                }

                var handlers = eventHandlers.GetInvocationList();

                for (int i = 0; (i < handlers.Length); ++i)
                {
                    var handler = (AsyncEventHandler<TArgs>)(handlers[i]);
                    try
                    {
                        BeforeCallingEventHandler(handler);
                        await handler(sender, args).ConfigureAwait(true);
                    }
                    catch (Exception e)
                    {
                        HandleException(sender, e);
                    }
                    finally
                    {
                        AfterCallingEventHandler(handler);
                    }
                }
            }).Task;
        }

        internal static bool IgnoreFailures = false;
        internal static bool BreakOnFailures = true;

        public bool TryCastToType<TArgs>(object toCast, out TArgs casted)
        {
            try
            {
                casted = (TArgs)toCast;
                return true;
            }
            catch (Exception ex)
            {
                HandleException(this, ex);
                casted = default(TArgs);
                return false;
            }
        }

        [Conditional("DEBUG")]
        private static void Fail(string message)
        {
            if (!IgnoreFailures)
            {
                if (BreakOnFailures && Debugger.IsAttached)
                    Debugger.Break();
                Debug.Fail(message);
            }
        }
    }
}
