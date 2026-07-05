using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text.Utilities;

namespace Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Implementation
{
    /// <summary>
    /// Telemetry data pertinent to a single <see cref="AsyncCompletionSession"/>
    /// </summary>
    internal class CompletionSessionTelemetry
    {
        private readonly CompletionTelemetryHost _telemetryHost;

        /// <summary>
        /// Tracks time spent on the worker thread - getting data, filtering and sorting. Used for telemetry.
        /// </summary>
        internal Stopwatch ComputationStopwatch { get; } = new Stopwatch();

        /// <summary>
        /// Tracks time spent on the UI thread - either rendering or committing. Used for telemetry.
        /// </summary>
        internal Stopwatch UiStopwatch { get; } = new Stopwatch();

        /// <summary>
        /// Tracks time spent between triggering session and displaying UI or committing the item, whichever is sooner.
        /// </summary>
        internal Stopwatch E2EStopwatch { get; } = new Stopwatch();

        // Names of parts that participated in completion
        internal string ItemManagerName { get; private set; }
        internal string PresenterProviderName { get; private set; }
        internal string CommitManagerName { get; private set; }

        // "Setup" is work done on UI thread by IAsyncCompletionBroker
        // since there are many participating MEF parts, we record their names together with the time
        internal Dictionary<string, long> CommitManagerSetupDuration { get; } = new Dictionary<string, long>();
        internal Dictionary<string, long> ItemSourceSetupDuration { get; } = new Dictionary<string, long>();

        // "Get Context" is work done by IAsyncCompletionItemSource
        // multiple sources may participate in a single completion session
        internal Dictionary<string, long> ItemSourceGetContextDuration { get; } = new Dictionary<string, long>();

        // "Processing" is work done by IAsyncCompletionItemManager
        internal long InitialProcessingDuration { get; private set; }
        internal long TotalProcessingDuration { get; private set; }
        internal int TotalProcessingCount { get; private set; }

        // "Rendering" is work done on UI thread by ICompletionPresenter
        internal long InitialRenderingDuration { get; private set; }
        internal long TotalRenderingDuration { get; private set; }
        internal int TotalRenderingCount { get; private set; }

        // "Closing" is also work done on UI thread by ICompletionPresenter
        internal long ClosingDuration { get; private set; }

        // "Commit" is work done on UI thread by IAsyncCompletionCommitManager
        internal long CommitDuration { get; private set; }

        // The following work is a mix of "Get Context" and "Processing" and blocks UI thread
        internal long BlockingComputationDuration { get; private set; }

        // Additional data for the E2E telemetry
        internal CompletionSessionState CompletionState { get; private set; }
        internal bool NoChanges { get; private set; }
        internal bool UserWaitedForNoChanges { get; private set; }
        internal Dictionary<string, int> BlockingExtensionCounter { get; } = new Dictionary<string, int>();

        // Additional parameters related to work done by IAsyncCompletionItemManager
        internal bool UserEverScrolled { get; private set; }
        internal bool UserEverSetFilters { get; private set; }
        internal int FinalItemCount { get; private set; }
        internal int NumberOfKeystrokes { get; private set; }

        // Additional parameters related to headless operation
        internal bool Headless { get; private set; }
        private const string HeadlessCallNamePrefix = "HEADLESS ";

        public CompletionSessionTelemetry(CompletionTelemetryHost telemetryHost, bool headless = false)
        {
            _telemetryHost = telemetryHost;
            Headless = headless;
        }

        internal void RecordProcessing(long duration, int itemCount)
        {
            if (TotalProcessingCount == 0)
            {
                InitialProcessingDuration = duration;
            }
            else
            {
                TotalProcessingDuration += duration;
                FinalItemCount = itemCount;
            }
            TotalProcessingCount++;
        }

        internal void RecordRendering(long duration)
        {
            if (TotalRenderingCount == 0)
                InitialRenderingDuration = duration;
            TotalRenderingCount++;
            TotalRenderingDuration += duration;
        }

        internal void RecordScrolling()
        {
            UserEverScrolled = true;
        }

        internal void RecordChangingFilters()
        {
            UserEverSetFilters = true;
        }

        internal void RecordKeystroke()
        {
            NumberOfKeystrokes++;
        }

        internal void RecordCommitted(long duration, bool noChanges,
            IAsyncCompletionCommitManager manager)
        {
            CommitManagerName = CompletionTelemetryHost.GetCommitManagerName(manager);
            CommitDuration = duration;
            NoChanges = noChanges;
        }

        internal void RecordClosing(long duration)
        {
            ClosingDuration += duration;
        }

        internal void Save(
            IAsyncCompletionItemManager itemManager,
            ICompletionPresenterProvider presenterProvider,
            CompletionSessionState state)
        {
            ItemManagerName = CompletionTelemetryHost.GetItemManagerName(itemManager);
            PresenterProviderName = CompletionTelemetryHost.GetPresenterProviderName(presenterProvider);
            CompletionState = state;
            if (NoChanges && BlockingComputationDuration > 0)
                UserWaitedForNoChanges = true;
            _telemetryHost.Add(this);
        }

