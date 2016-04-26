using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Serialization.Formatters;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using RoslynPad.Roslyn;
using RoslynPad.Runtime;
using RoslynPad.Utilities;

namespace RoslynPad.Host
{
    internal class ExecutionHost : MarshalByRefObject, IDisposable
    {
        private const int MillisecondsTimeout = 5000;
        private const int MaxAttemptsToCreateProcess = 2;

        private static Dispatcher _dispatcher;

        private readonly string _initialWorkingDirectory;
        private readonly IEnumerable<string> _references;
        private readonly IEnumerable<string> _imports;
        private readonly INuGetProvider _nuGetProvider;
        private readonly string _hostPath;
        private readonly ChildProcessManager _childProcessManager;

        private IpcServerChannel _serverChannel;
        private LazyRemoteService _lazyRemoteService;

        public static void RunServer(string serverPort, string semaphoreName)
        {
            // Disables Windows Error Reporting for the process, so that the process fails fast.
            if (Environment.OSVersion.Version >= new Version(6, 1, 0, 0))
            {
                SetErrorMode(GetErrorMode() | ErrorMode.SEM_FAILCRITICALERRORS | ErrorMode.SEM_NOOPENFILEERRORBOX | ErrorMode.SEM_NOGPFAULTERRORBOX);
            }

            IpcServerChannel serverChannel = null;
            IpcClientChannel clientChannel = null;
            try
            {
                using (var semaphore = Semaphore.OpenExisting(semaphoreName))
                {
                    var serverProvider = new BinaryServerFormatterSinkProvider { TypeFilterLevel = TypeFilterLevel.Full };
                    var clientProvider = new BinaryClientFormatterSinkProvider();

                    clientChannel = new IpcClientChannel(GenerateUniqueChannelLocalName(), clientProvider);
                    ChannelServices.RegisterChannel(clientChannel, ensureSecurity: false);

                    serverChannel = new IpcServerChannel(GenerateUniqueChannelLocalName(), serverPort, serverProvider);
                    ChannelServices.RegisterChannel(serverChannel, ensureSecurity: false);

                    RemotingConfiguration.RegisterWellKnownServiceType(
                        typeof(Service),
                        typeof(Service).Name,
                        WellKnownObjectMode.Singleton);

                    Console.SetOut(CreateConsoleWriter());
                    Console.SetError(CreateConsoleWriter());

                    using (var resetEvent = new ManualResetEventSlim(false))
                    {
                        var uiThread = new Thread(() =>
                        {
                            _dispatcher = Dispatcher.CurrentDispatcher;
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

                Thread.Sleep(Timeout.Infinite); // TODO
            }
            finally
            {
                if (serverChannel != null)
                {
                    ChannelServices.UnregisterChannel(serverChannel);
                }

                if (clientChannel != null)
                {
                    ChannelServices.UnregisterChannel(clientChannel);
                }
            }

            // force exit even if there are foreground threads running:
            Environment.Exit(0);
        }

        private static TextWriter CreateConsoleWriter()
        {
            return new DelegatingTextWriter(line => line.Dump());
        }

        public ExecutionHost(string hostPath, string initialWorkingDirectory,
            IEnumerable<string> references, IEnumerable<string> imports,
            INuGetProvider nuGetProvider, ChildProcessManager childProcessManager)
        {
            _hostPath = hostPath;
            _initialWorkingDirectory = initialWorkingDirectory;
            _references = references;
            _imports = imports;
            _nuGetProvider = nuGetProvider;
            _childProcessManager = childProcessManager;
            var serverProvider = new BinaryServerFormatterSinkProvider { TypeFilterLevel = TypeFilterLevel.Full };
            _serverChannel = new IpcServerChannel(GenerateUniqueChannelLocalName(), "Channel-" + Guid.NewGuid(), serverProvider);
            ChannelServices.RegisterChannel(_serverChannel, ensureSecurity: false);
        }

        public override object InitializeLifetimeService() => null;

        public event Action<ResultObject> Dumped;

        public void OnDumped(ResultObject o)
        {
            Dumped?.Invoke(o);
        }

        public event Action<int> ExecutionCompleted;

        public void OnExecutionCompleted(int token)
        {
            ExecutionCompleted?.Invoke(token);
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

                var remoteServerPort = "HostChannel-" + Guid.NewGuid();

                var processInfo = new ProcessStartInfo(_hostPath)
                {
                    Arguments = remoteServerPort + " " + semaphoreName,
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
                    _childProcessManager.AddProcess(newProcess);
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

                // instantiate remote service:
                Service newService;
                try
                {
                    newService = (Service)Activator.GetObject(
                        typeof(Service),
                        "ipc://" + remoteServerPort + "/" + nameof(Service));

                    cancellationToken.ThrowIfCancellationRequested();

                    newService.Initialize(_references.ToArray(), _imports.ToArray(), _nuGetProvider, _initialWorkingDirectory, OnDumped, OnExecutionCompleted);
                }
                catch (RemotingException) when (!newProcess.IsAlive())
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

        private static string GenerateUniqueChannelLocalName()
        {
            return typeof(Service).FullName + Guid.NewGuid();
        }

        public void Dispose()
        {
            if (_serverChannel != null)
            {
                ChannelServices.UnregisterChannel(_serverChannel);
                _serverChannel = null;
            }

            _lazyRemoteService?.Dispose();
            _lazyRemoteService = null;
        }

        private async Task<Service> TryGetOrCreateRemoteServiceAsync()
        {
            try
            {
                var currentRemoteService = _lazyRemoteService;

                // disposed or not reset:
                Debug.Assert(currentRemoteService != null);

                for (var attempt = 0; attempt < MaxAttemptsToCreateProcess; attempt++)
                {
                    var initializedService = await currentRemoteService.InitializedService.Value.ConfigureAwait(false);
                    if (initializedService != null && initializedService.Process.IsAlive())
                    {
                        return initializedService.Service;
                    }

                    // Service failed to start or initialize or the process died.
                    var newService = new LazyRemoteService(this);

                    var previousService = Interlocked.CompareExchange(ref _lazyRemoteService, newService, currentRemoteService);
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

        public async Task ExecuteAsync(string code, int token)
        {
            var service = await TryGetOrCreateRemoteServiceAsync().ConfigureAwait(false);
            if (service == null)
            {
                throw new InvalidOperationException("Unable to create host process");
            }
            service.ExecuteAsync(code, token);
        }

        public async Task ResetAsync()
        {
            // replace the existing service with a new one:
            var newService = new LazyRemoteService(this);

            var oldService = Interlocked.Exchange(ref _lazyRemoteService, newService);
            oldService?.Dispose();

            await TryGetOrCreateRemoteServiceAsync().ConfigureAwait(false);
        }

        internal class Service : MarshalByRefObject, IDisposable
        {
            private readonly object _lastTaskGuard;
            private ScriptOptions _scriptOptions;

            private Task _lastTask;
            private Action<ResultObject> _dumped;
            private Action<int> _completed;

            public Service()
            {
                _lastTaskGuard = new object();
                _lastTask = Task.CompletedTask;
                _scriptOptions = ScriptOptions.Default;

                ObjectExtensions.Dumped += OnDumped;
            }

            public override object InitializeLifetimeService() => null;

            public void Initialize(IList<string> references, IList<string> imports, INuGetProvider nuGetProvider, string workingDirectory, Action<ResultObject> dumped, Action<int> completed)
            {
                var scriptOptions = _scriptOptions
                    .WithReferences(references)
                    .WithImports(imports);
                if (nuGetProvider != null)
                {
                    var resolver = new NuGetScriptMetadataResolver(nuGetProvider, workingDirectory);
                    scriptOptions = scriptOptions.WithMetadataResolver(resolver);
                }
                _scriptOptions = scriptOptions;
                _dumped = dumped;
                _completed = completed;
            }

            private void OnDumped(object o, DumpTarget mode)
            {
                _dumped?.Invoke(ResultObject.Create(o));
            }

            public void Dispose()
            {
                ObjectExtensions.Dumped -= OnDumped;
            }

            [OneWay]
            public void ExecuteAsync(string code, int token)
            {
                Debug.Assert(code != null);

                lock (_lastTaskGuard)
                {
                    _lastTask = ExecuteAsync(_lastTask, code, token);
                }
            }

            private async Task ExecuteAsync(Task lastTask, string code, int token)
            {
                await ReportUnhandledExceptionIfAny(lastTask).ConfigureAwait(false);

                try
                {
                    var script = TryCompile(code, _scriptOptions);
                    if (script != null)
                    {
                        var scriptState = await ExecuteOnUIThread(script).ConfigureAwait(false);
                        if (scriptState != null)
                        {
                            DisplaySubmissionResult(scriptState);
                        }
                    }
                }
                catch (Exception e)
                {
                    ReportUnhandledException(e);
                }

                _completed?.Invoke(token);
            }

            private static Script<object> TryCompile(string code, ScriptOptions options)
            {
                var script = CSharpScript.Create<object>(code, options);

                var diagnostics = script.Compile();
                if (diagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error))
                {
                    DisplayErrors(diagnostics);
                    return null;
                }

                return script;
            }

            private static void DisplayErrors(ImmutableArray<Diagnostic> diagnostics)
            {
                foreach (var diagnostic in diagnostics)
                {
                    diagnostic.Dump();
                }
            }

            private static void DisplaySubmissionResult(ScriptState<object> state)
            {
                // TODO
                //if (state.Script.GetCompilation().HasSubmissionResult())
                if (state.ReturnValue != null)
                {
                    state.ReturnValue.Dump();
                }
            }

            private static async Task<ScriptState<object>> ExecuteOnUIThread(Script<object> script)
            {
                return await (await _dispatcher.InvokeAsync(
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
                            e.Dump();
                            return null;
                        }
                    })).ConfigureAwait(false);
            }

            private static async Task ReportUnhandledExceptionIfAny(Task lastTask)
            {
                try
                {
                    await lastTask.ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    ReportUnhandledException(e);
                }
            }

            private static void ReportUnhandledException(Exception e)
            {
                Console.Error.WriteLine("Unexpected error:");
                Console.Error.WriteLine(e);
                Debug.Fail("Unexpected error");
            }
        }

        internal sealed class RemoteService : IDisposable
        {
            public readonly Process Process;
            public readonly Service Service;
            private readonly int _processId;

            internal RemoteService(Process process, int processId, Service service)
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
                    Debug.WriteLine("HostProcess: can't terminate process {0}: {1}", processId, e.Message);
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
                    InitializedService.Value.Result.Dispose();
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