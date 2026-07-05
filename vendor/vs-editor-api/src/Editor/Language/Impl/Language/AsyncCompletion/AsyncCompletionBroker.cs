using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.Text.Utilities;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Implementation
{
    [Export(typeof(IAsyncCompletionBroker))]
    [Export(typeof(AsyncCompletionBroker))]
    [Shared]
    public sealed class AsyncCompletionBroker : IAsyncCompletionBroker
    {
        [Import]
        public IGuardedOperationsInternal GuardedOperations { get; set; }

        [Import]
        public JoinableTaskContext JoinableTaskContext { get; set; }

        [Import]
        public IContentTypeRegistryService ContentTypeRegistryService { get; set; }

        [Import]
        public CompletionAvailabilityUtility CompletionAvailability { get; set; }

        [ImportMany]
        public IEnumerable<Lazy<IAsyncCompletionSourceProvider, OrderableContentTypeAndOptionalTextViewRoleMetadata>> UnorderedCompletionSourceProviders { get; set; }

        [ImportMany]
        public IEnumerable<Lazy<IAsyncCompletionItemManagerProvider, OrderableContentTypeAndOptionalTextViewRoleMetadata>> UnorderedCompletionItemManagerProviders { get; set; }

        [ImportMany]
        public IEnumerable<Lazy<IAsyncCompletionCommitManagerProvider, OrderableContentTypeAndOptionalTextViewRoleMetadata>> UnorderedCompletionCommitManagerProviders { get; set; }

        [ImportMany]
        public IEnumerable<Lazy<ICompletionPresenterProvider, OrderableContentTypeAndOptionalTextViewRoleMetadata>> UnorderedPresenterProviders { get; set; }

        // Used for telemetry
        [Import(AllowDefault = true)]
        public ILoggingServiceInternal Logger { get; set; }

        // Used for legacy telemetry
        [Import(AllowDefault = true)]
        public ITextDocumentFactoryService TextDocumentFactoryService { get; set; }

        private IList<Lazy<IAsyncCompletionSourceProvider, OrderableContentTypeAndOptionalTextViewRoleMetadata>> _orderedCompletionSourceProviders;
        private IList<Lazy<IAsyncCompletionSourceProvider, OrderableContentTypeAndOptionalTextViewRoleMetadata>> OrderedCompletionSourceProviders
            => _orderedCompletionSourceProviders ?? (_orderedCompletionSourceProviders = Orderer.Order(UnorderedCompletionSourceProviders));

        private IList<Lazy<IAsyncCompletionItemManagerProvider, OrderableContentTypeAndOptionalTextViewRoleMetadata>> _orderedCompletionItemManagerProviders;
        private IList<Lazy<IAsyncCompletionItemManagerProvider, OrderableContentTypeAndOptionalTextViewRoleMetadata>> OrderedCompletionItemManagerProviders
            => _orderedCompletionItemManagerProviders ?? (_orderedCompletionItemManagerProviders = Orderer.Order(UnorderedCompletionItemManagerProviders));

        private IList<Lazy<IAsyncCompletionCommitManagerProvider, OrderableContentTypeAndOptionalTextViewRoleMetadata>> _orderedCompletionCommitManagerProviders;
        private IList<Lazy<IAsyncCompletionCommitManagerProvider, OrderableContentTypeAndOptionalTextViewRoleMetadata>> OrderedCompletionCommitManagerProviders
            => _orderedCompletionCommitManagerProviders ?? (_orderedCompletionCommitManagerProviders = Orderer.Order(UnorderedCompletionCommitManagerProviders));

        private IList<Lazy<ICompletionPresenterProvider, OrderableContentTypeAndOptionalTextViewRoleMetadata>> _orderedPresenterProviders;
        private IList<Lazy<ICompletionPresenterProvider, OrderableContentTypeAndOptionalTextViewRoleMetadata>> OrderedPresenterProviders
            => _orderedPresenterProviders ?? (_orderedPresenterProviders = Orderer.Order(UnorderedPresenterProviders));

        private bool firstRun = true; // used only for diagnostics
        private bool _firstInvocationReported; // used for "time to code"
        private object telemetryCreationLock = new object();
        private StableContentTypeComparer _contentTypeComparer;
        private Dictionary<CompletionAvailabilityCacheKey, bool> _providerAvailabilityCache = new Dictionary<CompletionAvailabilityCacheKey, bool>();

        /// <summary>
        /// Allow language to override which snapshot we use for mapping, to support completion in incorrectly built text views (Roslyn's DebuggerTextView)
        /// </summary>
        private const string RootSnapshotPropertyName = "CompletionRoot";

        public event EventHandler<CompletionTriggeredEventArgs> CompletionTriggered;

        #region IAsyncCompletionBroker implementation

        public bool IsCompletionActive(ITextView textView)
        {
            return textView?.Properties?.ContainsProperty(typeof(IAsyncCompletionSession)) == true;
        }

        public bool IsCompletionSupported(IContentType contentType) => CompletionAvailability.IsAvailable(contentType, roles: null); // This will call HasCompletionProviders among doing other checks

        public bool IsCompletionSupported(IContentType contentType, ITextViewRoleSet textViewRoleSet) => CompletionAvailability.IsAvailable(contentType, textViewRoleSet); // This will call HasCompletionProviders among doing other checks

        /// <summary>
        /// Returns whether there exist any <see cref="IAsyncCompletionSourceProvider"/>
        /// for the provided <see cref="IContentType"/> or any of its base content types.
        /// Since MEF parts don't change on runtime, the answer is cached per <see cref="IContentType"/> for faster retrieval.
        /// </summary>
        internal bool HasCompletionProviders(IContentType contentType, ITextViewRoleSet roles = null)
        {
            var key = new CompletionAvailabilityCacheKey(contentType, roles);

            // Use cache if available
            if (_providerAvailabilityCache.TryGetValue(key, out bool featureIsAvailable))
                return featureIsAvailable;

            featureIsAvailable = UnorderedCompletionSourceProviders.Any(n =>
                n.Metadata.ContentTypes.Any(ct => contentType.IsOfType(ct))
                && (n.Metadata.TextViewRoles == null || roles == null || roles.ContainsAny(n.Metadata.TextViewRoles)));

            _providerAvailabilityCache[key] = featureIsAvailable;
            return featureIsAvailable;
        }

        public IAsyncCompletionSession GetSession(ITextView textView)
        {
            if (textView.Properties.TryGetProperty(typeof(IAsyncCompletionSession), out IAsyncCompletionSession session))
            {
                return session;
            }
            return null;
        }

        public IAsyncCompletionSession TriggerCompletion(ITextView textView, CompletionTrigger trigger, SnapshotPoint triggerLocation, CancellationToken token)
        {
            var session = GetSession(textView);
            if (session != null)
            {
                return session;
            }

            // This is a simple check that only queries the feature service.
            // If it succeeds, we will map triggerLocation to available buffers to discover MEF parts.
            // This is expensive but projected languages require it to discover parts in all available buffers.
            // To avoid doing this work, call IsCompletionSupported with appropriate IContentType prior to calling TriggerCompletion
            if (!CompletionAvailability.IsCurrentlyAvailable(textView))
                return null;

            if (textView.IsClosed)
                return null;

            if (!JoinableTaskContext.IsOnMainThread)
                throw new InvalidOperationException($"This method must be callled on the UI thread.");

            var telemetryHost = GetOrCreateTelemetry(textView);
            var telemetry = new CompletionSessionTelemetry(telemetryHost);

            var rootSnapshot = GetRootSnapshot(textView);

            if (token.IsCancellationRequested || textView.IsClosed)
                return null;

            // See if we can use more aggressive cancellation token for typing scenarios
            if (trigger.Reason == CompletionTriggerReason.Insertion)
                token = CompletionUtilities.GetResponsiveToken(textView, token);

            GetCompletionSources(triggerLocation, GetItemSourceProviders, rootSnapshot, textView, textView.BufferGraph, trigger, telemetry, token,
                out var sourcesWithLocations, out var applicableToSpan);

            if (token.IsCancellationRequested || textView.IsClosed)
                return null;

            // No source declared an appropriate ApplicableToSpan
            if (applicableToSpan == default)
                return null;

            // No source wishes to participate
            if (!sourcesWithLocations.Any())
                return null;

            // Some of our extensions need to initialize the source providers before they initialize commit manager providers.
            // Therefore, it is important to invoke GetCommitManagerProviders after invoking GetItemSourceProviders.
            GetCommitManagersAndChars(triggerLocation, GetCommitManagerProviders, rootSnapshot, textView, telemetry,
                out var managersWithBuffers, out var potentialCommitChars);

            if (_contentTypeComparer == null)
                _contentTypeComparer = new StableContentTypeComparer(ContentTypeRegistryService);

            var itemManager = GetItemManager(triggerLocation, GetItemManagerProviders, rootSnapshot, textView, _contentTypeComparer);
            var presenterProvider = GetPresenterProvider(triggerLocation, GetPresenters, rootSnapshot, textView.Roles, _contentTypeComparer);

            if (token.IsCancellationRequested || textView.IsClosed)
                return null;

            session = new AsyncCompletionSession(applicableToSpan, potentialCommitChars, JoinableTaskContext, presenterProvider, sourcesWithLocations, managersWithBuffers, itemManager, this, textView, telemetry, GuardedOperations);
            textView.Properties.AddProperty(typeof(IAsyncCompletionSession), session);

            textView.Closed += DismissSessionOnViewClosed;
            EmulateLegacyCompletionTelemetry(textView);
            GuardedOperations.RaiseEvent(this, CompletionTriggered, new CompletionTriggeredEventArgs(session, textView));

            return session;
        }

        public async Task<AggregatedCompletionContext> GetAggregatedCompletionContextAsync(ITextView textView, CompletionTrigger trigger, SnapshotPoint triggerLocation, CancellationToken token)
        {
            if (token.IsCancellationRequested || textView.IsClosed)
                return AggregatedCompletionContext.Empty;

            var telemetryHost = GetOrCreateTelemetry(textView);
            var telemetry = new CompletionSessionTelemetry(telemetryHost, headless: true);

            // ----- GetCompletionSources and GetRootSnapshot need to be run on the UI thread:
            await JoinableTaskContext.Factory.SwitchToMainThreadAsync();

            if (token.IsCancellationRequested || textView.IsClosed)
                return AggregatedCompletionContext.Empty;

            var rootSnapshot = GetRootSnapshot(textView);

            GetCompletionSources(triggerLocation, GetItemSourceProviders, rootSnapshot, textView, textView.BufferGraph, trigger, telemetry, token,
                out var sourcesWithLocations, out var applicableToSpan);

            // ----- Go back to background thread to continue processing
            await TaskScheduler.Default;

            if (token.IsCancellationRequested || textView.IsClosed)
                return AggregatedCompletionContext.Empty;

            // No source declared an appropriate ApplicableToSpan
            if (applicableToSpan == default)
                return AggregatedCompletionContext.Empty;

            // No source wishes to participate
            if (!sourcesWithLocations.Any())
                return null;

            var aggregatingSession = AsyncCompletionSession.CreateAggregatingSession(applicableToSpan, JoinableTaskContext, sourcesWithLocations, this, textView, telemetry, GuardedOperations);
            
            var completionData = await aggregatingSession.ConnectToCompletionSources(
                trigger, triggerLocation, rootSnapshot,
                getExpandedContext: false, initialItems: default, expander: default,
                token: token).ConfigureAwait(true);

            if (completionData.IsCanceled)
                return AggregatedCompletionContext.Empty;

            var aggregateCompletionContext = new CompletionContext(
                completionData.Items,
                completionData.RequestedSuggestionItemOptions,
                completionData.InitialSelectionHint);
            return new AggregatedCompletionContext(aggregateCompletionContext, aggregatingSession);
        }

        /// <summary>
        /// Gets the root snapshot which we use to locate all buffers available at a given location
        /// Normally, <see cref="ITextView.TextSnapshot"/> is appropriate to use.
        ///
        /// However, in Roslyn Debugger scenario, the text view is built in an uncoventional way,
        /// such that the TextView's TextSnapshot corresponds to what should be in middle of the buffer graph.
        /// To work around this, we ask Roslyn to provide the true root in the property bag
        /// so that we can correctly perform mapping. To retire this method, we need Roslyn
        /// to refactor the debugger text view and the immediate window to use correct projection.
        ///
        /// Note that the <see cref="ITextView.VisualSnapshot"/> (usually the same as <see cref="IBufferGraph.TopBuffer"/>)
        /// is inappropriate, because it might be an elision buffer. If we map down from the elision buffer,
        /// we may locate incorrect points around elided text.
        ///
        /// Note that the root snapshot cannot be use to realize the <see cref="IAsyncCompletionSession.ApplicableToSpan"/>,
        /// which is always defined on the <see cref="ITextView.TextSnapshot"/>
        /// </summary>
        /// <param name="textView">TextView which will host completion</param>
        /// <returns><see cref="ITextSnapshot"/> appropriate to map down to locate buffers.</returns>
        internal static ITextSnapshot GetRootSnapshot(ITextView textView)
        {
            if (textView.Properties.TryGetProperty(RootSnapshotPropertyName, out ITextBuffer rootBuffer))
            {
                return rootBuffer.CurrentSnapshot;
            }
            return textView.TextSnapshot;
        }

        #endregion

        #region Internal communication with AsyncCompletionSession

        /// <summary>
        /// This method is used by <see cref="IAsyncCompletionSession"/> to inform the broker that it should forget about the session.
        /// Invoked as a result of dismissing. This method does not dismiss the session!
        /// </summary>
        /// <param name="session">Session being dismissed</param>
#pragma warning disable CA1822 // Member does not access instance data and can be marked as static
        internal void ForgetSession(IAsyncCompletionSession session)
        {
            session.TextView.Closed -= DismissSessionOnViewClosed;
            session.TextView.Properties.RemoveProperty(typeof(IAsyncCompletionSession));
        }
#pragma warning restore CA1822

        #endregion

        #region MEF part helper methods

        private void GetCommitManagersAndChars(
            SnapshotPoint triggerLocation,
            Func<IContentType, ITextViewRoleSet, IReadOnlyList<Lazy<IAsyncCompletionCommitManagerProvider, OrderableContentTypeAndOptionalTextViewRoleMetadata>>> getImports,
            ITextSnapshot rootSnapshot,
            ITextView textView,
            CompletionSessionTelemetry telemetry,
            out IList<(IAsyncCompletionCommitManager, ITextBuffer)> managersWithBuffers,
            out ImmutableArray<char> potentialCommitChars)
        {
            var commitManagersWithData = MetadataUtilities<IAsyncCompletionCommitManagerProvider, OrderableContentTypeAndOptionalTextViewRoleMetadata>
                .GetBuffersAndImports(triggerLocation, rootSnapshot, textView.Roles, getImports);

            var potentialCommitCharsBuilder = ImmutableArray.CreateBuilder<char>();
            managersWithBuffers = new List<(IAsyncCompletionCommitManager, ITextBuffer)>(1);
            foreach (var (buffer, point, import) in commitManagersWithData)
            {
                telemetry.UiStopwatch.Restart();
                var managerProvider = GuardedOperations.InstantiateExtension(this, import);
                var manager = GuardedOperations.CallExtensionPoint(
                    errorSource: managerProvider,
                    call: () => managerProvider.GetOrCreate(textView),
                    valueOnThrow: null);

                if (manager == null)
                    continue;

                GuardedOperations.CallExtensionPoint(
                    errorSource: manager,
                    call: () =>
                    {
                        var characters = manager.PotentialCommitCharacters;
                        potentialCommitCharsBuilder.AddRange(characters);
                    });
                managersWithBuffers.Add((manager, buffer));
                telemetry.UiStopwatch.Stop();
                telemetry.RecordObtainingCommitManagerData(manager, telemetry.UiStopwatch.ElapsedMilliseconds);
            }
            potentialCommitChars = potentialCommitCharsBuilder.ToImmutable();
        }

        private void GetCompletionSources(
            SnapshotPoint triggerLocation,
            Func<IContentType, ITextViewRoleSet, IReadOnlyList<Lazy<IAsyncCompletionSourceProvider, OrderableContentTypeAndOptionalTextViewRoleMetadata>>> getImports,
            ITextSnapshot rootSnapshot,
            ITextView textView,
            IBufferGraph bufferGraph,
            CompletionTrigger trigger,
            CompletionSessionTelemetry telemetry,
            CancellationToken token,
            out List<(IAsyncCompletionSource Source, SnapshotPoint Point)> sourcesWithLocations,
            out SnapshotSpan applicableToSpan)
        {
            var sourcesWithData = MetadataUtilities<IAsyncCompletionSourceProvider, OrderableContentTypeAndOptionalTextViewRoleMetadata>
                .GetBuffersAndImports(triggerLocation, rootSnapshot, textView.Roles, getImports);

            var applicableToSpanBuilder = default(SnapshotSpan);
            bool applicableToSpanExists = false;
            bool anySourceParticipates = false;
            bool anySourceExclusive = false;
            var sourcesWithLocationsBuilder = new List<(IAsyncCompletionSource, SnapshotPoint, CompletionParticipation)>();

            foreach (var (buffer, point, import) in sourcesWithData)
            {
                telemetry.UiStopwatch.Restart();

                var sourceProvider = GuardedOperations.InstantiateExtension(this, import);
                var source = GuardedOperations.CallExtensionPoint(
                    errorSource: sourceProvider,
                    call: () => sourceProvider.GetOrCreate(textView),
                    valueOnThrow: null);

                if (source == null)
                {
                    telemetry.UiStopwatch.Stop();
                    telemetry.RecordObtainingSourceSpan(source, telemetry.UiStopwatch.ElapsedMilliseconds);
                    continue;
                }

                // Iterate through all sources and add them to collection
                var startData = GuardedOperations.CallExtensionPoint(
                    errorSource: source,
                    call: () => source.InitializeCompletion(trigger, point, token),
                    valueOnThrow: CompletionStartData.DoesNotParticipateInCompletion);

                telemetry.UiStopwatch.Stop();
                telemetry.RecordObtainingSourceSpan(source, telemetry.UiStopwatch.ElapsedMilliseconds);

                if (!applicableToSpanExists && startData.ApplicableToSpan != default)
                {
                    applicableToSpanExists = true;
                    applicableToSpanBuilder = startData.ApplicableToSpan;
                }
                if (startData.Participation == CompletionParticipation.ProvidesItems)
                {
                    anySourceParticipates = true;
                }
                else if (startData.Participation == CompletionParticipation.ExclusivelyProvidesItems)
                {
                    anySourceParticipates = true;
                    anySourceExclusive = true;
                }
                sourcesWithLocationsBuilder.Add((source, point, startData.Participation));
            }

            // Map the applicable to span to the view's text snapshot and use it for completion,
            if (applicableToSpanExists)
            {
                if (rootSnapshot == textView.TextSnapshot)
                {
                    // Typical case; ApplicableToSpan is always defined on TextView.TextBuffer, so we will map up
                    var mappingSpan = bufferGraph.CreateMappingSpan(applicableToSpanBuilder, SpanTrackingMode.EdgeInclusive);
                    var spans = mappingSpan.GetSpans(textView.TextSnapshot);

                    if (spans.Count == 0)
                        throw new InvalidOperationException("Completion expects the Applicable To Span to be mappable to the view's TextBuffer.");
                    applicableToSpanBuilder = spans[0];
                }
                else
                {
                    // Edge case; in Roslyn's DebuggerTextView, TextView.TextSnapshot is below the root snapshot
                    // ApplicableToSpan is always defined on textView's TextBuffer, so we will to map down
                    var spans = MappingHelper.MapDownToBufferNoTrack(applicableToSpanBuilder, textView.TextBuffer);

                    if (spans.Count == 0)
                        throw new InvalidOperationException("Completion expects the Applicable To Span to be mappable to the view's TextBuffer.");
                    applicableToSpanBuilder = spans[0];
                }
            }

            // Copying temporary values because we can't access out&ref params in lambdas
            if (anySourceExclusive)
            {
                sourcesWithLocations = sourcesWithLocationsBuilder.Where(n => n.Item3 == CompletionParticipation.ExclusivelyProvidesItems).Select(n => (n.Item1, n.Item2)).ToList();
            }
            else if (anySourceParticipates)
            {
                sourcesWithLocations = sourcesWithLocationsBuilder.Where(n => n.Item3 == CompletionParticipation.ProvidesItems).Select(n => (n.Item1, n.Item2)).ToList();
            }
            else
            {
                sourcesWithLocations = new List<(IAsyncCompletionSource Source, SnapshotPoint Point)>();
            }
            applicableToSpan = applicableToSpanBuilder;
        }

        private IAsyncCompletionItemManager GetItemManager(
            SnapshotPoint triggerLocation,
            Func<IContentType, ITextViewRoleSet, IReadOnlyList<Lazy<IAsyncCompletionItemManagerProvider, OrderableContentTypeAndOptionalTextViewRoleMetadata>>> getImports,
            ITextSnapshot rootSnapshot,
            ITextView textView,
            StableContentTypeComparer contentTypeComparer
            )
        {
            var itemManagerProvidersWithData = MetadataUtilities<IAsyncCompletionItemManagerProvider, OrderableContentTypeAndOptionalTextViewRoleMetadata>
                .GetOrderedBuffersAndImports(triggerLocation, rootSnapshot, textView.Roles, getImports, contentTypeComparer);
            if (!itemManagerProvidersWithData.Any())
            {
                // This should never happen because we provide a default for "text" content type. Does content type not derive from "text"?
                throw new InvalidOperationException("No IAsyncCompletionItemManager found. Completion will be unavailable.");
            }

            var bestItemManagerProvider = GuardedOperations.InstantiateExtension(this, itemManagerProvidersWithData.First().import);
            return GuardedOperations.CallExtensionPoint(bestItemManagerProvider, () => bestItemManagerProvider.GetOrCreate(textView), null);
        }

        private ICompletionPresenterProvider GetPresenterProvider(
            SnapshotPoint triggerLocation,
            Func<IContentType, ITextViewRoleSet, IReadOnlyList<Lazy<ICompletionPresenterProvider, OrderableContentTypeAndOptionalTextViewRoleMetadata>>> getImports,
            ITextSnapshot rootSnapshot,
            ITextViewRoleSet textViewRoles,
            StableContentTypeComparer contentTypeComparer)
        {
            var presenterProvidersWithData = MetadataUtilities<ICompletionPresenterProvider, OrderableContentTypeAndOptionalTextViewRoleMetadata>
                .GetOrderedBuffersAndImports(triggerLocation, rootSnapshot, textViewRoles, getImports, contentTypeComparer);
            ICompletionPresenterProvider presenterProvider = null;
            if (presenterProvidersWithData.Any())
                presenterProvider = GuardedOperations.InstantiateExtension(this, presenterProvidersWithData.First().import);

            if (firstRun)
            {
                System.Diagnostics.Debug.Assert(presenterProvider != null, $"No instance of {nameof(ICompletionPresenterProvider)} is loaded. Completion will work without the UI.");
                firstRun = false;
            }

            return presenterProvider;
        }

        private IReadOnlyList<Lazy<IAsyncCompletionSourceProvider, OrderableContentTypeAndOptionalTextViewRoleMetadata>> GetItemSourceProviders(IContentType contentType, ITextViewRoleSet textViewRoles)
        {
            return OrderedCompletionSourceProviders.Where(n => n.Metadata.ContentTypes.Any(c => contentType.IsOfType(c)) && (n.Metadata.TextViewRoles == null || textViewRoles.ContainsAny(n.Metadata.TextViewRoles))).ToList();
        }

        private IReadOnlyList<Lazy<IAsyncCompletionItemManagerProvider, OrderableContentTypeAndOptionalTextViewRoleMetadata>> GetItemManagerProviders(IContentType contentType, ITextViewRoleSet textViewRoles)
        {
            return OrderedCompletionItemManagerProviders.Where(n => n.Metadata.ContentTypes.Any(c => contentType.IsOfType(c)) && (n.Metadata.TextViewRoles == null || textViewRoles.ContainsAny(n.Metadata.TextViewRoles))).OrderBy(n => n.Metadata.ContentTypes, _contentTypeComparer).ToList();
        }

        private IReadOnlyList<Lazy<IAsyncCompletionCommitManagerProvider, OrderableContentTypeAndOptionalTextViewRoleMetadata>> GetCommitManagerProviders(IContentType contentType, ITextViewRoleSet textViewRoles)
        {
            return OrderedCompletionCommitManagerProviders.Where(n => n.Metadata.ContentTypes.Any(c => contentType.IsOfType(c)) && (n.Metadata.TextViewRoles == null || textViewRoles.ContainsAny(n.Metadata.TextViewRoles))).ToList();
        }

        private IReadOnlyList<Lazy<ICompletionPresenterProvider, OrderableContentTypeAndOptionalTextViewRoleMetadata>> GetPresenters(IContentType contentType, ITextViewRoleSet textViewRoles)
        {
            return OrderedPresenterProviders.Where(n => n.Metadata.ContentTypes.Any(c => contentType.IsOfType(c)) && (n.Metadata.TextViewRoles == null || textViewRoles.ContainsAny(n.Metadata.TextViewRoles))).OrderBy(n => n.Metadata.ContentTypes, _contentTypeComparer).ToList();
        }

        #endregion

        #region Telemetry

        private CompletionTelemetryHost GetOrCreateTelemetry(ITextView textView)
        {
            if (textView.Properties.TryGetProperty(typeof(CompletionTelemetryHost), out CompletionTelemetryHost existingTelemetry))
            {
                return existingTelemetry;
            }
            else
            {
                lock (telemetryCreationLock)
                {
                    if (!textView.Properties.TryGetProperty(typeof(CompletionTelemetryHost), out CompletionTelemetryHost telemetry))
                    {
                        telemetry = new CompletionTelemetryHost(Logger, this, textView.TextBuffer.ContentType.DisplayName);
                        textView.Properties.AddProperty(typeof(CompletionTelemetryHost), telemetry);
                        textView.Closed += SendTelemetryOnViewClosed;
                    }
                    return telemetry;
                }
            }
        }

#pragma warning disable CA1822 // Member does not access instance data and can be marked as static
        private static void SendTelemetry(ITextView textView)
        {
            if (textView.Properties.TryGetProperty(typeof(CompletionTelemetryHost), out CompletionTelemetryHost telemetry))
            {
                telemetry.Send();
                textView.Properties.RemoveProperty(typeof(CompletionTelemetryHost));
            }
        }
#pragma warning restore CA1822

        // Parity with legacy telemetry
        private void EmulateLegacyCompletionTelemetry(ITextView textView)
        {
            if (Logger == null || _firstInvocationReported)
                return;

            string GetFileExtension(ITextBuffer buffer)
            {
                var documentFactoryService = TextDocumentFactoryService;
                if (buffer != null && documentFactoryService != null)
                {
                    documentFactoryService.TryGetTextDocument(buffer, out ITextDocument currentDocument);
                    if (currentDocument != null && currentDocument.FilePath != null)
                    {
                        return System.IO.Path.GetExtension(currentDocument.FilePath);
                    }
                }
                return null;
            }
            var fileExtension = GetFileExtension(textView.TextBuffer) ?? "Unknown";
            var reportedContentType = textView.TextBuffer.ContentType?.ToString() ?? "Unknown";

            _firstInvocationReported = true;
            Logger.PostEvent(TelemetryEventType.Operation, "VS/Editor/IntellisenseFirstRun/Opened", TelemetryResult.Success,
                ("VS.Editor.IntellisenseFirstRun.Opened.ContentType", reportedContentType),
                ("VS.Editor.IntellisenseFirstRun.Opened.FileExtension", fileExtension));
        }

        #endregion

        private void DismissSessionOnViewClosed(object sender, EventArgs e)
        {
            var view = (ITextView)sender;
            view.Closed -= DismissSessionOnViewClosed;
            GetSession(view)?.Dismiss();
        }

        private void SendTelemetryOnViewClosed(object sender, EventArgs e)
        {
            var view = (ITextView)sender;
            view.Closed -= SendTelemetryOnViewClosed;
            try
            {
                SendTelemetry(view);
            }
            catch (Exception ex)
            {
                GuardedOperations.HandleException(this, ex);
            }
        }
    }
}