        internal void RecordObtainingCommitManagerData(IAsyncCompletionCommitManager manager, long elapsedMilliseconds)
        {
            var name = CompletionTelemetryHost.GetCommitManagerName(manager);
            CommitManagerSetupDuration[name] = elapsedMilliseconds;
        }

        internal void RecordObtainingSourceSpan(IAsyncCompletionSource source, long elapsedMilliseconds)
        {
            var name = CompletionTelemetryHost.GetSourceName(source);
            if (Headless)
                name = HeadlessCallNamePrefix + name;
            ItemSourceSetupDuration[name] = elapsedMilliseconds;
        }

        internal void RecordObtainingSourceContext(IAsyncCompletionSource source, long elapsedMilliseconds)
        {
            var name = CompletionTelemetryHost.GetSourceName(source);
            if (Headless)
                name = HeadlessCallNamePrefix + name;
            ItemSourceGetContextDuration[name] = elapsedMilliseconds;
        }

        internal void RecordBlockingWaitForComputation(long elapsedMilliseconds)
        {
            BlockingComputationDuration = elapsedMilliseconds;
        }

        internal void RecordBlockingExtension(object extension)
        {
            if (extension == null)
                return;

            string extensionName;
            switch (extension)
            {
                case IAsyncCompletionSource source:
                    extensionName = CompletionTelemetryHost.GetSourceName(source);
                    break;
                case IAsyncCompletionItemManager itemManager:
                    extensionName = CompletionTelemetryHost.GetItemManagerName(itemManager);
                    break;
                case IAsyncCompletionCommitManager commitManager:
                    extensionName = CompletionTelemetryHost.GetCommitManagerName(commitManager);
                    break;
                default:
                    extensionName = extension.GetType().ToString();
                    break;
            }

            if (!BlockingExtensionCounter.ContainsKey(extensionName))
                BlockingExtensionCounter[extensionName] = 0;
            BlockingExtensionCounter[extensionName]++;
        }
    }

    /// <summary>
    /// Aggregates <see cref="CompletionSessionTelemetry"/>.
    /// </summary>
    internal class CompletionTelemetryHost
    {
        private class AggregateCommitManagerData
        {
            internal long TotalCommitTime;
            internal long TotalSetupTime;

            // These values are used to calculate averages
            internal long CommitCount;
            internal long SetupCount;

            // We persist the slowest duration for operations on the UI thread
            internal long MaxCommitTime;
            internal long MaxSetupTime;
        }

        private class AggregateSourceData
        {
            internal long TotalGetContextTime;
            internal long TotalSetupTime;

            // These values are used to calculate averages
            internal long GetContextCount;
            internal long SetupCount;

            // We persist the slowest duration for operations on the UI thread
            internal long MaxSetupTime;
        }

        private class AggregateItemManagerData
        {
            internal long InitialProcessTime;
            internal long TotalProcessTime;
            internal long TotalBlockingComputationTime;
            internal long MaxBlockingComputationTime;

            internal int TotalKeystrokes;
            internal int UserEverScrolled;
            internal int UserEverSetFilters;
            internal int FinalItemCount;

            // These values are used to calculate averages
            internal int SessionCount;
            // This value is used to calculate average processing time. One session may have multiple processing operations.
            internal int ProcessCount;
        }

        private class AggregatePresenterData
        {
            internal long InitialRenderTime;
            internal long TotalRenderTime;
            internal long TotalClosingTime;

            // These values are used to calculate averages
            internal int RenderCount;
            internal int ClosingCount;

            // We persist the slowest duration for operations on the UI thread
            internal long MaxRenderTime;
            internal long MaxClosingTime;
        }

        private class AggregateE2EData
        {
            internal int Committed;
            internal int CommittedThroughClick;
            internal int CommittedThroughCompleteWord;
            internal int CommittedSuggestionItem;
            internal int CommittedThroughTypedChar;
            internal int Dismissed;
            internal int DismissedDueToBackspace;
            internal int DismissedDueToCancellation;
            internal int DismissedDueToCaretLeaving;
            internal int DismissedDuringFiltering;
            internal int DismissedDueToNoItems;
            internal int DismissedDueToNonBlockingMode;
            internal int DismissedDueToResponsiveMode;
            internal int DismissedDueToSuggestionMode;
            internal int DismissedDueToUnhandledError;
            internal int DismissedThroughUI;
            internal int DismissedUninitialized;

