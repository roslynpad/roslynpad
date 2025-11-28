using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace RoslynPad.Build;

internal static class JsonHelpers
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SpanDisposer GetSpan(this scoped ref Utf8JsonReader reader)
    {
        if (!reader.HasValueSequence)
        {
            return new SpanDisposer(reader.ValueSpan);
        }

        var length = (int)reader.ValueSequence.Length;
        var array = ArrayPool<byte>.Shared.Rent(length);
        reader.ValueSequence.CopyTo(array);
        return new SpanDisposer(array.AsSpan(0, length), array);
    }

    public readonly ref struct SpanDisposer(ReadOnlySpan<byte> span, byte[]? array = null)
    {
        private readonly byte[]? _array = array;
        public readonly ReadOnlySpan<byte> Span = span;

        public void Dispose()
        {
            if (_array != null)
            {
                ArrayPool<byte>.Shared.Return(_array);
            }
        }
    }
}
