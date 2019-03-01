using System;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using StreamJsonRpc;
using Newtonsoft.Json;
using System.Linq;

namespace RoslynPad.Hosting
{
    public abstract class RpcServer : IDisposable
    {
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly NamedPipeServerStream _stream;
        private readonly Lazy<Task> _connectTask;
        private JsonRpc? _rpc;

        protected RpcServer(string pipeName)
        {
            _stream = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            _connectTask = new Lazy<Task>(async () =>
            {
                while (true)
                {
                    await _stream.WaitForConnectionAsync(_cts.Token).ConfigureAwait(false);
                    var rpc = JsonRpc.Attach(_stream, this);
                    ChangeSerializationSettings(rpc);
                    _rpc = rpc;
                }
            });
        }

        private static readonly PropertyInfo[] MessageJsonSerializerSettingsProperties = typeof(JsonRpc).GetRuntimeProperties()
            .Where(p => p.PropertyType == typeof(JsonSerializerSettings) && p.GetIndexParameters().Length == 0 && p.CanRead).ToArray();

        internal static void ChangeSerializationSettings(JsonRpc rpc)
        {
            rpc.JsonSerializer.DateParseHandling = DateParseHandling.None;
            foreach (var property in MessageJsonSerializerSettingsProperties)
            {
                var settings = (JsonSerializerSettings)property.GetValue(rpc);
                settings.DateParseHandling = DateParseHandling.None;
            }
        }

        public Task ConnectTask => _connectTask.Value;

        public void Start()
        {
            // ReSharper disable once UnusedVariable
            var task = _connectTask.Value;
        }

        protected async Task InvokeAsync(string targetName, object argument)
        {
            await _connectTask.Value.ConfigureAwait(false);
            var rpc = _rpc;
            if (rpc != null)
            {
                await rpc.InvokeAsync(targetName, argument).ConfigureAwait(false);
                return;
            }

            throw new InvalidOperationException("Not connected");
        }

        protected async Task<TResult> InvokeAsync<TResult>(string targetName, object argument)
        {
            await _connectTask.Value.ConfigureAwait(false);
            var rpc = _rpc;
            if (rpc != null)
            {
                return await rpc.InvokeAsync<TResult>(targetName, argument).ConfigureAwait(false);
            }

            throw new InvalidOperationException("Not connected");
        }

        public virtual void Dispose()
        {
            _cts.Cancel();
            if (_cts.IsCancellationRequested) return;

            _cts.Cancel();
            _rpc?.Dispose();

            _stream.Disconnect();
            _cts.Dispose();
            _stream.Dispose();
        }
    }
}