            // Measuring distribution of time spent between triggering session and displaying UI or committing the item, whichever is sooner
            internal int HistogramBucket25;
            internal int HistogramBucket50;
            internal int HistogramBucket100;
            internal int HistogramBucket250;
            internal int HistogramBucket500;
            internal int HistogramBucket1000;
            internal int HistogramBucket2000;
            internal int HistogramBucketLast;
            internal int HistogramBucketCanceled;
            internal int HistogramBucketInvalid;

            // Measuring distribution of user action and reaction of completion session
            internal int HistogramNoChanges;
            internal int HistogramNoChangesAndUserWaited;
            internal int HistogramNoChangesThroughTypedChar;
            internal int HistogramNoChangesAndUserWaitedThroughTypedChar;
            internal int HistogramChanges;
            internal int HistogramChangesAndUserWaited;
            internal int HistogramChangesThroughTypedChar;
            internal int HistogramChangesAndUserWaitedThroughTypedChar;
        }

        Dictionary<string, AggregateCommitManagerData> CommitManagerData = new Dictionary<string, AggregateCommitManagerData>();
        Dictionary<string, AggregateItemManagerData> ItemManagerData = new Dictionary<string, AggregateItemManagerData>();
        Dictionary<string, AggregatePresenterData> PresenterData = new Dictionary<string, AggregatePresenterData>();
        Dictionary<string, AggregateSourceData> SourceData = new Dictionary<string, AggregateSourceData>();
        Dictionary<string, int> BlockingExtensionData = new Dictionary<string, int>();
        AggregateE2EData E2EData = new AggregateE2EData();

        private readonly ILoggingServiceInternal _logger;
        private readonly AsyncCompletionBroker _broker;
        private readonly string _textViewContentType;

        public CompletionTelemetryHost(ILoggingServiceInternal logger, AsyncCompletionBroker broker, string textViewContentType)
        {
            _logger = logger;
            _broker = broker;
            _textViewContentType = textViewContentType;
        }

        internal static string GetSourceName(IAsyncCompletionSource source) => source?.GetType().ToString() ?? string.Empty;
        internal static string GetCommitManagerName(IAsyncCompletionCommitManager commitManager) => commitManager?.GetType().ToString() ?? string.Empty;
        internal static string GetItemManagerName(IAsyncCompletionItemManager itemManager) => itemManager?.GetType().ToString() ?? string.Empty;
        internal static string GetPresenterProviderName(ICompletionPresenterProvider provider) => provider?.GetType().ToString() ?? string.Empty;

        /// <summary>
        /// Adds data from <see cref="CompletionSessionTelemetry" /> to appropriate buckets.
        /// </summary>
        /// <param name=""></param>
        internal void Add(CompletionSessionTelemetry telemetry)
        {
            if (_logger == null)
                return;

            AddSourceData(telemetry, SourceData);
            AddItemManagerData(telemetry, ItemManagerData);
            AddCommitManagerData(telemetry, CommitManagerData);
            AddPresenterData(telemetry, PresenterData);
            AddE2EData(telemetry, E2EData);
            AddBlockingExtensionData(telemetry, BlockingExtensionData);
        }

