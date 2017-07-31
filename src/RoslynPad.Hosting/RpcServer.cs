using System;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using StreamJsonRpc;

namespace RoslynPad.Hosting
{
    public abstract class RpcServer : IDisposable
    {
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly NamedPipeServerStream _stream;
        private readonly Lazy<Task<JsonRpc>> _connectTask;

        protected RpcServer(string pipeName)
        {
            _stream = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            _connectTask = new Lazy<Task<JsonRpc>>(async () =>
            {
                await _stream.WaitForConnectionAsync(_cts.Token).ConfigureAwait(false);
                return JsonRpc.Attach(_stream, this);
            });
        }

        public Task ConnectTask => _connectTask.Value;

        public void Start()
        {
            // ReSharper disable once UnusedVariable
            var task = _connectTask.Value;
        }

        protected async Task InvokeAsync(string targetName, object argument) => 
            await (await _connectTask.Value.ConfigureAwait(false)).InvokeAsync(targetName, argument).ConfigureAwait(false);

        protected async Task<TResult> InvokeAsync<TResult>(string targetName, object argument) => 
            await (await _connectTask.Value.ConfigureAwait(false)).InvokeAsync<TResult>(targetName, argument).ConfigureAwait(false);


        public async Task StopAsync()
        {
            if (_cts.IsCancellationRequested) return;

            _cts.Cancel();

            if (_connectTask.IsValueCreated)
            {
                var value = await _connectTask.Value.ConfigureAwait(false);
                value.Dispose();
            }

            _stream.Disconnect();
        }

        public virtual void Dispose()
        {
            // ReSharper disable once UnusedVariable
            var task = StopAsync();
            _cts.Dispose();
            _stream.Dispose();
        }
    }
}