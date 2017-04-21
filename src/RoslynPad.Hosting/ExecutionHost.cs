using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Xml;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using RoslynPad.Roslyn;
using RoslynPad.Roslyn.Scripting;
using RoslynPad.Runtime;
using RoslynPad.Utilities;
using InteractiveAssemblyLoader = RoslynPad.Roslyn.Scripting.InteractiveAssemblyLoader;

namespace RoslynPad.Hosting
{
    internal class ExecutionHost : IDisposable
    {
        private const int MillisecondsTimeout = 5000;
        private const int MaxAttemptsToCreateProcess = 2;

        private static readonly ManualResetEventSlim _clientExited = new ManualResetEventSlim(false);

        private static Dispatcher _serverDispatcher;
        private static DelegatingTextWriter _outWriter;
        private static DelegatingTextWriter _errorWriter;

        private readonly string _initialWorkingDirectory;
        private readonly IEnumerable<string> _references;
        private readonly IEnumerable<string> _imports;
        private readonly NuGetConfiguration _nuGetConfiguration;
        private readonly bool _shadowCopyAssemblies;
        private readonly OptimizationLevel _optimizationLevel;
        private readonly bool _checkOverflow;
        private readonly bool _allowUnsafe;

        private LazyRemoteService _lazyRemoteService;
        private bool _disposed;

        public static void RunServer(string serverPort, string semaphoreName, int clientProcessId)
        {
            if (!AttachToClientProcess(clientProcessId))
            {
                return;
            }

            // Disables Windows Error Reporting for the process, so that the process fails fast.
            if (Environment.OSVersion.Version >= new Version(6, 1, 0, 0))
            {
                SetErrorMode(GetErrorMode() | ErrorMode.SEM_FAILCRITICALERRORS | ErrorMode.SEM_NOOPENFILEERRORBOX |
                             ErrorMode.SEM_NOGPFAULTERRORBOX);
            }

            ServiceHost serviceHost = null;
            try
            {
                using (var semaphore = Semaphore.OpenExisting(semaphoreName))
                {
                    serviceHost = new ServiceHost(typeof(Service));
                    serviceHost.AddServiceEndpoint(typeof(IService), CreateBinding(), GetAddress(serverPort));
                    serviceHost.Open();

                    _outWriter = CreateConsoleWriter();
                    Console.SetOut(_outWriter);
                    _errorWriter = CreateConsoleWriter();
                    Console.SetError(_errorWriter);
                    Debug.Listeners.Clear();
                    Debug.Listeners.Add(new ConsoleTraceListener());
                    Debug.AutoFlush = true;

                    using (var resetEvent = new ManualResetEventSlim(false))
                    {
                        var uiThread = new Thread(() =>
                        {
                            _serverDispatcher = Dispatcher.CurrentDispatcher;
                            // ReSharper disable once AccessToDisposedClosure
                            resetEvent.Set();
                            Dispatcher.Run();
                        });
                        uiThread.SetApartmentState(ApartmentState.STA);
                        uiThread.IsBackground = true;
                        uiThread.Start();
                        resetEvent.Wait();
                    }

                    semaphore.Release();
                }

                _clientExited.Wait();
            }
            finally
            {
                if (serviceHost?.State == CommunicationState.Opened)
                {
                    serviceHost.Close();
                }
            }

            // force exit even if there are foreground threads running:
            Environment.Exit(0);
        }

        private static bool AttachToClientProcess(int clientProcessId)
        {
            Process clientProcess;
            try
            {
                clientProcess = Process.GetProcessById(clientProcessId);
            }
            catch (ArgumentException)
            {
                return false;
            }

            clientProcess.EnableRaisingEvents = true;
            clientProcess.Exited += (o, e) =>
            {
                _clientExited.Set();
            };

            return clientProcess.IsAlive();
        }


        private static Uri GetAddress(string serverPort)
        {
            return new UriBuilder
            {
                Scheme = Uri.UriSchemeNetPipe,
                Path = serverPort
            }.Uri;
        }

        private static NetNamedPipeBinding CreateBinding()
        {
            return new NetNamedPipeBinding(NetNamedPipeSecurityMode.None)
            {
                ReceiveTimeout = TimeSpan.MaxValue,
                SendTimeout = TimeSpan.MaxValue,
                ReaderQuotas = XmlDictionaryReaderQuotas.Max,
                MaxReceivedMessageSize = int.MaxValue
            };
        }

        private static DelegatingTextWriter CreateConsoleWriter()
        {
            return new DelegatingTextWriter(line => line.Dump());
        }