        /// <summary>
        /// Sends batch of collected data.
        /// </summary>
        internal void Send()
        {
            if (_logger == null)
                return;

            foreach (var data in ItemManagerData)
            {
                if (data.Value.SessionCount == 0)
                    continue;
                if (data.Value.ProcessCount == 0)
                    continue;

                _logger.PostEvent(TelemetryEventType.Operation,
                    ItemManagerEventName,
                    TelemetryResult.Success,
                    (ItemManagerName, data.Key),
                    (ItemManagerAverageFinalItemCount, data.Value.FinalItemCount / (double)data.Value.SessionCount),
                    (ItemManagerAverageInitialProcessDuration, data.Value.InitialProcessTime / (double)data.Value.SessionCount),
                    (ItemManagerAverageFilterDuration, data.Value.TotalProcessTime / (double)data.Value.ProcessCount),
                    (ItemManagerAverageKeystrokeCount, data.Value.TotalKeystrokes / (double)data.Value.SessionCount),
                    (ItemManagerAverageScrolled, data.Value.UserEverScrolled / (double)data.Value.SessionCount),
                    (ItemManagerAverageSetFilters, data.Value.UserEverSetFilters / (double)data.Value.SessionCount),
                    (ItemManagerAverageBlockingComputationDuration, data.Value.TotalBlockingComputationTime / (double)data.Value.SessionCount),
                    (ItemManagerMaxBlockingComputationDuration, data.Value.MaxBlockingComputationTime)
                );
            }

            foreach (var data in SourceData)
            {
                if (data.Value.SetupCount == 0)
                    continue;
                if (data.Value.GetContextCount == 0)
                    data.Value.GetContextCount = 1; // the result of division will remain 0 and the division won't throw

                _logger.PostEvent(TelemetryEventType.Operation,
                    SourceEventName,
                    TelemetryResult.Success,
                    (SourceName, data.Key),
                    (SourceAverageGetContextDuration, data.Value.TotalGetContextTime / (double)data.Value.GetContextCount),
                    (SourceAverageSetupDuration, data.Value.TotalSetupTime / (double)data.Value.SetupCount),
                    (SourceMaxSetupDuration, data.Value.MaxSetupTime)
                );
            }

            foreach (var data in CommitManagerData)
            {
                if (data.Value.CommitCount == 0)
                    continue;

                _logger.PostEvent(TelemetryEventType.Operation,
                    CommitManagerEventName,
                    TelemetryResult.Success,
                    (CommitManagerName, data.Key),
                    (CommitManagerAverageCommitDuration, data.Value.TotalCommitTime / (double)data.Value.CommitCount),
                    (CommitManagerAverageSetupDuration, data.Value.TotalSetupTime / (double)data.Value.SetupCount),
                    (CommitManagerMaxCommitDuration, data.Value.MaxCommitTime),
                    (CommitManagerMaxSetupDuration, data.Value.MaxSetupTime)
                );
            }

            foreach (var data in PresenterData)
            {
                if (data.Value.RenderCount == 0)
                    continue;

                _logger.PostEvent(TelemetryEventType.Operation,
                    PresenterEventName,
                    TelemetryResult.Success,
                    (PresenterName, data.Key),
                    (PresenterAverageInitialRendering, data.Value.InitialRenderTime / (double)data.Value.ClosingCount),
                    (PresenterAverageRendering, data.Value.TotalRenderTime / (double)data.Value.RenderCount),
                    (PresenterAverageClosing, data.Value.TotalClosingTime / (double)data.Value.ClosingCount),
                    (PresenterMaxRendering, data.Value.MaxRenderTime),
                    (PresenterMaxClosing, data.Value.MaxClosingTime)
                );
            }

            _logger.PostEvent(TelemetryEventType.Operation,
                E2EEventName,
                TelemetryResult.Success,
                (E2EContentType, _textViewContentType),
                (E2EBucket25, E2EData.HistogramBucket25),
                (E2EBucket50, E2EData.HistogramBucket50),
                (E2EBucket100, E2EData.HistogramBucket100),
                (E2EBucket250, E2EData.HistogramBucket250),
                (E2EBucket500, E2EData.HistogramBucket500),
                (E2EBucket1000, E2EData.HistogramBucket1000),
                (E2EBucket2000, E2EData.HistogramBucket2000),
                (E2EBucketLast, E2EData.HistogramBucketLast),
                (E2EBucketCanceled, E2EData.HistogramBucketCanceled),
                (E2EBucketInvalid, E2EData.HistogramBucketInvalid),
                (E2ECommittedStandard, E2EData.Committed),
                (E2ECommittedClick, E2EData.CommittedThroughClick),
                (E2ECommittedCompleteWord, E2EData.CommittedThroughCompleteWord),
                (E2ECommittedSuggestionItem, E2EData.CommittedSuggestionItem),
                (E2ECommittedThroughTypedChar, E2EData.CommittedThroughTypedChar),
                (E2EDismissedStandard, E2EData.Dismissed),
                (E2EDismissedBackspace, E2EData.DismissedDueToBackspace),
                (E2EDismissedCancellation, E2EData.DismissedDueToCancellation),
                (E2EDismissedCaretLeaving, E2EData.DismissedDueToCaretLeaving),
                (E2EDismissedFiltering, E2EData.DismissedDuringFiltering),
                (E2EDismissedNoItems, E2EData.DismissedDueToNoItems),
                (E2EDismissedNonBlocking, E2EData.DismissedDueToNonBlockingMode),
                (E2EDismissedResponsive, E2EData.DismissedDueToResponsiveMode),
                (E2EDismissedSuggestion, E2EData.DismissedDueToSuggestionMode),
                (E2EDismissedUnhandledError, E2EData.DismissedDueToUnhandledError),
                (E2EDismissedUI, E2EData.DismissedThroughUI),
                (E2EDismissedUninitialized, E2EData.DismissedUninitialized),
                (E2EScenarioNoChanges, E2EData.HistogramNoChanges),
                (E2EScenarioUserWaitedForNoChanges, E2EData.HistogramNoChangesAndUserWaited)
            );

            foreach (var data in BlockingExtensionData)
            {
                if (data.Value == 0)
                    continue;

                _logger.PostEvent(TelemetryEventType.Operation,
                    BlockingExtensionEventName,
                    TelemetryResult.Success,
                    (BlockingExtensionName, data.Key),
                    (BlockingCount, data.Value)
                );
            }
        }

