using System.Collections;
using System.Diagnostics.Contracts;

namespace RoslynPad.Runtime;

public static class ObjectExtensions
{
    public static T Dump<T>(this T o, string? header = null, int maxDepth = DumpQuotas.DefaultMaxDepth, int maxExpandedDepth = DumpQuotas.DefaultMaxExpandedDepth, int maxEnumerableLength = DumpQuotas.DefaultMaxEnumerableLength, int maxStringLength = DumpQuotas.DefaultMaxStringLength)
    {
        Dumped?.Invoke(new DumpData(o, header, new DumpQuotas(maxDepth, maxExpandedDepth, maxEnumerableLength, maxStringLength)));
        return o;
    }

#if NET6_0_OR_GREATER
    public static Span<T> Dump<T>(this Span<T> o, string? header = null, int maxDepth = DumpQuotas.DefaultMaxDepth, int maxExpandedDepth = DumpQuotas.DefaultMaxExpandedDepth, int maxEnumerableLength = DumpQuotas.DefaultMaxEnumerableLength, int maxStringLength = DumpQuotas.DefaultMaxStringLength)
    {
        Dump(o.ToArray());
        return o;
    }

    public static ReadOnlySpan<T> Dump<T>(this ReadOnlySpan<T> o, string? header = null, int maxDepth = DumpQuotas.DefaultMaxDepth, int maxExpandedDepth = DumpQuotas.DefaultMaxExpandedDepth, int maxEnumerableLength = DumpQuotas.DefaultMaxEnumerableLength, int maxStringLength = DumpQuotas.DefaultMaxStringLength)
    {
        Dump(o.ToArray());
        return o;
    }

    public static Memory<T> Dump<T>(this Memory<T> o, string? header = null, int maxDepth = DumpQuotas.DefaultMaxDepth, int maxExpandedDepth = DumpQuotas.DefaultMaxExpandedDepth, int maxEnumerableLength = DumpQuotas.DefaultMaxEnumerableLength, int maxStringLength = DumpQuotas.DefaultMaxStringLength)
    {
        Dump(o.ToArray());
        return o;
    }

    public static ReadOnlyMemory<T> Dump<T>(this ReadOnlyMemory<T> o, string? header = null, int maxDepth = DumpQuotas.DefaultMaxDepth, int maxExpandedDepth = DumpQuotas.DefaultMaxExpandedDepth, int maxEnumerableLength = DumpQuotas.DefaultMaxEnumerableLength, int maxStringLength = DumpQuotas.DefaultMaxStringLength)
    {
        Dump(o.ToArray());
        return o;
    }
#endif

    public static T DumpAs<T, TResult>(this T o, Func<T, TResult>? selector, string? header = null, int maxDepth = DumpQuotas.DefaultMaxDepth, int maxExpandedDepth = DumpQuotas.DefaultMaxExpandedDepth, int maxEnumerableLength = DumpQuotas.DefaultMaxEnumerableLength, int maxStringLength = DumpQuotas.DefaultMaxStringLength)
    {
        Dump(selector != null ? (object?)selector.Invoke(o) : null, header, maxDepth, maxExpandedDepth, maxEnumerableLength, maxStringLength);
        return o;
    }

    public static TEnumerable DumpFirst<TEnumerable>(this TEnumerable enumerable, string? header = null, int maxDepth = DumpQuotas.DefaultMaxDepth, int maxExpandedDepth = DumpQuotas.DefaultMaxExpandedDepth, int maxEnumerableLength = DumpQuotas.DefaultMaxEnumerableLength, int maxStringLength = DumpQuotas.DefaultMaxStringLength)
        where TEnumerable : IEnumerable
    {
        Dump(enumerable?.Cast<object>().FirstOrDefault(), header, maxDepth, maxExpandedDepth, maxEnumerableLength, maxStringLength);
        return enumerable!;
    }

    public static TEnumerable DumpLast<TEnumerable>(this TEnumerable enumerable, string? header = null, int maxDepth = DumpQuotas.DefaultMaxDepth, int maxExpandedDepth = DumpQuotas.DefaultMaxExpandedDepth, int maxEnumerableLength = DumpQuotas.DefaultMaxEnumerableLength, int maxStringLength = DumpQuotas.DefaultMaxStringLength)
        where TEnumerable : IEnumerable
    {
        Dump(enumerable?.Cast<object>().LastOrDefault(), header, maxDepth, maxExpandedDepth, maxEnumerableLength, maxStringLength);
        return enumerable!;
    }

    public static TEnumerable DumpElementAt<TEnumerable>(this TEnumerable enumerable, int index, string? header = null, int maxDepth = DumpQuotas.DefaultMaxDepth, int maxExpandedDepth = DumpQuotas.DefaultMaxExpandedDepth, int maxEnumerableLength = DumpQuotas.DefaultMaxEnumerableLength, int maxStringLength = DumpQuotas.DefaultMaxStringLength)
        where TEnumerable : IEnumerable
    {
        Dump(enumerable?.Cast<object>().ElementAtOrDefault(index), header, maxDepth, maxExpandedDepth, maxEnumerableLength, maxStringLength);
        return enumerable!;
    }

    internal static event DumpDelegate? Dumped;

    internal delegate void DumpDelegate(in DumpData data);
}

internal readonly struct DumpData(object? o, string? header, DumpQuotas quotas)
{
    public object? Object { get; } = o;
    public string? Header { get; } = header;
    public DumpQuotas Quotas { get; } = quotas;
}

internal struct DumpQuotas(int maxDepth, int maxExpandedDepth, int maxEnumerableLength, int maxStringLength)
{
    internal const int DefaultMaxDepth = 4;
    internal const int DefaultMaxExpandedDepth = 1;
    internal const int DefaultMaxStringLength = 10000;
    internal const int DefaultMaxEnumerableLength = 10000;

    public int MaxDepth { get; } = maxDepth;
    public int MaxExpandedDepth { get; } = maxExpandedDepth;
    public int MaxEnumerableLength { get; } = maxEnumerableLength;
    public int MaxStringLength { get; } = maxStringLength;

    public static DumpQuotas Default { get; } = new DumpQuotas(DefaultMaxDepth, DefaultMaxExpandedDepth, DefaultMaxEnumerableLength, DefaultMaxStringLength);

    [Pure]
    internal DumpQuotas StepDown() =>
        new(MaxDepth - 1, MaxExpandedDepth - 1, MaxEnumerableLength, MaxStringLength);

    [Pure]
    internal DumpQuotas WithMaxDepth(int maxDepth) =>
        new(maxDepth, MaxExpandedDepth, MaxEnumerableLength, MaxStringLength);
}
