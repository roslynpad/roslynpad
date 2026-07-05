using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Text.Editor;
using System.Composition;
using Microsoft.VisualStudio.Text.Editor.Commanding;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Threading;
using System.Linq;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Utilities;

namespace Microsoft.VisualStudio.UI.Text.Commanding.Implementation
{
    [Export(typeof(IEditorCommandHandlerServiceFactory))]
    [Shared]
    public class EditorCommandHandlerServiceFactory : IEditorCommandHandlerServiceFactory
    {
        private readonly Lazy<IEnumerable<Lazy<ICommandHandler, CommandHandlerMetadata>>> _commandHandlers;
        private readonly IList<Lazy<ICommandingTextBufferResolverProvider, ContentTypeMetadata>> _bufferResolverProviders;
        private readonly IContentTypeRegistryService _contentTypeRegistryService;

        [ImportingConstructor]
        public EditorCommandHandlerServiceFactory(
            [ImportMany]IEnumerable<Lazy<ICommandHandler, CommandHandlerMetadata>> commandHandlers,
            [ImportMany]IEnumerable<Lazy<ICommandingTextBufferResolverProvider, ContentTypeMetadata>> bufferResolvers,
            IUIThreadOperationExecutor uiThreadOperationExecutor,
            JoinableTaskContext joinableTaskContext,
            IStatusBarService statusBar,
            IContentTypeRegistryService contentTypeRegistryService,
            IGuardedOperations guardedOperations,
            ILoggingServiceInternal loggingService)
        {
            UIThreadOperationExecutor = uiThreadOperationExecutor;
            JoinableTaskContext = joinableTaskContext;
            StatusBar = statusBar;
            GuardedOperations = guardedOperations;
            LoggingService = loggingService;

            _contentTypeRegistryService = contentTypeRegistryService;
            ContentTypeOrderer = new StableContentTypeOrderer<ICommandHandler, CommandHandlerMetadata>(_contentTypeRegistryService);
            // Ordering queries the content type registry, whose [ImportMany] properties are not
            // injected yet if this factory is activated within the same composition operation:
            // System.Composition, unlike MEF v1, hands out parts before property imports are
            // satisfied. Defer until first use, when composition has completed.
            _commandHandlers = new Lazy<IEnumerable<Lazy<ICommandHandler, CommandHandlerMetadata>>>(() => OrderCommandHandlers(commandHandlers));
            if (!bufferResolvers.Any())
            {
                throw new InvalidOperationException($"Expected to import at least one {typeof(ICommandingTextBufferResolver).Name}");
            }

            _bufferResolverProviders = bufferResolvers.ToList();
        }

        internal IGuardedOperations GuardedOperations { get; }

        internal ILoggingServiceInternal LoggingService { get; }

        internal JoinableTaskContext JoinableTaskContext { get; }

        internal IUIThreadOperationExecutor UIThreadOperationExecutor { get; }

        internal IStatusBarService StatusBar { get; }

        internal StableContentTypeOrderer<ICommandHandler, CommandHandlerMetadata> ContentTypeOrderer { get; }

        public IEditorCommandHandlerService GetService(ITextView textView)
        {
            return textView.Properties.GetOrCreateSingletonProperty(() =>
            {
                var bufferResolverProvider = GuardedOperations.InvokeBestMatchingFactory(_bufferResolverProviders, textView.TextBuffer.ContentType, _contentTypeRegistryService, errorSource: this);
                ICommandingTextBufferResolver bufferResolver = null;
                GuardedOperations.CallExtensionPoint(() => bufferResolver = bufferResolverProvider.CreateResolver(textView));
                bufferResolver = bufferResolver ?? new DefaultBufferResolver(textView);
                return new EditorCommandHandlerService(this, textView, _commandHandlers.Value, bufferResolver);
            });
        }

        public IEditorCommandHandlerService GetService(ITextView textView, ITextBuffer subjectBuffer)
        {
            if (subjectBuffer == null)
            {
                return GetService(textView);
            }

            // We cannot cache view/buffer affinitized service instance in the buffer property bag as the
            // buffer can be used by another text view, see https://devdiv.visualstudio.com/DevDiv/_workitems/edit/563472.
            // There is no good way to cache it without holding onto the buffer (which can be disconnected
            // from the text view anytime).
            return new EditorCommandHandlerService(this, textView, _commandHandlers.Value, new SingleBufferResolver(subjectBuffer));
        }

        // internal for unit tests
        internal IEnumerable<Lazy<ICommandHandler, CommandHandlerMetadata>> OrderCommandHandlers(IEnumerable<Lazy<ICommandHandler, CommandHandlerMetadata>> commandHandlers)
        {
            return this.ContentTypeOrderer.Order(commandHandlers);
        }
    }
}