        /// <summary>
        /// Tracks obtaining applicable span and getting items
        /// </summary>
        /// <param name="telemetry">Telemetry from <see cref="IAsyncCompletionSession"/></param>
        /// <param name="sourceData">Data aggregator</param>
        private static void AddSourceData(CompletionSessionTelemetry telemetry, Dictionary<string, AggregateSourceData> sourceData)
        {
            foreach (var setupData in telemetry.ItemSourceSetupDuration)
            {
                if (!sourceData.ContainsKey(setupData.Key))
                    sourceData[setupData.Key] = new AggregateSourceData();
                var aggregateSourceData = sourceData[setupData.Key];

                aggregateSourceData.TotalSetupTime += setupData.Value;
                aggregateSourceData.SetupCount++;

                aggregateSourceData.MaxSetupTime = Math.Max(aggregateSourceData.MaxSetupTime, setupData.Value);
            }

            foreach (var getContextData in telemetry.ItemSourceGetContextDuration)
            {
                if (!sourceData.ContainsKey(getContextData.Key))
                    sourceData[getContextData.Key] = new AggregateSourceData();
                var aggregateSourceData = sourceData[getContextData.Key];

                aggregateSourceData.TotalGetContextTime += getContextData.Value;
                aggregateSourceData.GetContextCount++;
            }
        }

        /// <summary>
        /// Tracks sorting and filtering items
        /// </summary>
        /// <param name="telemetry">Telemetry from <see cref="IAsyncCompletionSession"/></param>
        /// <param name="sourceData">Data aggregator</param>
        private static void AddItemManagerData(CompletionSessionTelemetry telemetry, Dictionary<string, AggregateItemManagerData> itemManagerData)
        {
            var itemManagerKey = telemetry.ItemManagerName;
            if (!itemManagerData.ContainsKey(itemManagerKey))
                itemManagerData[itemManagerKey] = new AggregateItemManagerData();
            var aggregateItemManagerData = itemManagerData[itemManagerKey];

            aggregateItemManagerData.InitialProcessTime += telemetry.InitialProcessingDuration;
            aggregateItemManagerData.TotalProcessTime += telemetry.TotalProcessingDuration;
            aggregateItemManagerData.TotalBlockingComputationTime += telemetry.BlockingComputationDuration;
            aggregateItemManagerData.ProcessCount += telemetry.TotalProcessingCount;
            aggregateItemManagerData.TotalKeystrokes += telemetry.NumberOfKeystrokes;
            aggregateItemManagerData.UserEverScrolled += telemetry.UserEverScrolled ? 1 : 0;
            aggregateItemManagerData.UserEverSetFilters += telemetry.UserEverSetFilters ? 1 : 0;
            aggregateItemManagerData.FinalItemCount += telemetry.FinalItemCount;
            aggregateItemManagerData.SessionCount++;

            aggregateItemManagerData.MaxBlockingComputationTime = Math.Max(aggregateItemManagerData.MaxBlockingComputationTime, telemetry.BlockingComputationDuration);
        }

        /// <summary>
        /// Tracks obtaining commit characters and committing
        /// </summary>
        /// <param name="telemetry">Telemetry from <see cref="IAsyncCompletionSession"/></param>
        /// <param name="sourceData">Data aggregator</param>
        private static void AddCommitManagerData(CompletionSessionTelemetry telemetry, Dictionary<string, AggregateCommitManagerData> commitManagerData)
        {
            var commitKey = telemetry.CommitManagerName;
            if (!string.IsNullOrEmpty(commitKey))
            {
                // commitKey is empty when session is dismissed without committing.
                if (!commitManagerData.ContainsKey(commitKey))
                    commitManagerData[commitKey] = new AggregateCommitManagerData();
                var aggregateCommitManagerData = commitManagerData[commitKey];

                aggregateCommitManagerData.TotalCommitTime += telemetry.CommitDuration;
                aggregateCommitManagerData.CommitCount++;

                aggregateCommitManagerData.MaxCommitTime = Math.Max(aggregateCommitManagerData.MaxCommitTime, telemetry.CommitDuration);
            }

            foreach (var commitManagerSetupData in telemetry.CommitManagerSetupDuration)
            {
                if (!commitManagerData.ContainsKey(commitManagerSetupData.Key))
                    commitManagerData[commitManagerSetupData.Key] = new AggregateCommitManagerData();
                var aggregateCommitManagerData = commitManagerData[commitManagerSetupData.Key];

                aggregateCommitManagerData.TotalSetupTime += commitManagerSetupData.Value;
                aggregateCommitManagerData.SetupCount++;

                aggregateCommitManagerData.MaxSetupTime = Math.Max(aggregateCommitManagerData.MaxSetupTime, commitManagerSetupData.Value);
            }
        }

