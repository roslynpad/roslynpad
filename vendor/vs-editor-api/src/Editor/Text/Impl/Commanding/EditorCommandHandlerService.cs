using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Text.Utilities;
using Microsoft.VisualStudio.Utilities;
using ICommandHandlerAndMetadata = System.Lazy<Microsoft.VisualStudio.Commanding.ICommandHandler, Microsoft.VisualStudio.UI.Text.Commanding.Implementation.CommandHandlerMetadata>;

namespace Microsoft.VisualStudio.UI.Text.Commanding.Implementation
{
    internal class EditorCommandHandlerService : IEditorCommandHandlerService
    {
        private const string TelemetryEventPrefix = "VS/Editor/Commanding";
        private const string TelemetryPropertyPrefix = "VS.Editor.Commanding";

        private readonly IEnumerable<ICommandHandlerAndMetadata> _commandHandlers;
        private readonly EditorCommandHandlerServiceFactory _factory;
        private readonly ITextView _textView;
        private readonly ICommandingTextBufferResolver _bufferResolver;
        private readonly bool _isOldGtkEditor;
        private readonly ITypingTelemetrySession _telemetrySession;

        private readonly static IReadOnlyList<ICommandHandlerAndMetadata> EmptyHandlerList = new List<ICommandHandlerAndMetadata>(0);
        private readonly static Action EmptyAction = delegate { };
        private readonly static Func<CommandState> UnavalableCommandFunc = new Func<CommandState>(() => CommandState.Unavailable);
        private readonly static string WaitForCommandExecutionString = CommandingStrings.WaitForCommandExecution;

        /// This dictionary acts as a cache so we can avoid having to look through the full list of
        /// handlers every time we need handlers of a specific type, for a given content type.
        private readonly Dictionary<(Type commandArgType, IContentType contentType), IReadOnlyList<ICommandHandlerAndMetadata>> _commandHandlersByTypeAndContentType;

        public EditorCommandHandlerService(EditorCommandHandlerServiceFactory factory,
            ITextView textView,
            IEnumerable<ICommandHandlerAndMetadata> commandHandlers,
            ICommandingTextBufferResolver bufferResolver)
        {
            _commandHandlers = commandHandlers ?? throw new ArgumentNullException(nameof(commandHandlers));
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _textView = textView ?? throw new ArgumentNullException(nameof(textView));
            _isOldGtkEditor = _textView.GetType ().FullName == "MonoDevelop.SourceEditor.ExtensibleTextEditor";
            _commandHandlersByTypeAndContentType = new Dictionary<(Type commandArgType, IContentType contentType), IReadOnlyList<ICommandHandlerAndMetadata>>();
            _bufferResolver = bufferResolver ?? throw new ArgumentNullException(nameof(bufferResolver));
            _textView.Properties.TryGetProperty(typeof(ITypingTelemetrySession), out _telemetrySession);
        }

        public CommandState GetCommandState<T>(Func<ITextView, ITextBuffer, T> argsFactory, Func<CommandState> nextCommandHandler) where T : EditorCommandArgs
        {
            if (!_factory.JoinableTaskContext.IsOnMainThread)
            {
                throw new InvalidOperationException($"{nameof(IEditorCommandHandlerService.GetCommandState)} method should only be called on the UI thread.");
            }

            // Build up chain of handlers per buffer
            Func<CommandState> handlerChain = nextCommandHandler ?? UnavalableCommandFunc;
            foreach (var bufferAndHandler in GetOrderedBuffersAndCommandHandlers<T>().Reverse())
            {
                T args = null;
                // Declare locals to ensure that we don't end up capturing the wrong thing
                var nextHandler = handlerChain;
                var handler = bufferAndHandler.handler;
                args = args ?? (args = argsFactory(_textView, bufferAndHandler.buffer));
                if (args == null)
                {
                    // Args factory failed, skip command handlers and just call next
                    return handlerChain();
                }

                handlerChain = () => handler.GetCommandState(args, nextHandler);
            }

            // Kick off the first command handler
            return handlerChain();
        }

