using System.Collections;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;

namespace RoslynPad.Runtime;

public static class ObjectExtensions
{
    public static T Dump<T>(this T o, [CallerArgumentExpression(nameof(o))] string? header = null, int maxDepth = DumpQuotas.DefaultMaxDepth, int maxExpandedDepth = DumpQuotas.DefaultMaxExpandedDepth, int maxEnumerableLength = DumpQuotas.DefaultMaxEnumerableLength, int maxStringLength = DumpQuotas.DefaultMaxStringLength, [CallerLineNumber] int? line = null)
    {
        Dumped?.Invoke(new DumpData(o, header, line, new DumpQuotas(maxDepth, maxExpandedDepth, maxEnumerableLength, maxStringLength)));
        return o;
    }

#if NET6_0_OR_GREATER
    public static Span<T> Dump<T>(this Span<T> o, [CallerArgumentExpression(nameof(o))] string? header = null, int maxDepth = DumpQuotas.DefaultMaxDepth, int maxExpandedDepth = DumpQuotas.DefaultMaxExpandedDepth, int maxEnumerableLength = DumpQuotas.DefaultMaxEnumerableLength, int maxStringLength = DumpQuotas.DefaultMaxStringLength, [CallerLineNumber] int? line = null)
    {
        Dump(o.ToArray(), header, maxDepth, maxExpandedDepth, maxEnumerableLength, maxStringLength, line);
        return o;
    }

    public static ReadOnlySpan<T> Dump<T>(this ReadOnlySpan<T> o, [CallerArgumentExpression(nameof(o))] string? header = null, int maxDepth = DumpQuotas.DefaultMaxDepth, int maxExpandedDepth = DumpQuotas.DefaultMaxExpandedDepth, int maxEnumerableLength = DumpQuotas.DefaultMaxEnumerableLength, int maxStringLength = DumpQuotas.DefaultMaxStringLength, [CallerLineNumber] int? line = null)
    {
        Dump(o.ToArray(), header, maxDepth, maxExpandedDepth, maxEnumerableLength, maxStringLength, line);
        return o;
    }

    public static Memory<T> Dump<T>(this Memory<T> o, [CallerArgumentExpression(nameof(o))] string? header = null, int maxDepth = DumpQuotas.DefaultMaxDepth, int maxExpandedDepth = DumpQuotas.DefaultMaxExpandedDepth, int maxEnumerableLength = DumpQuotas.DefaultMaxEnumerableLength, int maxStringLength = DumpQuotas.DefaultMaxStringLength, [CallerLineNumber] int? line = null)
    {
        Dump(o.ToArray(), header, maxDepth, maxExpandedDepth, maxEnumerableLength, maxStringLength, line);
        return o;
    }

    public static ReadOnlyMemory<T> Dump<T>(this ReadOnlyMemory<T> o, [CallerArgumentExpression(nameof(o))] string? header = null, int maxDepth = DumpQuotas.DefaultMaxDepth, int maxExpandedDepth = DumpQuotas.DefaultMaxExpandedDepth, int maxEnumerableLength = DumpQuotas.DefaultMaxEnumerableLength, int maxStringLength = DumpQuotas.DefaultMaxStringLength, [CallerLineNumber] int? line = null)
    {
        Dump(o.ToArray(), header, maxDepth, maxExpandedDepth, maxEnumerableLength, maxStringLength, line);
        return o;
    }
#endif

    public static T DumpAs<T, TResult>(this T o, Func<T, TResult>? selector, [CallerArgumentExpression(nameof(o))] string? header = null, int maxDepth = DumpQuotas.DefaultMaxDepth, int maxExpandedDepth = DumpQuotas.DefaultMaxExpandedDepth, int maxEnumerableLength = DumpQuotas.DefaultMaxEnumerableLength, int maxStringLength = DumpQuotas.DefaultMaxStringLength, [CallerLineNumber] int? line = null)
    {
        Dump(selector != null ? (object?)selector.Invoke(o) : null, header, maxDepth, maxExpandedDepth, maxEnumerableLength, maxStringLength, line);
        return o;
    }

    public static TEnumerable DumpFirst<TEnumerable>(this TEnumerable enumerable, [CallerArgumentExpression(nameof(enumerable))] string? header = null, int maxDepth = DumpQuotas.DefaultMaxDepth, int maxExpandedDepth = DumpQuotas.DefaultMaxExpandedDepth, int maxEnumerableLength = DumpQuotas.DefaultMaxEnumerableLength, int maxStringLength = DumpQuotas.DefaultMaxStringLength, [CallerLineNumber] int? line = null)
        where TEnumerable : IEnumerable
    {
        Dump(enumerable?.Cast<object>().FirstOrDefault(), header, maxDepth, maxExpandedDepth, maxEnumerableLength, maxStringLength, line);
        return enumerable!;
    }

    public static TEnumerable DumpLast<TEnumerable>(this TEnumerable enumerable, [CallerArgumentExpression(nameof(enumerable))] string? header = null, int maxDepth = DumpQuotas.DefaultMaxDepth, int maxExpandedDepth = DumpQuotas.DefaultMaxExpandedDepth, int maxEnumerableLength = DumpQuotas.DefaultMaxEnumerableLength, int maxStringLength = DumpQuotas.DefaultMaxStringLength, [CallerLineNumber] int? line = null)
        where TEnumerable : IEnumerable
    {
        Dump(enumerable?.Cast<object>().LastOrDefault(), header, maxDepth, maxExpandedDepth, maxEnumerableLength, maxStringLength, line);
        return enumerable!;
    }

    public static TEnumerable DumpElementAt<TEnumerable>(this TEnumerable enumerable, int index, [CallerArgumentExpression(nameof(enumerable))] string? header = null, int maxDepth = DumpQuotas.DefaultMaxDepth, int maxExpandedDepth = DumpQuotas.DefaultMaxExpandedDepth, int maxEnumerableLength = DumpQuotas.DefaultMaxEnumerableLength, int maxStringLength = DumpQuotas.DefaultMaxStringLength, [CallerLineNumber] int? line = null)
        where TEnumerable : IEnumerable
    {
        Dump(enumerable?.Cast<object>().ElementAtOrDefault(index), header, maxDepth, maxExpandedDepth, maxEnumerableLength, maxStringLength, line);
        return enumerable!;
    }

    internal static event DumpDelegate? Dumped;

    internal delegate void DumpDelegate(in DumpData data);
}

internal record struct DumpData(object? Object, string? Header, int? Line, DumpQuotas Quotas);

internal record struct DumpQuotas(int MaxDepth, int MaxExpandedDepth, int MaxEnumerableLength, int MaxStringLength)
{
    internal const int DefaultMaxDepth = 4;
    internal const int DefaultMaxExpandedDepth = 1;
    internal const int DefaultMaxStringLength = 10000;
    internal const int DefaultMaxEnumerableLength = 10000;

    public static DumpQuotas Default { get; } = new DumpQuotas(DefaultMaxDepth, DefaultMaxExpandedDepth, DefaultMaxEnumerableLength, DefaultMaxStringLength);

    [Pure]
    internal DumpQuotas StepDown() => this with { MaxDepth = MaxDepth - 1, MaxExpandedDepth = MaxExpandedDepth - 1 };
}
