using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using Mono.Cecil;
using RoslynPad.Hosting.ILDecompiler;
using RoslynPad.Roslyn;
using RoslynPad.Roslyn.Scripting;
using RoslynPad.Runtime;
using RoslynPad.Utilities;

namespace RoslynPad.Hosting
{
    internal class ExecutionHost : IDisposable
    {
        private readonly InitializationParameters _initializationParameters;
        private const int MillisecondsTimeout = 5000;
        private const int MaxAttemptsToCreateProcess = 2;

        private static readonly ManualResetEventSlim _clientExited = new ManualResetEventSlim(false);

        private static DelegatingTextWriter _outWriter;
        private static DelegatingTextWriter _errorWriter;

        private LazyRemoteService _lazyRemoteService;
        private bool _disposed;

        private static bool Is64BitProcess => IntPtr.Size == 8;

        public static void RunServer(string serverPort, string semaphoreName, int clientProcessId)
        {
            if (!AttachToClientProcess(clientProcessId))
            {
                return;
            }

            DisableWer();

            ServerImpl server = null;
            try
            {
                var executionThread = CreateExecutionThread();

                server = new ServerImpl(serverPort, executionThread.syncContext);
                server.Start();

                _outWriter = CreateConsoleWriter();
                Console.SetOut(_outWriter);
                _errorWriter = CreateConsoleWriter();
                Console.SetError(_errorWriter);

                // TODO: fix debug capturing
                //Debug.Listeners.Clear();
                //Debug.Listeners.Add(new ConsoleTraceListener());
                //Debug.AutoFlush = true;

                _clientExited.Wait();
                executionThread.complete();
            }
            finally
            {
                server?.Dispose();
            }

            // force exit even if there are foreground threads running
            Exit?.Invoke(0);
        }

        private static (SynchronizationContext syncContext, Action complete) CreateExecutionThread()
        {
#if NET46
            var tcs = new TaskCompletionSource<SynchronizationContext>();
            var executionThread = new Thread(() =>
            {
                System.Windows.Threading.Dispatcher.CurrentDispatcher.InvokeAsync(() =>
                    tcs.TrySetResult(SynchronizationContext.Current));
                System.Windows.Threading.Dispatcher.Run();
            });

            executionThread.SetApartmentState(ApartmentState.STA);
            executionThread.IsBackground = true;
            executionThread.Start();

            var syncContext = tcs.Task.Result;
            return (syncContext, () => syncContext.Post(o => 
                System.Windows.Threading.Dispatcher.CurrentDispatcher.InvokeShutdown(), null));
#else
            var syncContext = new AsyncPump.SingleThreadSynchronizationContext(false);
            var executionThread = new Thread(() =>
            {
                SynchronizationContext.SetSynchronizationContext(syncContext);
                syncContext.RunOnCurrentThread();
            });

            var setApartmentState = typeof(Thread).GetRuntimeMethods()
                .FirstOrDefault(m => m.Name == "SetApartmentState");
            if (setApartmentState != null)
            {
                setApartmentState.Invoke(executionThread, new object[] {0});
            }
            
            executionThread.IsBackground = true;
            executionThread.Start();
            return (syncContext, syncContext.Complete);
#endif
        }

        // Environment.Exit is not part of netstandard1.3...
        private static readonly Action<int> Exit = (Action<int>)typeof(Environment)
            .GetRuntimeMethod("Exit", new[] { typeof(int) })
            ?.CreateDelegate(typeof(Action<int>));

        /// <summary>
        /// Disables Windows Error Reporting for the process, so that the process fails fast.
        /// </summary>
        private static void DisableWer()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                try
                {
                    SetErrorMode(GetErrorMode() | ErrorMode.SEM_FAILCRITICALERRORS | ErrorMode.SEM_NOOPENFILEERRORBOX |
                                 ErrorMode.SEM_NOGPFAULTERRORBOX);
                }
                catch
                {
                    // ignored
                }
            }
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

