using System;
using System.IO.Pipes;
using System.Threading.Tasks;
using StreamJsonRpc;

namespace RoslynPad.Hosting
{
    public abstract class RpcClient : IDisposable
    {
        private readonly string _pipeName;

        private NamedPipeClientStream _stream;
        private JsonRpc _rpc;

        protected RpcClient(string pipeName)
        {
            _pipeName = pipeName;
        }

        public async Task Connect(TimeSpan timeout)
        {
            var stream = new NamedPipeClientStream(".", _pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
            try
            {
                await stream.ConnectAsync((int)timeout.TotalMilliseconds).ConfigureAwait(false);
            }
            catch
            {
                stream.Dispose();
                throw;
            }

            _stream = stream;
            _rpc = JsonRpc.Attach(stream, this);
            RpcServer.ChangeSerializationSettings(_rpc);
        }

        private JsonRpc Rpc => _rpc ?? throw new InvalidOperationException("Not connected");

        protected Task InvokeAsync(string targetName, object argument) => Rpc.InvokeAsync(targetName, argument);

        protected Task<TResult> InvokeAsync<TResult>(string targetName, object argument) => Rpc.InvokeAsync<TResult>(targetName, argument);

        public virtual void Dispose()
        {
            _stream?.Dispose();
            _rpc?.Dispose();
        }
    }
}