        public ExecutionHost(string hostPath, string initialWorkingDirectory,
            IEnumerable<string> references, IEnumerable<string> imports,
            NuGetConfiguration nuGetConfiguration, bool shadowCopyAssemblies = true,
            OptimizationLevel optimizationLevel = OptimizationLevel.Debug, bool checkOverflow = false, bool allowUnsafe = true)
        {
            HostPath = hostPath;
            _initialWorkingDirectory = initialWorkingDirectory;
            _references = references;
            _imports = imports;
            _nuGetConfiguration = nuGetConfiguration;
            _shadowCopyAssemblies = shadowCopyAssemblies;
            _optimizationLevel = optimizationLevel;
            _checkOverflow = checkOverflow;
            _allowUnsafe = allowUnsafe;
        }

        public string HostPath { get; set; }

        public event Action<IList<ResultObject>> Dumped;

        private void OnDumped(IList<ResultObject> results)
        {
            Dumped?.Invoke(results);
        }

        private RemoteService TryStartProcess(CancellationToken cancellationToken)
        {
            Process newProcess = null;
            int newProcessId = -1;
            Semaphore semaphore = null;
            try
            {
                string semaphoreName;
                while (true)
                {
                    semaphoreName = "HostSemaphore-" + Guid.NewGuid();
                    bool semaphoreCreated;
                    semaphore = new Semaphore(0, 1, semaphoreName, out semaphoreCreated);

                    if (semaphoreCreated)
                    {
                        break;
                    }

                    semaphore.Close();
                    cancellationToken.ThrowIfCancellationRequested();
                }

                var currentProcessId = Process.GetCurrentProcess().Id;

                var remoteServerPort = "HostChannel-" + Guid.NewGuid();

                var processInfo = new ProcessStartInfo(HostPath)
                {
                    Arguments = remoteServerPort + " " + semaphoreName + " " + currentProcessId,
                    WorkingDirectory = _initialWorkingDirectory,
                    CreateNoWindow = true,
                    UseShellExecute = false
                };

                newProcess = new Process { StartInfo = processInfo };
                newProcess.Start();

                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    newProcessId = newProcess.Id;
                }
                catch
                {
                    newProcessId = 0;
                }

                // sync:
                while (!semaphore.WaitOne(MillisecondsTimeout))
                {
                    if (!newProcess.IsAlive())
                    {
                        return null;
                    }
                    cancellationToken.ThrowIfCancellationRequested();
                }

                // instantiate remote service
                IService newService;
                try
                {
                    newService = DuplexChannelFactory<IService>.CreateChannel(
                        new InstanceContext(new ServiceCallback(OnDumped)),
                        CreateBinding(),
                        new EndpointAddress(GetAddress(remoteServerPort)));

                    // ReSharper disable once SuspiciousTypeConversion.Global
                    ((ICommunicationObject)newService).Open();

                    cancellationToken.ThrowIfCancellationRequested();

                    newService.Initialize(_references.ToArray(), _imports.ToArray(), _nuGetConfiguration,
                        _initialWorkingDirectory, _shadowCopyAssemblies,
                        _optimizationLevel, _checkOverflow, _allowUnsafe);
                }
                catch (CommunicationException) when (!newProcess.IsAlive())
                {
                    return null;
                }

                return new RemoteService(newProcess, newProcessId, newService);
            }
            catch (OperationCanceledException)
            {
                if (newProcess != null)
                {
                    RemoteService.InitiateTermination(newProcess, newProcessId);
                }

                return null;
            }
            finally
            {
                semaphore?.Close();
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(ExecutionHost));
            }
        }

        public void Dispose()
        {
            _disposed = true;
            _lazyRemoteService?.Dispose();
            _lazyRemoteService = null;
        }

        private async Task<IService> TryGetOrCreateRemoteServiceAsync()
        {
            ThrowIfDisposed();

            try
            {
                var currentRemoteService = _lazyRemoteService;

                for (var attempt = 0; attempt < MaxAttemptsToCreateProcess; attempt++)
                {
                    if (currentRemoteService == null)
                    {
                        return null;
                    }

                    var initializedService = await currentRemoteService.InitializedService.Value.ConfigureAwait(false);
                    if (initializedService != null && initializedService.Process.IsAlive())
                    {
                        return initializedService.Service;
                    }

                    // Service failed to start or initialize or the process died.
                    var newService = new LazyRemoteService(this);

                    var previousService = Interlocked.CompareExchange(ref _lazyRemoteService, newService,
                        currentRemoteService);
                    if (previousService == currentRemoteService)
                    {
                        // we replaced the service whose process we know is dead:
                        currentRemoteService.Dispose();
                        currentRemoteService = newService;
                    }
                    else
                    {
                        // the process was reset in between our checks, try to use the new service:
                        newService.Dispose();
                        currentRemoteService = previousService;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // The user reset the process during initialization. 
                // The reset operation will recreate the process.
            }
            return null;
        }

        public async Task<ExceptionResultObject> ExecuteAsync(string code)
        {
            var service = await TryGetOrCreateRemoteServiceAsync().ConfigureAwait(false);
            if (service == null)
            {
                throw new InvalidOperationException("Unable to create host process");
            }
            return await service.ExecuteAsync(code).ConfigureAwait(false);
        }

        public async Task CompileAndSave(string code, string assemblyPath)
        {
            var service = await TryGetOrCreateRemoteServiceAsync().ConfigureAwait(false);
            if (service == null)
            {
                throw new InvalidOperationException("Unable to create host process");
            }
            await service.CompileAndSave(code, assemblyPath).ConfigureAwait(false);
        }

        public async Task ResetAsync()
        {
            // replace the existing service with a new one:
            var newService = new LazyRemoteService(this);

            var oldService = Interlocked.Exchange(ref _lazyRemoteService, newService);
            oldService?.Dispose();

            await TryGetOrCreateRemoteServiceAsync().ConfigureAwait(false);
        }

        [ServiceContract]
        internal interface IServiceCallback
        {
            [OperationContract]
            Task Dump(IList<ResultObject> result);
        }

        [ServiceContract(CallbackContract = typeof(IServiceCallback))]
        internal interface IService
        {
            [OperationContract]
            Task Initialize(IList<string> references, IList<string> imports, NuGetConfiguration nuGetConfiguration,
                string workingDirectory, bool shadowCopyAssemblies,
                OptimizationLevel optimizationLevel = OptimizationLevel.Debug, bool checkOverflow = false, bool allowUnsafe = true);

            [OperationContract]
            Task<ExceptionResultObject> ExecuteAsync(string code);

            [OperationContract]
            Task CompileAndSave(string code, string assemblyPath);
        }

        [CallbackBehavior(UseSynchronizationContext = false)]
        internal class ServiceCallback : IServiceCallback
        {
            private readonly Action<IList<ResultObject>> _dumped;

            public ServiceCallback(Action<IList<ResultObject>> dumped)
            {
                _dumped = dumped;
            }

            public Task Dump(IList<ResultObject> result)
            {
                _dumped?.Invoke(result);
                return Task.CompletedTask;
            }
        }

        [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Reentrant,
             UseSynchronizationContext = false)]
        internal class Service : IService, IDisposable
        {
            private const int WindowMillisecondsTimeout = 500;
            private const int WindowMaxCount = 10000;

            private static readonly ImmutableArray<string> SystemNoShadowCopyDirectories = GetSystemNoShadowCopyDirectories();

            private static ImmutableArray<string> GetSystemNoShadowCopyDirectories()
            {
                var paths = new List<string>
                {
                    Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    RuntimeEnvironment.GetRuntimeDirectory()
                };

                if (Environment.Is64BitOperatingSystem)
                {
                    paths.Add(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86));
                }

                return paths.ToImmutableArray();
            }

            private readonly ConcurrentQueue<ResultObject> _dumpQueue;
            private readonly SemaphoreSlim _dumpLock;

            private ScriptOptions _scriptOptions;
            private IServiceCallback _callbackChannel;
            private CSharpParseOptions _parseOptions;
            private string _workingDirectory;
            private bool _shadowCopyAssemblies;
            private OptimizationLevel _optimizationLevel;
            private bool _checkOverflow;
            private bool _allowUnsafe;

            public Service()
            {
                _dumpQueue = new ConcurrentQueue<ResultObject>();
                _dumpLock = new SemaphoreSlim(0);
                _scriptOptions = ScriptOptions.Default;

                ObjectExtensions.Dumped += OnDumped;
            }

            public Task Initialize(IList<string> references, IList<string> imports,
                NuGetConfiguration nuGetConfiguration, string workingDirectory, bool shadowCopyAssemblies,
                OptimizationLevel optimizationLevel = OptimizationLevel.Debug, bool checkOverflow = false, bool allowUnsafe = true)
            {
                _parseOptions = new CSharpParseOptions().WithPreprocessorSymbols("__DEMO__", "__DEMO_EXPERIMENTAL__");

                _workingDirectory = workingDirectory;

                var scriptOptions = _scriptOptions
                    .WithReferences(references)
                    .WithImports(imports);
                if (nuGetConfiguration != null)
                {
                    var resolver = new NuGetScriptMetadataResolver(nuGetConfiguration, workingDirectory);
                    scriptOptions = scriptOptions.WithMetadataResolver(resolver);
                }
                _scriptOptions = scriptOptions;

                _shadowCopyAssemblies = shadowCopyAssemblies;
                _optimizationLevel = optimizationLevel;
                _checkOverflow = checkOverflow;
                _allowUnsafe = allowUnsafe;

                _callbackChannel = OperationContext.Current.GetCallbackChannel<IServiceCallback>();

                return Task.CompletedTask;
            }

            private void OnDumped(object o, string header)
            {
                EnqueueResult(ResultObject.Create(o, header));
            }

            private void EnqueueResult(ResultObject resultObject)
            {
                _dumpQueue.Enqueue(resultObject);
                _dumpLock.Release();
            }

            private async Task ProcessDumpQueue(CancellationToken cancellationToken)
            {
                while (true)
                {
                    // ReSharper disable once MethodSupportsCancellation
                    var hasItem = await _dumpLock.WaitAsync(WindowMillisecondsTimeout).ConfigureAwait(false);
                    if (!hasItem)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            return;
                        }
                        continue;
                    }

                    var list = new List<ResultObject>();
                    var timestamp = Environment.TickCount;
                    ResultObject item;
                    while (Environment.TickCount - timestamp < WindowMillisecondsTimeout &&
                           list.Count < WindowMaxCount &&
                           _dumpQueue.TryDequeue(out item))
                    {
                        if (list.Count > 0)
                        {
                            // ReSharper disable once MethodSupportsCancellation
                            await _dumpLock.WaitAsync().ConfigureAwait(false);
                        }
                        list.Add(item);
                    }

                    if (list.Count > 0)
                    {
                        try
                        {
                            var task = _callbackChannel?.Dump(list);
                            if (task != null)
                            {
                                await task.ConfigureAwait(false);
                            }
                        }
                        catch
                        {
                            // ignored
                        }
                    }
                }
                // ReSharper disable once FunctionNeverReturns
            }

            public void Dispose()
            {
                ObjectExtensions.Dumped -= OnDumped;
            }

            public async Task CompileAndSave(string code, string assemblyPath)
            {
                var processCancelSource = new CancellationTokenSource();
                var processCancelToken = processCancelSource.Token;
                // ReSharper disable once MethodSupportsCancellation
                var processTask = Task.Run(() => ProcessDumpQueue(processCancelToken));

                var outputKind = string.Equals(Path.GetExtension(assemblyPath), ".exe",
                    StringComparison.OrdinalIgnoreCase)
                    ? OutputKind.ConsoleApplication
                    : OutputKind.DynamicallyLinkedLibrary;

                var platform = !Environment.Is64BitProcess &&
                               (outputKind == OutputKind.ConsoleApplication ||
                                outputKind == OutputKind.WindowsApplication)
                    ? Platform.AnyCpu32BitPreferred
                    : Platform.AnyCpu;

                try
                {
                    var script = CreateScript(code, _scriptOptions, outputKind, platform);
                    // ReSharper disable once MethodSupportsCancellation
                    if (script != null)
                    {
                        var diagnostics = await script.SaveAssembly(assemblyPath).ConfigureAwait(false);
                        DisplayErrors(diagnostics);
                    }
                }
                catch (Exception e)
                {
                    ReportUnhandledException(e);
                }
                finally
                {
                    _outWriter.Flush();
                    _errorWriter.Flush();

                    processCancelSource.Cancel();
                    await processTask.ConfigureAwait(false);
                }
            }

            public async Task<ExceptionResultObject> ExecuteAsync(string code)
            {
                Debug.Assert(code != null);

                var processCancelSource = new CancellationTokenSource();
                var processCancelToken = processCancelSource.Token;
                // ReSharper disable once MethodSupportsCancellation
                var processTask = Task.Run(() => ProcessDumpQueue(processCancelToken));

                try
                {
                    var script = TryCompile(code, _scriptOptions);
                    if (script != null)
                    {
                        var result = await ExecuteOnUIThread(script).ConfigureAwait(false);
                        var errorResult = result as ExceptionResultObject;
                        if (errorResult == null && result != null)
                        {
                            DisplaySubmissionResult(result);
                        }
                        return errorResult;
                    }
                }
                catch (Exception e)
                {
                    ReportUnhandledException(e);
                }
                finally
                {
                    _outWriter.Flush();
                    _errorWriter.Flush();

                    processCancelSource.Cancel();
                    await processTask.ConfigureAwait(false);
                }
                return null;
            }

            private ScriptRunner TryCompile(string code, ScriptOptions options)
            {
                var script = CreateScript(code, options);

                var diagnostics = script.Compile();
                if (diagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error))
                {
                    DisplayErrors(diagnostics);
                    return null;
                }

                return script;
            }

            private ScriptRunner CreateScript(string code, ScriptOptions options, OutputKind outputKind = OutputKind.DynamicallyLinkedLibrary, Platform platform = Platform.AnyCpu)
            {
                var script = new ScriptRunner(code, _parseOptions, outputKind, platform, 
                    options.MetadataReferences, options.Imports,
                    options.FilePath, _workingDirectory, options.MetadataResolver,
                    assemblyLoader: _shadowCopyAssemblies 
                        ? new InteractiveAssemblyLoader(
                            new MetadataShadowCopyProvider(Path.GetTempPath(), SystemNoShadowCopyDirectories)) 
                        : null,
                    optimizationLevel: _optimizationLevel,
                    checkOverflow: _checkOverflow,
                    allowUnsafe: _allowUnsafe
                    );

                return script;
            }

            private static void DisplayErrors(ImmutableArray<Diagnostic> diagnostics)
            {
                foreach (var diagnostic in diagnostics)
                {
                    diagnostic.Dump();
                }
            }

            private static void DisplaySubmissionResult(object state)
            {
                // TODO
                //if (state.Script.GetCompilation().HasSubmissionResult())
                if (state != null)
                {
                    state.Dump();
                }
            }

            private async Task<object> ExecuteOnUIThread(ScriptRunner script)
            {
                return await (await _serverDispatcher.InvokeAsync(
                    async () =>
                    {
                        try
                        {
                            var task = script.RunAsync();
                            return await task.ConfigureAwait(false);
                        }
                        catch (FileLoadException e) when (e.InnerException is NotSupportedException)
                        {
                            Console.Error.WriteLine(e.InnerException.Message);
                            return null;
                        }
                        catch (Exception e)
                        {
                            return ExceptionResultObject.Create(e);
                        }
                    })).ConfigureAwait(false);
            }

            private static void ReportUnhandledException(Exception e)
            {
                e.Dump();
                Debug.Fail("Unexpected error");
            }
        }

        internal sealed class RemoteService : IDisposable
        {
            public readonly Process Process;
            // ReSharper disable once MemberHidesStaticFromOuterClass
            public readonly IService Service;
            private readonly int _processId;

            internal RemoteService(Process process, int processId, IService service)
            {
                Debug.Assert(process != null);
                Debug.Assert(service != null);

                Process = process;
                _processId = processId;
                Service = service;
            }

            public void Dispose()
            {
                InitiateTermination(Process, _processId);
            }

            internal static void InitiateTermination(Process process, int processId)
            {
                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill();
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine($"HostProcess: can't terminate process {processId}: {e.Message}");
                }
            }
        }

        private sealed class LazyRemoteService : IDisposable
        {
            public readonly Lazy<Task<RemoteService>> InitializedService;
            private readonly CancellationTokenSource _cancellationSource;
            private readonly ExecutionHost _host;

            public LazyRemoteService(ExecutionHost host)
            {
                _cancellationSource = new CancellationTokenSource();
                InitializedService = new Lazy<Task<RemoteService>>(TryStartAndInitializeProcessAsync);
                _host = host;
            }

            public void Dispose()
            {
                // Cancel the creation of the process if it is in progress.
                // If it is the cancellation will clean up all resources allocated during the creation.
                _cancellationSource.Cancel();

                // If the value has been calculated already, dispose the service.
                if (InitializedService.IsValueCreated && InitializedService.Value.Status == TaskStatus.RanToCompletion)
                {
                    InitializedService.Value.Result?.Dispose();
                }
            }

            private Task<RemoteService> TryStartAndInitializeProcessAsync()
            {
                var cancellationToken = _cancellationSource.Token;
                return Task.Run(() => _host.TryStartProcess(cancellationToken), cancellationToken);
            }
        }

        #region Win32 API

        [DllImport("kernel32", PreserveSig = true)]
        internal static extern ErrorMode SetErrorMode(ErrorMode mode);

        [DllImport("kernel32", PreserveSig = true)]
        internal static extern ErrorMode GetErrorMode();

        [Flags]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        internal enum ErrorMode
        {
            SEM_FAILCRITICALERRORS = 0x0001,

            SEM_NOGPFAULTERRORBOX = 0x0002,

            SEM_NOALIGNMENTFAULTEXCEPT = 0x0004,

            SEM_NOOPENFILEERRORBOX = 0x8000,
        }

        #endregion
    }
}