        private static DelegatingTextWriter CreateConsoleWriter()
        {
            return new DelegatingTextWriter(line => line.Dump());
        }

        public ExecutionHost(InitializationParameters initializationParameters)
        {
            _initializationParameters = initializationParameters;
        }

        public string HostPath { get; set; }

        public string HostArguments { get; set; }


        public event Action<IList<ResultObject>> Dumped;

        private void OnDumped(IList<ResultObject> results) => Dumped?.Invoke(results);

        public event Action<ExceptionResultObject> Error;

        private void OnError(ExceptionResultObject error) => Error?.Invoke(error);

        public event Action<List<CompilationErrorResultObject>> CompilationErrors;

        private void OnCompilationErrors(List<CompilationErrorResultObject> errors) => CompilationErrors?.Invoke(errors);

        public event Action<string> Disassembled;

        private void OnDisassembled(string il) => Disassembled?.Invoke(il);

        private async Task<RemoteService> TryStartProcess(CancellationToken cancellationToken)
        {
            Process newProcess = null;
            int newProcessId = -1;
            try
            {
                var currentProcessId = Process.GetCurrentProcess().Id;

                var remotePort = "RoslynPad-" + Guid.NewGuid();

                var processInfo = new ProcessStartInfo(HostPath)
                {
                    Arguments = $"{HostArguments} {remotePort} 0 {currentProcessId}",
                    WorkingDirectory = _initializationParameters.WorkingDirectory,
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

                if (!newProcess.IsAlive())
                {
                    return null;
                }

                cancellationToken.ThrowIfCancellationRequested();

                ClientImpl client = null;
                // instantiate remote service
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    client = new ClientImpl(remotePort, this);
                    await client.Connect(TimeSpan.FromSeconds(4)).ConfigureAwait(false);
                    await client.Initialize(new InitializationMessage { Parameters = _initializationParameters })
                        .ConfigureAwait(false);
                }
                catch (Exception)
                {
                    client?.Dispose();
                    return null;
                }

                return new RemoteService(newProcess, newProcessId, client);
            }
            catch (OperationCanceledException)
            {
                if (newProcess != null)
                {
                    RemoteService.InitiateTermination(newProcess, newProcessId);
                }

                return null;
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

        public async Task ExecuteAsync(string code, bool disassemble, OptimizationLevel? optimizationLevel)
        {
            var service = await TryGetOrCreateRemoteServiceAsync().ConfigureAwait(false);
            if (service == null)
            {
                throw new InvalidOperationException("Unable to create host process");
            }
            await service.ExecuteAsync(new ExecuteMessage { Code = code, Disassemble = disassemble, OptimizationLevel = optimizationLevel }).ConfigureAwait(false);
        }

        public async Task CompileAndSave(string code, string assemblyPath, OptimizationLevel? optimizationLevel)
        {
            var service = await TryGetOrCreateRemoteServiceAsync().ConfigureAwait(false);
            if (service == null)
            {
                throw new InvalidOperationException("Unable to create host process");
            }
            await service.CompileAndSave(new CompileAndSaveMessage { AssemblyPath = assemblyPath, Code = code, OptimizationLevel = optimizationLevel }).ConfigureAwait(false);
        }

        public async Task ResetAsync()
        {
            // replace the existing service with a new one:
            var newService = new LazyRemoteService(this);

            var oldService = Interlocked.Exchange(ref _lazyRemoteService, newService);
            oldService?.Dispose();

            await TryGetOrCreateRemoteServiceAsync().ConfigureAwait(false);
        }

        [DataContract]
        private class DumpMessage
        {
            [DataMember]
            public IList<ResultObject> Results { get; set; }
        }

        [DataContract]
        private class ErrorMessage
        {
            [DataMember]
            public ExceptionResultObject Error { get; set; }
        }

        [DataContract]
        private class InitializationMessage
        {
            [DataMember]
            public InitializationParameters Parameters { get; set; }
        }

        [DataContract]
        private class ExecuteMessage
        {
            [DataMember]
            public string Code { get; set; }

            [DataMember]
            public bool Disassemble { get; set; }

            [DataMember]
            public OptimizationLevel? OptimizationLevel { get; set; }
        }

        [DataContract]
        private class CompileAndSaveMessage
        {
            [DataMember]
            public string Code { get; set; }

            [DataMember]
            public string AssemblyPath { get; set; }

            [DataMember]
            public OptimizationLevel? OptimizationLevel { get; set; }
        }

        [DataContract]
        private class DisassembledMesssage
        {
            [DataMember]
            public string IL { get; set; }
        }

        private interface IServiceCallback
        {
            Task Dump(DumpMessage message);
            Task Error(ErrorMessage message);
            Task Disassembled(DisassembledMesssage message);
            Task CompilationErrors(List<CompilationErrorResultObject> errors);
        }

        private interface IService
        {
            Task Initialize(InitializationMessage message);
            Task CompileAndSave(CompileAndSaveMessage message);
            Task ExecuteAsync(ExecuteMessage message);
        }

        private class ServerImpl : RpcServer, IService, IServiceCallback
        {
            private const int WindowMillisecondsTimeout = 500;
            private const int WindowMaxCount = 10000;

            private static readonly ImmutableArray<string> SystemNoShadowCopyDirectories = GetSystemNoShadowCopyDirectories();

            private static ImmutableArray<string> GetSystemNoShadowCopyDirectories()
            {
                var paths = new List<string>
                {
                    Path.GetDirectoryName(typeof(object).GetTypeInfo().Assembly.GetLocation())
                };

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    paths.Add(Environment.GetEnvironmentVariable("windir"));
                    paths.Add(Environment.GetEnvironmentVariable("ProgramFiles"));

                    if (RuntimeInformation.OSArchitecture == Architecture.X64 ||
                        RuntimeInformation.OSArchitecture == Architecture.Arm64)
                    {
                        paths.Add(Environment.GetEnvironmentVariable("ProgramFiles(x86)"));
                    }
                }

                return paths.ToImmutableArray();
            }

            private readonly ConcurrentQueue<ResultObject> _dumpQueue;
            private readonly SynchronizationContext _syncContext;
            private readonly SemaphoreSlim _dumpLock;

            private ScriptOptions _scriptOptions;
            private CSharpParseOptions _parseOptions;
            private string _workingDirectory;
            private bool _shadowCopyAssemblies;
            private OptimizationLevel _optimizationLevel;
            private bool _checkOverflow;
            private bool _allowUnsafe;

            public ServerImpl(string pipeName, SynchronizationContext syncContext) : base(pipeName)
            {
                _dumpQueue = new ConcurrentQueue<ResultObject>();
                _dumpLock = new SemaphoreSlim(0);
                _scriptOptions = ScriptOptions.Default;

                ObjectExtensions.Dumped += OnDumped;
                this._syncContext = syncContext;
            }

            public Task Dump(DumpMessage message)
            {
                return InvokeAsync(nameof(Dump), message);
            }

            public Task Error(ErrorMessage message)
            {
                return InvokeAsync(nameof(Error), message);
            }

            public Task Disassembled(DisassembledMesssage message)
            {
                return InvokeAsync(nameof(Disassembled), message);
            }

            public Task CompilationErrors(List<CompilationErrorResultObject> errors)
            {
                return InvokeAsync(nameof(CompilationErrors), errors);
            }

            public Task Initialize(InitializationMessage message)
            {
                _parseOptions = new CSharpParseOptions(preprocessorSymbols: new[] { "__DEMO__", "__DEMO_EXPERIMENTAL__" }, languageVersion: LanguageVersion.Latest);

                var initializationParameters = message.Parameters;

                _workingDirectory = initializationParameters.WorkingDirectory;

                var scriptOptions = _scriptOptions
                    .WithReferences(initializationParameters.References)
                    .WithImports(initializationParameters.Imports);
                if (initializationParameters.NuGetConfiguration != null)
                {
                    var resolver = new NuGetScriptMetadataResolver(initializationParameters.NuGetConfiguration, initializationParameters.WorkingDirectory);
                    scriptOptions = scriptOptions.WithMetadataResolver(resolver);
                }
                _scriptOptions = scriptOptions;

                _shadowCopyAssemblies = initializationParameters.ShadowCopyAssemblies;
                _optimizationLevel = initializationParameters.OptimizationLevel;
                _checkOverflow = initializationParameters.CheckOverflow;
                _allowUnsafe = initializationParameters.AllowUnsafe;

                return Task.CompletedTask;
            }

            private void OnDumped(DumpData data)
            {
                EnqueueResult(ResultObject.Create(data.Object, data.Quotas, data.Header));
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
                    while (Environment.TickCount - timestamp < WindowMillisecondsTimeout &&
                           list.Count < WindowMaxCount &&
                           _dumpQueue.TryDequeue(out var item))
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
                            await Dump(new DumpMessage { Results = list }).ConfigureAwait(false);
                        }
                        catch
                        {
                            // ignored
                        }
                    }
                }
                // ReSharper disable once FunctionNeverReturns
            }

            public override void Dispose()
            {
                ObjectExtensions.Dumped -= OnDumped;
            }

            public async Task CompileAndSave(CompileAndSaveMessage message)
            {
                var processCancelSource = new CancellationTokenSource();
                var processCancelToken = processCancelSource.Token;
                // ReSharper disable once MethodSupportsCancellation
                var processTask = Task.Run(() => ProcessDumpQueue(processCancelToken));

                var outputKind = string.Equals(Path.GetExtension(message.AssemblyPath), ".exe",
                    StringComparison.OrdinalIgnoreCase)
                    ? OutputKind.ConsoleApplication
                    : OutputKind.DynamicallyLinkedLibrary;

                var platform = !Is64BitProcess &&
                               (outputKind == OutputKind.ConsoleApplication ||
                                outputKind == OutputKind.WindowsApplication)
                    ? Platform.AnyCpu32BitPreferred
                    : Platform.AnyCpu;

                try
                {
                    var script = CreateScript(message.Code, message.OptimizationLevel, _scriptOptions, outputKind, platform);
                    // ReSharper disable once MethodSupportsCancellation
                    if (script != null)
                    {
                        var diagnostics = await script.SaveAssembly(message.AssemblyPath).ConfigureAwait(false);
                        await DisplayErrors(diagnostics).ConfigureAwait(false);
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

            public async Task ExecuteAsync(ExecuteMessage message)
            {
                Debug.Assert(message?.Code != null);

                var processCancelSource = new CancellationTokenSource();
                var processCancelToken = processCancelSource.Token;
                // ReSharper disable once MethodSupportsCancellation
                var processTask = Task.Run(() => ProcessDumpQueue(processCancelToken));

                try
                {
                    var script = await TryCompile(message.Code, message.Disassemble, message.OptimizationLevel, _scriptOptions).ConfigureAwait(false);
                    if (script != null)
                    {
                        var result = await PostToExecutionThread(script).ConfigureAwait(false);
                        var errorResult = result as ExceptionResultObject;
                        if (errorResult == null)
                        {
                            if (result != null)
                            {
                                DisplaySubmissionResult(result);
                            }
                        }
                        else
                        {
                            await Error(new ErrorMessage { Error = errorResult }).ConfigureAwait(false);
                        }
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

            private async Task<ScriptRunner> TryCompile(string code, bool decompile, OptimizationLevel? optimizationLevel, ScriptOptions options)
            {
                var script = CreateScript(code, optimizationLevel, options);

                var diagnostics = script.Compile(decompile ? (Action<Stream>)Disassemble : null);
                if (diagnostics.Any())
                {
                    await DisplayErrors(diagnostics).ConfigureAwait(false);
                }

                return diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error) ? null : script;
            }

            private void Disassemble(Stream peStream)
            {
                using (var assembly = AssemblyDefinition.ReadAssembly(peStream))
                {
                    var output = new PlainTextOutput();
                    var disassembler = new ReflectionDisassembler(output, false, CancellationToken.None);
                    disassembler.WriteModuleContents(assembly.MainModule);
                    Disassembled(new DisassembledMesssage { IL = output.ToString() });
                }
            }

            private ScriptRunner CreateScript(string code, OptimizationLevel? optimizationLevel, ScriptOptions options, OutputKind outputKind = OutputKind.DynamicallyLinkedLibrary, Platform platform = Platform.AnyCpu)
            {
                var script = new ScriptRunner(code, _parseOptions, outputKind, platform,
                    options.MetadataReferences, options.Imports,
                    options.FilePath, _workingDirectory, options.MetadataResolver,
                    assemblyLoader: _shadowCopyAssemblies
                        ? new InteractiveAssemblyLoader(
                            new MetadataShadowCopyProvider(Path.GetTempPath(), SystemNoShadowCopyDirectories))
                        : null,
                    optimizationLevel: optimizationLevel ?? _optimizationLevel,
                    checkOverflow: _checkOverflow,
                    allowUnsafe: _allowUnsafe
                );

                return script;
            }

            private async Task DisplayErrors(ImmutableArray<Diagnostic> diagnostics)
            {
                var errors = new List<CompilationErrorResultObject>();

                foreach (var diagnostic in diagnostics)
                {
                    var lineSpan = diagnostic.Location.GetLineSpan();

                    var error = CompilationErrorResultObject.Create(diagnostic.Severity.ToString(),
                        diagnostic.Id, diagnostic.GetMessage(),
                        lineSpan.StartLinePosition.Line, lineSpan.StartLinePosition.Character);

                    errors.Add(error);
                }

                await CompilationErrors(errors).ConfigureAwait(false);
            }

            private static void DisplaySubmissionResult(object state)
            {
                // TODO
                //if (state.Script.GetCompilation().HasSubmissionResult())
                state?.Dump();
            }

            private Task<object> PostToExecutionThread(ScriptRunner script)
            {
                var tcs = new TaskCompletionSource<object>();

                _syncContext.Post(async o =>
                {
                    var innerTcs = (TaskCompletionSource<object>)o;
                    try
                    {
                        innerTcs.TrySetResult(await script.RunAsync().ConfigureAwait(false));
                    }
                    catch (FileLoadException e) when (e.InnerException is NotSupportedException)
                    {
                        Console.Error.WriteLine(e.InnerException.Message);
                        innerTcs.TrySetResult(null);
                    }
                    catch (Exception e)
                    {
                        innerTcs.TrySetResult(ExceptionResultObject.Create(e));
                    }
                }, tcs);

                return tcs.Task;
            }

            private static void ReportUnhandledException(Exception e)
            {
                e.Dump();
            }
        }

        private class ClientImpl : RpcClient, IService, IServiceCallback
        {
            private readonly ExecutionHost _host;

            public ClientImpl(string pipeName, ExecutionHost host) : base(pipeName)
            {
                _host = host;
            }

            public Task Initialize(InitializationMessage message)
            {
                return InvokeAsync(nameof(Initialize), message);
            }

            public Task CompileAndSave(CompileAndSaveMessage message)
            {
                return InvokeAsync(nameof(CompileAndSave), message);
            }

            public Task ExecuteAsync(ExecuteMessage message)
            {
                return InvokeAsync(nameof(ExecuteAsync), message);
            }

            public Task Dump(DumpMessage message)
            {
                _host.OnDumped(message.Results);
                return Task.CompletedTask;
            }

            public Task Error(ErrorMessage message)
            {
                _host.OnError(message.Error);
                return Task.CompletedTask;
            }

            public Task Disassembled(DisassembledMesssage message)
            {
                _host.OnDisassembled(message.IL);
                return Task.CompletedTask;
            }

            public Task CompilationErrors(List<CompilationErrorResultObject> errors)
            {
                _host.OnCompilationErrors(errors);
                return Task.CompletedTask;
            }
        }

        private sealed class RemoteService : IDisposable
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
                using (Service as IDisposable) { }
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