        public void Execute<T>(Func<ITextView, ITextBuffer, T> argsFactory, Action nextCommandHandler) where T : EditorCommandArgs
        {
            if (!_factory.JoinableTaskContext.IsOnMainThread)
            {
                throw new InvalidOperationException($"{nameof(IEditorCommandHandlerService.Execute)} method should only be called on the UI thread.");
            }

            // In contained languge (Razor) scenario it's possible that EditorCommandHandlerService is called re-entrantly
            // for the same command, first by contained language command filter and then by editor command chain.
            // To preserve Razor commanding semantics, only execute handlers once for the same command.
            if (IsReentrantCall<T>())
            {
                nextCommandHandler?.Invoke();
                return;
            }

            EditorCommandHandlerServiceState state = null;

            using (var reentrancyGuard = new ReentrancyGuard<T>(_textView))
            {
                // Build up chain of handlers per buffer
                Action handlerChain = nextCommandHandler ?? EmptyAction;
                // TODO: realize the chain dynamically and without Reverse()
                foreach (var bufferAndHandler in GetOrderedBuffersAndCommandHandlers<T>().Reverse())
                {
                    T args = null;
                    // Declare locals to ensure that we don't end up capturing the wrong thing
                    var nextHandler = handlerChain;
                    var handler = bufferAndHandler.handler;
                    args = args ?? (args = argsFactory(_textView, bufferAndHandler.buffer));
                    if (args == null)
                    {
                        // Args factory failed, skip command handlers and just call next
                        handlerChain();
                        return;
                    }

                    if (handler is IDynamicCommandHandler<T> dynamicCommandHandler &&
                        !dynamicCommandHandler.CanExecuteCommand(args))
                    {
                        // Skip this one as it cannot execute the command.
                        continue;
                    }

                    if (state == null)
                    {
                        state = InitializeExecutionState(args);
                    }

                    handlerChain = () => _factory.GuardedOperations.CallExtensionPoint(handler,
                        () =>
                        {
                            state.OnExecutingCommandHandlerBegin(handler);
                            handler.ExecuteCommand(args, nextHandler, state.ExecutionContext);
                            state.OnExecutingCommandHandlerEnd(handler);
                        },
                        // Do not guard against cancellation exceptions, they are handled by ExecuteCommandHandlerChain
                        exceptionGuardFilter: (e) => !IsOperationCancelledException(e));
                }

                if (state == null)
                {
                    // No matching command handlers, just call next
                    handlerChain();
                    return;
                }

                _telemetrySession?.BeforeKeyProcessed();
                ExecuteCommandHandlerChain(state, handlerChain, nextCommandHandler);
                _telemetrySession?.AfterKeyProcessed();
            }
        }

