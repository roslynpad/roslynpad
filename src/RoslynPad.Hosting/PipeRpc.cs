using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace RoslynPad.Hosting
{
    internal class PipeRpcClient : IDisposable
    {
        private readonly NamedPipeClientStream _stream;
        private readonly DataContractSerializer _serializer;
        private readonly CancellationTokenSource _cts;

        private PipeRpcClient(string name, IEnumerable<Type> knownTypes)
        {
            _stream = new NamedPipeClientStream(".", name, PipeDirection.InOut, PipeOptions.Asynchronous | PipeOptions.WriteThrough);
            _serializer = new DataContractSerializer(typeof(object), knownTypes);
            _cts = new CancellationTokenSource();
        }

        public async Task<object> ReadMessageAsync()
        {
            var buffer = ArrayPool<byte>.Shared.Rent(1024);

            try
            {
                while (true)
                {
                    var read = await _stream.ReadAsync(buffer, 0, buffer.Length, _cts.Token).ConfigureAwait(false);

                    if (!_stream.IsMessageComplete)
                    {
                        var oldBuffer = buffer;
                        buffer = ArrayPool<byte>.Shared.Rent(oldBuffer.Length * 2);
                        Buffer.BlockCopy(oldBuffer, 0, buffer, 0, read);

                        ArrayPool<byte>.Shared.Return(oldBuffer);
                    }
                    else
                    {
                        break;
                    }
                }

                using (var reader = XmlDictionaryReader.CreateBinaryReader(buffer, XmlDictionaryReaderQuotas.Max))
                {
                    return _serializer.ReadObject(reader);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        public static async Task<PipeRpcClient> CreateAsync(string name, IEnumerable<Type> knownTypes)
        {
            var client = new PipeRpcClient(name, knownTypes);

            try
            {
                await client._stream.ConnectAsync().ConfigureAwait(false);
                client._stream.ReadMode = PipeTransmissionMode.Message;
            }
            catch
            {
                client.Dispose();
                throw;
            }

            return client;
        }

        public void Dispose()
        {
            _stream.Dispose();
            _cts.Cancel();
            _cts.Dispose();
        }
    }

    internal class PipeRpcServer
    {
        private readonly NamedPipeServerStream _stream;
        private readonly DataContractSerializer _serializer;
        private readonly CancellationTokenSource _cts;

        private PipeRpcServer(string name, IEnumerable<Type> knownTypes)
        {
            _stream = new NamedPipeServerStream(name, PipeDirection.InOut, 1, PipeTransmissionMode.Message, PipeOptions.Asynchronous | PipeOptions.WriteThrough);
            _serializer = new DataContractSerializer(typeof(object), knownTypes);
            _cts = new CancellationTokenSource();
        }

        public async Task WriteMessageAsync<T>(T message)
        {
            using (var stream = new PooledWriteOnlyStream())
            using (var writer = XmlDictionaryWriter.CreateBinaryWriter(stream))
            {
                _serializer.WriteObject(writer, message);

                await _stream.WriteAsync(stream.Buffer, 0, (int)stream.Length).ConfigureAwait(false);
            }
        }

        public async Task Start()
        {
            await _stream.WaitForConnectionAsync().ConfigureAwait(false);
        }

        public static PipeRpcServer Create(string name, IEnumerable<Type> knownTypes)
        {
            return new PipeRpcServer(name, knownTypes);
        }

        public void Dispose()
        {
            _stream.Dispose();
            _cts.Cancel();
            _cts.Dispose();
        }
    }

    internal class PooledWriteOnlyStream : Stream
    {
        private int _position;
        private int _length;
        private int _capacity;
        private byte[] _buffer;

        public byte[] Buffer => _buffer;

        public PooledWriteOnlyStream()
        {
            EnsureCapacity(1024);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            ReleaseBuffer();
        }

        private void ReleaseBuffer()
        {
            ArrayPool<byte>.Shared.Return(_buffer);
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            var i = _position + count;
            // Check for overflow
            if (i < 0)
                throw new IOException("StreamTooLong");

            if (i > _length)
            {
                var mustZero = _position > _length;
                if (i > _capacity)
                {
                    var allocatedNewArray = EnsureCapacity(i);
                    if (allocatedNewArray)
                    {
                        mustZero = false;
                    }
                }

                if (mustZero)
                {
                    Array.Clear(_buffer, _length, i - _length);
                }

                _length = i;
            }
            if ((count <= 8) && (buffer != _buffer))
            {
                var byteCount = count;
                while (--byteCount >= 0)
                {
                    _buffer[_position + byteCount] = buffer[offset + byteCount];
                }
            }
            else
            {
                System.Buffer.BlockCopy(buffer, offset, _buffer, _position, count);
            }

            _position = i;
        }

        private const int MaxByteArrayLength = 2147483591;

        private bool EnsureCapacity(int value)
        {
            // Check for overflow
            if (value < 0)
                throw new IOException("StreamTooLong");
            if (value > _capacity)
            {
                var newCapacity = value;
                if (newCapacity < 256)
                {
                    newCapacity = 256;
                }

                // We are ok with this overflowing since the next statement will deal
                // with the cases where _capacity*2 overflows.
                if (newCapacity < _capacity * 2)
                {
                    newCapacity = _capacity * 2;
                }

                // We want to expand the array up to Array.MaxArrayLengthOneDimensional
                // And we want to give the user the value that they asked for
                if ((uint)(_capacity * 2) > MaxByteArrayLength)
                {
                    newCapacity = value > MaxByteArrayLength ? value : MaxByteArrayLength;
                }

                _capacity = newCapacity;
                ReleaseBuffer();
                _buffer = ArrayPool<byte>.Shared.Rent(_capacity);

                return true;
            }
            return false;
        }

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => _length;

        public override long Position
        {
            get => _position;
            set => throw new NotSupportedException();
        }
    }
}