        /// <summary>
        /// Tracks opening, updating and closing the GUI
        /// </summary>
        /// <param name="telemetry">Telemetry from <see cref="IAsyncCompletionSession"/></param>
        /// <param name="sourceData">Data aggregator</param>
        private static void AddPresenterData(CompletionSessionTelemetry telemetry, Dictionary<string, AggregatePresenterData> presenterData)
        {
            var presenterKey = telemetry.PresenterProviderName;
            if (!presenterData.ContainsKey(presenterKey))
                presenterData[presenterKey] = new AggregatePresenterData();
            var aggregatePresenterData = presenterData[presenterKey];

            aggregatePresenterData.InitialRenderTime += telemetry.InitialRenderingDuration;
            aggregatePresenterData.TotalRenderTime += telemetry.TotalRenderingDuration;
            aggregatePresenterData.RenderCount += telemetry.TotalRenderingCount;
            aggregatePresenterData.TotalClosingTime += telemetry.ClosingDuration;
            aggregatePresenterData.ClosingCount++;

            aggregatePresenterData.MaxRenderTime = Math.Max(aggregatePresenterData.MaxRenderTime, telemetry.InitialRenderingDuration);
            aggregatePresenterData.MaxClosingTime = Math.Max(aggregatePresenterData.MaxClosingTime, telemetry.ClosingDuration);
        }

        private static void AddE2EData(CompletionSessionTelemetry telemetry, AggregateE2EData e2eData)
        {
            switch (telemetry.CompletionState)
            {
                case CompletionSessionState.Committed:
                    e2eData.Committed++;
                    break;
                case CompletionSessionState.CommittedSuggestionItem:
                    e2eData.CommittedSuggestionItem++;
                    break;
                case CompletionSessionState.CommittedThroughClick:
                    e2eData.CommittedThroughClick++;
                    break;
                case CompletionSessionState.CommittedThroughCompleteWord:
                    e2eData.CommittedThroughCompleteWord++;
                    break;
                case CompletionSessionState.CommittedThroughTypedChar:
                    e2eData.CommittedThroughTypedChar++;
                    break;
                case CompletionSessionState.DismissedDueToBackspace:
                    e2eData.DismissedDueToBackspace++;
                    break;
                case CompletionSessionState.DismissedDueToCancellation:
                    e2eData.DismissedDueToCancellation++;
                    break;
                case CompletionSessionState.DismissedDueToCaretLeaving:
                    e2eData.DismissedDueToCaretLeaving++;
                    break;
                case CompletionSessionState.DismissedDuringFiltering:
                    e2eData.DismissedDuringFiltering++;
                    break;
                case CompletionSessionState.DismissedDueToNoItems:
                    e2eData.DismissedDueToNoItems++;
                    break;
                case CompletionSessionState.DismissedDueToNonBlockingMode:
                    e2eData.DismissedDueToNonBlockingMode++;
                    break;
                case CompletionSessionState.DismissedDueToResponsiveMode:
                    e2eData.DismissedDueToResponsiveMode++;
                    break;
                case CompletionSessionState.DismissedDueToSuggestionMode:
                    e2eData.DismissedDueToSuggestionMode++;
                    break;
                case CompletionSessionState.DismissedDueToUnhandledError:
                    e2eData.DismissedDueToUnhandledError++;
                    break;
                case CompletionSessionState.DismissedThroughUI:
                    e2eData.DismissedThroughUI++;
                    break;
                case CompletionSessionState.DismissedUninitialized:
                    e2eData.DismissedUninitialized++;
                    break;
                default:
                    e2eData.Dismissed++;
                    break;
            }

            if (telemetry.CompletionState == CompletionSessionState.DismissedDueToCancellation)
            {
                e2eData.HistogramBucketCanceled++;
            }
            else
            {
                var E2eDuration = telemetry.E2EStopwatch.ElapsedMilliseconds;
                if (E2eDuration == 0)
                    e2eData.HistogramBucketInvalid++;
                else if (E2eDuration <= 25)
                    e2eData.HistogramBucket25++;
                else if (E2eDuration <= 50)
                    e2eData.HistogramBucket50++;
                else if (E2eDuration <= 100)
                    e2eData.HistogramBucket100++;
                else if (E2eDuration <= 250)
                    e2eData.HistogramBucket250++;
                else if (E2eDuration <= 500)
                    e2eData.HistogramBucket500++;
                else if (E2eDuration <= 1000)
                    e2eData.HistogramBucket1000++;
                else if (E2eDuration <= 2000)
                    e2eData.HistogramBucket2000++;
                else
                    e2eData.HistogramBucketLast++;
            }

            if (telemetry.CompletionState == CompletionSessionState.Committed || telemetry.CompletionState == CompletionSessionState.CommittedThroughTypedChar || telemetry.CompletionState == CompletionSessionState.CommittedThroughCompleteWord)
            {
                if (telemetry.NoChanges)
                {
                    if (telemetry.CompletionState == CompletionSessionState.CommittedThroughTypedChar && telemetry.UserWaitedForNoChanges)
                        e2eData.HistogramNoChangesAndUserWaitedThroughTypedChar++;
                    else if (telemetry.CompletionState == CompletionSessionState.CommittedThroughTypedChar && !telemetry.UserWaitedForNoChanges)
                        e2eData.HistogramNoChangesThroughTypedChar++;
                    else if (telemetry.CompletionState == CompletionSessionState.CommittedThroughTypedChar && telemetry.UserWaitedForNoChanges)
                        e2eData.HistogramNoChangesAndUserWaited++;
                    else if (telemetry.CompletionState == CompletionSessionState.CommittedThroughTypedChar && !telemetry.UserWaitedForNoChanges)
                        e2eData.HistogramNoChanges++;
                }
                else
                {
                    if (telemetry.CompletionState == CompletionSessionState.CommittedThroughTypedChar && telemetry.UserWaitedForNoChanges)
                        e2eData.HistogramChangesAndUserWaitedThroughTypedChar++;
                    else if (telemetry.CompletionState == CompletionSessionState.CommittedThroughTypedChar && !telemetry.UserWaitedForNoChanges)
                        e2eData.HistogramChangesThroughTypedChar++;
                    else if (telemetry.CompletionState != CompletionSessionState.CommittedThroughTypedChar && telemetry.UserWaitedForNoChanges)
                        e2eData.HistogramChangesAndUserWaited++;
                    else if (telemetry.CompletionState != CompletionSessionState.CommittedThroughTypedChar && !telemetry.UserWaitedForNoChanges)
                        e2eData.HistogramChanges++;
                }
            }
        }