        private EditorCommandHandlerServiceState InitializeExecutionState<T>(T args) where T : EditorCommandArgs
        {
            var state = new EditorCommandHandlerServiceState(args, IsTypingCommand(args));
            var uiThreadOperationContext = _factory.UIThreadOperationExecutor.BeginExecute(
                new UIThreadOperationExecutionOptions(
                    title: null, // We want same caption as the main window
                    defaultDescription: WaitForCommandExecutionString, allowCancellation: true, showProgress: true,
                    timeoutController: new TimeoutController(state, _textView, _factory.LoggingService)));
            var commandExecutionContext = new CommandExecutionContext(uiThreadOperationContext);
            commandExecutionContext.OperationContext.UserCancellationToken.Register(OnExecutionCancellationRequested, state);
            state.ExecutionContext = commandExecutionContext;

            // Per internal convention hosts can add additional host specific input context properties into
            // text view's property bag. We then surface it to command handlers (first item in case it's a list) via
            // CommandExecutionContext properties using type name as a key.
            if (_textView.Properties.TryGetProperty(CommandingConstants.AdditionalCommandExecutionContext, out object hostSpecificInputContext))
            {
                if (hostSpecificInputContext != null)
                {
                    if (hostSpecificInputContext is IList hostSpecificInputContextList &&
                        hostSpecificInputContextList.Count > 0)
                    {
                        hostSpecificInputContext = hostSpecificInputContextList[0];
                    }

                    commandExecutionContext.Properties.AddProperty(hostSpecificInputContext.GetType(), hostSpecificInputContext);
                }
            }

            return state;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsOperationCancelledException(Exception e)
        {
            return e is OperationCanceledException || e is AggregateException aggregate && aggregate.InnerExceptions.All(ie => ie is OperationCanceledException);
        }

        private void ExecuteCommandHandlerChain(
            EditorCommandHandlerServiceState state,
            Action handlerChain,
            Action nextCommandHandler)
        {
            try
            {
                // Kick off the first command handler.
                handlerChain();
                if (state.ExecutionContext.OperationContext.UserCancellationToken.IsCancellationRequested)
                {
                    LogCancellationWasIgnored(state);
                }
            }
            catch (OperationCanceledException)
            {
                OnCommandExecutionCancelled(nextCommandHandler, state);
            }
            catch (AggregateException aggregate) when (aggregate.InnerExceptions.All(e => e is OperationCanceledException))
            {
                OnCommandExecutionCancelled(nextCommandHandler, state);
            }
            finally
            {
                state.ExecutionContext?.OperationContext?.Dispose();
            }
        }

        private void OnExecutionCancellationRequested(object state)
        {
            Debug.Assert(!_factory.JoinableTaskContext.IsOnMainThread);
            ((EditorCommandHandlerServiceState)state).OnExecutionCancellationRequested();
        }

        private void OnCommandExecutionCancelled(Action nextCommandHandler, EditorCommandHandlerServiceState state)
        {
            var executingHandler = state.GetCurrentlyExecutingCommandHander();
            var executingCommand = state.ExecutingCommand;
            bool userCancelled = !state.ExecutionHasTimedOut;
            _factory.JoinableTaskContext.Factory.RunAsync(async () =>
            {
                LogCommandExecutionCancelled(executingHandler, executingCommand, userCancelled);

                string statusBarMessage = string.Format(CultureInfo.CurrentCulture, CommandingStrings.CommandCancelled, executingHandler?.DisplayName);
                await _factory.StatusBar.SetTextAsync(statusBarMessage).ConfigureAwait(false);
            });

            nextCommandHandler?.Invoke();
        }

        // Guards against re-entrant execution of the same command (can happen in contained language scenario
        // where two command handler services are chained together).
        // The guard works by placing a key composed of the ReentrancyGuard's type and the type of command
        // being executed into text view's property bag.
        private class ReentrancyGuard<T> : IDisposable
            where T : EditorCommandArgs
        {
            private readonly IPropertyOwner _owner;

            public ReentrancyGuard(IPropertyOwner owner)
            {
                _owner = owner ?? throw new ArgumentNullException(nameof(owner));
                _owner.Properties[GetGuardKey()] = this;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static (Type, Type) GetGuardKey()
            {
                return (typeof(ReentrancyGuard<>), typeof(T));
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool IsReentrantCall(IPropertyOwner owner)
            {
                return owner.Properties.ContainsProperty((typeof(ReentrancyGuard<>), typeof(T)));
            }

            public void Dispose()
            {
                _owner.Properties.RemoveProperty(GetGuardKey());
            }
        }

        private bool IsReentrantCall<T>() where T : EditorCommandArgs
        {
            return ReentrancyGuard<T>.IsReentrantCall(_textView);
        }

        //internal for unit tests
        internal IEnumerable<(ITextBuffer buffer, ICommandHandler handler)> GetOrderedBuffersAndCommandHandlers<T>() where T : EditorCommandArgs
        {
            // This method creates an ordered sequence of (buffer, handler) pairs that define proper order of
            // command handling that takes into account the buffer graph and command handlers matching buffers in the graph by
            // content types.

            // Currently this method discovers affected buffers based on caret mapping only.
            // TODO: this should be an extensibility point as in some scenarios we might want to consider selection too for example.

            // A general idea is that command handlers matching more specifically content type of buffers higher in the buffer
            // graph should be executed before those matching buffers lower in the graph or less specific content types.

            // So for example in a projection scenario (projection buffer containing C# buffer), 3 command handlers
            // matching "projection", "CSharp" and "any" content types will be ordered like this:
            // 1. command handler matching "projection" content type is executed on the projection buffer
            // 2. command handler matching "CSharp" content type is executed on the C# buffer
            // 3. command handler matching "any" content type is executed on the projection buffer

            // The ordering algorithm is as follows:
            // 1. Create an ordered list of all affected buffers in the buffer graph 
            //    by mapping caret position down and up the buffer graph. In a typical projection scenario
            //    (projection buffer containing C# buffer) that will produce (projection buffer, C# buffer) sequence.
            // 2. For each affected buffer get or create a bucket of matching command handlers,
            //    ordered by [Order] and content type specificity.
            // 3. Pick best command handler in all buckets in terms of content type specificity (e.g.
            //    if one command handler can handle "text" content type, but another can
            //    handle "CSharp" content type, we pick the latter one:
            // 3. Start with top command handler in first non empty bucket.
            // 4. Compare it with top command handlers in all other buckets in terms of content type specificity.
            // 5. yield return current handler or better one if found, pop it from its bucket
            // 6. Repeat starting with #3 utill all buckets are empty.
            //    In the projection scenario that will result in the following
            //    list of (buffer, handler) pairs: (projection buffer, projection handler), (C# buffer, C# handler),
            //    (projection buffer, any handler).

            IReadOnlyList<ITextBuffer> buffers = _bufferResolver.ResolveBuffersForCommand<T>().ToArray();
            if (buffers == null || buffers.Count == 0)
            {
                yield break;
            }

            // An array of per-buffer buckets, each containing cached list of matching command handlers,
            // ordered by [Order] and content type specificity
            var handlerBuckets = new CommandHandlerBucket[buffers.Count];
            for (int i = 0; i < buffers.Count; i++)
            {
                handlerBuckets[i] = new CommandHandlerBucket(GetOrCreateOrderedHandlers<T>(buffers[i].ContentType, _textView.Roles));
            }

            while (true)
            {
                ICommandHandlerAndMetadata currentHandler = null;
                int currentHandlerBufferIndex = 0;

                for (int i = 0; i < handlerBuckets.Length; i++)
                {
                    if (!handlerBuckets[i].IsEmpty)
                    {
                        currentHandler = handlerBuckets[i].Peek();
                        currentHandlerBufferIndex = i;
                        break;
                    }
                }

                if (currentHandler == null)
                {
                    // All buckets are empty, all done
                    break;
                }

                // Check if any other bucket has a better handler (i.e. can handle more specific content type).
                var foundBetterHandler = false;
                for (int i = 0; i < buffers.Count; i++)
                {
                    // Search in other buckets only
                    if (i != currentHandlerBufferIndex)
                    {
                        if (!handlerBuckets[i].IsEmpty)
                        {
                            var handler = handlerBuckets[i].Peek();
                            // Can this handler handle content type more specific than top handler in firstNonEmptyBucket?
                            if (_factory.ContentTypeOrderer.IsMoreSpecific(candidate: handler.Metadata.ContentTypes,
                                                                           current: currentHandler.Metadata.ContentTypes))
                            {
                                foundBetterHandler = true;
                                handlerBuckets[i].Pop();
                                yield return (buffers[i], handler.Value);
                                break;
                            }
                        }
                    }
                }

                if (!foundBetterHandler)
                {
                    yield return (buffers[currentHandlerBufferIndex], currentHandler.Value);
                    handlerBuckets[currentHandlerBufferIndex].Pop();
                }
            }
        }

        private IReadOnlyList<ICommandHandlerAndMetadata> GetOrCreateOrderedHandlers<T>(IContentType contentType, ITextViewRoleSet textViewRoles) where T : EditorCommandArgs
        {
            var cacheKey = (commandArgsType: typeof(T), contentType: contentType);
            if (!_commandHandlersByTypeAndContentType.TryGetValue(cacheKey, out var commandHandlerList))
            {
                IList<ICommandHandlerAndMetadata> newCommandHandlerList = null;
                foreach (var lazyCommandHandler in SelectMatchingCommandHandlers(_commandHandlers, contentType, textViewRoles))
                {
                    var commandHandler = _factory.GuardedOperations.InstantiateExtension<ICommandHandler>(this, lazyCommandHandler);
                    if (commandHandler is ICommandHandler<T> || commandHandler is IChainedCommandHandler<T>)
                    {
                        // The old editor in VSmac is not compatible with any command handlers except for those coming from Roslyn
                        if (_isOldGtkEditor && !commandHandler.GetType().FullName.StartsWith("Microsoft.CodeAnalysis", StringComparison.Ordinal))
                        {
                            continue;
                        }

                        if (newCommandHandlerList == null)
                        {
                            newCommandHandlerList = new FrugalList<ICommandHandlerAndMetadata>();
                        }

                        newCommandHandlerList.Add(lazyCommandHandler);
                    }
                }

                if (newCommandHandlerList?.Count > 1)
                {
                    // Order handlers by [Order] across content types, but preserve sort order otherwise
                    newCommandHandlerList = StableOrderer.Order(newCommandHandlerList).ToArray();
                }

                commandHandlerList = newCommandHandlerList?.ToArray() ?? EmptyHandlerList;
                _commandHandlersByTypeAndContentType[cacheKey] = commandHandlerList;
            }

            return commandHandlerList;
        }

        /// <summary>
        /// Selects matching command handlers without allocating a new list.
        /// </summary>
        private static IEnumerable<ICommandHandlerAndMetadata> SelectMatchingCommandHandlers(
            IEnumerable<ICommandHandlerAndMetadata> commandHandlers,
            IContentType contentType, ITextViewRoleSet textViewRoles)
        {
            foreach (var handler in commandHandlers)
            {
                if (MatchesContentType(handler.Metadata, contentType) &&
                    MatchesTextViewRoles(handler.Metadata, textViewRoles))
                {
                    yield return handler;
                }
            }
        }

        private static bool MatchesContentType(ICommandHandlerMetadata handlerMetadata, IContentType contentType)
        {
            foreach (var handlerContentType in handlerMetadata.ContentTypes)
            {
                if (contentType.IsOfType(handlerContentType))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool MatchesTextViewRoles(ICommandHandlerMetadata handlerMetadata, ITextViewRoleSet roles)
        {
            // Text view roles are optional
            if (handlerMetadata.TextViewRoles == null)
            {
                return true;
            }

            foreach (var handlerRole in handlerMetadata.TextViewRoles)
            {
                if (roles.Contains(handlerRole))
                {
                    return true;
                }
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsTypingCommand(EditorCommandArgs args)
        {
            // TODO: temporarily only include typechar to not break Roslyn inline rename and other non-typing scenario, tracked by #657668
            return args is TypeCharCommandArgs;
                   //args is DeleteKeyCommandArgs ||
                   //args is ReturnKeyCommandArgs ||
                   //args is BackspaceKeyCommandArgs ||
                   //args is TabKeyCommandArgs ||
                   //args is UndoCommandArgs ||
                   //args is RedoCommandArgs;
        }

        private void LogCommandExecutionCancelled(INamed executingHandler, EditorCommandArgs executingCommand, bool userCancelled)
        {
            _factory.LoggingService?.PostEvent($"{TelemetryEventPrefix}/ExecutionCancelled",
                $"{TelemetryPropertyPrefix}.Command", executingCommand?.GetType().FullName,
                $"{TelemetryPropertyPrefix}.CommandHandler", executingHandler?.GetType().FullName,
                $"{TelemetryPropertyPrefix}.UserCancelled", userCancelled);
        }

        private void LogCancellationWasIgnored(EditorCommandHandlerServiceState state)
        {
            bool userCancelled = !state.ExecutionHasTimedOut;
            var executingCommand = state.ExecutingCommand;
            _factory.LoggingService?.PostEvent($"{TelemetryEventPrefix}/IgnoredExecutionCancellation",
                $"{TelemetryPropertyPrefix}.Command", executingCommand?.GetType().FullName,
                $"{TelemetryPropertyPrefix}.CommandHandler", state.CommandHandlerExecutingDuringCancellationRequest?.GetType().FullName,
                $"{TelemetryPropertyPrefix}.UserCancelled", userCancelled);
        }

        private class TimeoutController : IUIThreadOperationTimeoutController
        {
            private readonly EditorCommandHandlerServiceState _state;
            private readonly ITextView _textView;
            private readonly ILoggingServiceInternal _loggingService;
            private INamed _timedOutCommandHandler;

            public TimeoutController(EditorCommandHandlerServiceState state, ITextView textView, ILoggingServiceInternal loggingService)
            {
                _state = state;
                _textView = textView;
                _loggingService = loggingService;
            }

            public int CancelAfter
                => _state.IsExecutingTypingCommand && _textView.Options.GetOptionValue(DefaultOptions.EnableTypingLatencyGuardOptionId) ?
                _textView.Options.GetOptionValue(DefaultOptions.MaximumTypingLatencyOptionId) :
                Timeout.Infinite;

            public bool ShouldCancel()
            {
                // Grab currently executing command handler as by the time it's cancelled and OnTimeout() is called it migth be gone.
                _timedOutCommandHandler = _state.GetCurrentlyExecutingCommandHander();
                return _state.IsExecutingTypingCommand;
            }

            public void OnTimeout(bool wasExecutionCancelled)
            {
                Debug.Assert(_state.IsExecutingTypingCommand);
                _state.ExecutionHasTimedOut = true;
                var executingCommand = _state.ExecutingCommand;

                _loggingService?.PostEvent($"{TelemetryEventPrefix}/ExecutionTimeout",
                    $"{TelemetryPropertyPrefix}.Command", executingCommand?.GetType().FullName,
                    $"{TelemetryPropertyPrefix}.CommandHandler", _timedOutCommandHandler?.GetType().FullName,
                    $"{TelemetryPropertyPrefix}.Timeout", this.CancelAfter,
                    $"{TelemetryPropertyPrefix}.WasExecutionCancelled", wasExecutionCancelled);
            }

            public void OnDelay()
            {
                var executingCommand = _state.ExecutingCommand;
                _loggingService?.PostEvent($"{TelemetryEventPrefix}/WaitDialogShown",
                    $"{TelemetryPropertyPrefix}.Command", executingCommand?.GetType().FullName,
                    $"{TelemetryPropertyPrefix}.CommandHandler", _state.GetCurrentlyExecutingCommandHander()?.GetType().FullName);
            }
        }
    }
}