        private static void AddBlockingExtensionData(CompletionSessionTelemetry telemetry, Dictionary<string, int> blockingExtensionData)
        {
            foreach (var blockingExtension in telemetry.BlockingExtensionCounter)
            {
                if (!blockingExtensionData.ContainsKey(blockingExtension.Key))
                    blockingExtensionData[blockingExtension.Key] = 0;
                blockingExtensionData[blockingExtension.Key] += blockingExtension.Value;
            }
        }

        // Property and event names
        internal const string PresenterEventName = "VS/Editor/Completion/PresenterData";
        internal const string PresenterName = "Property.Presenter.Name";
        internal const string PresenterAverageInitialRendering = "Property.Presenter.InitialRenderDuration";
        internal const string PresenterAverageRendering = "Property.Presenter.AllRenderDuration";
        internal const string PresenterAverageClosing = "Property.Presenter.AllClosingDuration";
        internal const string PresenterMaxRendering = "Property.Presenter.MaxRenderDuration";
        internal const string PresenterMaxClosing = "Property.Presenter.MaxClosingDuration";

        internal const string ItemManagerEventName = "VS/Editor/Completion/ItemManagerData";
        internal const string ItemManagerName = "Property.ItemManager.Name";
        internal const string ItemManagerAverageFinalItemCount = "Property.ItemManager.FinalItemCount";
        internal const string ItemManagerAverageInitialProcessDuration = "Property.ItemManager.InitialDuration";
        internal const string ItemManagerAverageFilterDuration = "Property.ItemManager.AnyDuration";
        internal const string ItemManagerAverageKeystrokeCount = "Property.ItemManager.KeystrokeCount";
        internal const string ItemManagerAverageScrolled = "Property.ItemManager.Scrolled";
        internal const string ItemManagerAverageSetFilters = "Property.ItemManager.SetFilters";
        internal const string ItemManagerAverageBlockingComputationDuration = "Property.ItemManager.BlockingComputationDuration";
        internal const string ItemManagerMaxBlockingComputationDuration = "Property.ItemManager.MaxBlockingComputationDuration";

        internal const string CommitManagerEventName = "VS/Editor/Completion/CommitManagerData";
        internal const string CommitManagerName = "Property.CommitManager.Name";
        internal const string CommitManagerAverageCommitDuration = "Property.Commit.CommitDuration";
        internal const string CommitManagerAverageSetupDuration = "Property.Commit.SetupDuration";
        internal const string CommitManagerMaxCommitDuration = "Property.Commit.MaxCommitDuration";
        internal const string CommitManagerMaxSetupDuration = "Property.Commit.MaxSetupDuration";

        internal const string SourceEventName = "VS/Editor/Completion/SourceData";
        internal const string SourceName = "Property.Source.Name";
        internal const string SourceAverageGetContextDuration = "Property.Source.GetContextDuration";
        internal const string SourceAverageSetupDuration = "Property.Source.SetupDuration";
        internal const string SourceMaxSetupDuration = "Property.Source.MaxSetupDuration";

        internal const string BlockingExtensionEventName = "VS/Editor/Completion/BlockingExtensionData";
        internal const string BlockingExtensionName = "Property.Extension.Name";
        internal const string BlockingCount = "Property.Extension.GetBlockingCount";

        internal const string E2EEventName = "VS/Editor/Completion/E2EData";
        internal const string E2EContentType = "Property.E2E.ContentType";
        internal const string E2EBucket25 = "Property.E2E.Bucket.25";
        internal const string E2EBucket50 = "Property.E2E.Bucket.50";
        internal const string E2EBucket100 = "Property.E2E.Bucket.100";
        internal const string E2EBucket250 = "Property.E2E.Bucket.250";
        internal const string E2EBucket500 = "Property.E2E.Bucket.500";
        internal const string E2EBucket1000 = "Property.E2E.Bucket.1000";
        internal const string E2EBucket2000 = "Property.E2E.Bucket.2000";
        internal const string E2EBucketLast = "Property.E2E.Bucket.Last";
        internal const string E2EBucketCanceled = "Property.E2E.Bucket.Canceled";
        internal const string E2EBucketInvalid = "Property.E2E.Bucket.Invalid";
        internal const string E2ECommittedStandard = "Property.E2E.Committed.Standard";
        internal const string E2ECommittedClick = "Property.E2E.Committed.ThroughClick";
        internal const string E2ECommittedCompleteWord = "Property.E2E.Committed.CompleteWord";
        internal const string E2ECommittedSuggestionItem = "Property.E2E.Committed.SuggestionItem";
        internal const string E2ECommittedThroughTypedChar = "Property.E2E.Committed.TypedChar";
        internal const string E2EDismissedStandard = "Property.E2E.Dismissed.Standard";
        internal const string E2EDismissedBackspace = "Property.E2E.Dismissed.Backspace";
        internal const string E2EDismissedCancellation = "Property.E2E.Dismissed.Cancellation";
        internal const string E2EDismissedCaretLeaving = "Property.E2E.Dismissed.CaretLeaving";
        internal const string E2EDismissedFiltering = "Property.E2E.Dismissed.Filtering";
        internal const string E2EDismissedNoItems = "Property.E2E.Dismissed.NoItems";
        internal const string E2EDismissedNonBlocking = "Property.E2E.Dismissed.NonBlocking";
        internal const string E2EDismissedResponsive = "Property.E2E.Dismissed.Responsive";
        internal const string E2EDismissedSuggestion = "Property.E2E.Dismissed.Suggestion";
        internal const string E2EDismissedUnhandledError = "Property.E2E.Dismissed.UnhandledError";
        internal const string E2EDismissedUI = "Property.E2E.Dismissed.UI";
        internal const string E2EDismissedUninitialized = "Property.E2E.Dismissed.Uninitialized";
        internal const string E2EScenarioNoChanges = "Property.E2E.Scenario.NoChanges";
        internal const string E2EScenarioUserWaitedForNoChanges = "Property.E2E.Scenario.UserWaitedForNoChanges";
    }

    /// <summary>
    /// Represents state of the session at the end of its life.
    /// It is set during the operation of the session and published when session dismisses.
    /// </summary>
    internal enum CompletionSessionState
    {
        /// <summary>
        /// Nothing significant has happened
        /// </summary>
        Default,
        /// <summary>
        /// Session committed through enter, tab or programmatically. Excludes committing through typed char.
        /// </summary>
        Committed,
        /// <summary>
        /// Session committed by double clicking the UI
        /// </summary>
        CommittedThroughClick,
        /// <summary>
        /// Session committed through complete word gesture (Ctrl+Space)
        /// </summary>
        CommittedThroughCompleteWord,
        /// <summary>
        /// Session committed when typing suggestion item
        /// </summary>
        CommittedSuggestionItem,
        /// <summary>
        /// Session committed through typing
        /// </summary>
        CommittedThroughTypedChar,
        /// <summary>
        /// Session dismissed because user erased its contents
        /// </summary>
        DismissedDueToBackspace,
        /// <summary>
        /// Session dismissed because user moved caret outside of the applicable to span
        /// </summary>
        DismissedDueToCaretLeaving,
        /// <summary>
        /// Session dismissed because a cancellation token was canceled
        /// </summary>
        DismissedDueToCancellation,
        /// <summary>
        /// Session dismissed because there was an issue filtering
        /// </summary>
        DismissedDuringFiltering,
        /// <summary>
        /// Session dismissed because there was no item to commit
        /// </summary>
        DismissedDueToNoItems,
        /// <summary>
        /// Session dismissed because computation has not finished before attempt to commit
        /// </summary>
        DismissedDueToNonBlockingMode,
        /// <summary>
        /// Session dismissed because computation has not finished within grace period before attempt to commit
        /// </summary>
        DismissedDueToResponsiveMode,
        /// <summary>
        /// Session dismissed because it was in suggestion mode and user did not use tab to commit it
        /// </summary>
        DismissedDueToSuggestionMode,
        /// <summary>
        /// Session dismissed because an error brought down the computation
        /// </summary>
        DismissedDueToUnhandledError,
        /// <summary>
        /// Session dismissed because UI closed, likely by losing focus
        /// </summary>
        DismissedThroughUI,
        /// <summary>
        /// Session dismissed because it never received completion data
        /// </summary>
        DismissedUninitialized,
    }
}
