using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using RoslynPad.Annotations;

namespace RoslynPad.Runtime
{
    [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
    public static class ObjectExtensions
    {
        public static T Dump<T>(this T o, string? header = null, int maxDepth = DumpQuotas.DefaultMaxDepth, int maxExpandedDepth = DumpQuotas.DefaultMaxExpandedDepth, int maxEnumerableLength = DumpQuotas.DefaultMaxEnumerableLength, int maxStringLength = DumpQuotas.DefaultMaxStringLength)
        {
            Dumped?.Invoke(new DumpData(o, header, new DumpQuotas(maxDepth, maxExpandedDepth, maxEnumerableLength, maxStringLength)));
            return o;
        }

        public static T DumpAs<T, TResult>(this T o, Func<T, TResult>? selector, string? header = null, int maxDepth = DumpQuotas.DefaultMaxDepth, int maxExpandedDepth = DumpQuotas.DefaultMaxExpandedDepth, int maxEnumerableLength = DumpQuotas.DefaultMaxEnumerableLength, int maxStringLength = DumpQuotas.DefaultMaxStringLength)
        {
            Dump(selector != null ? (object?)selector.Invoke(o) : null, header, maxDepth, maxExpandedDepth, maxEnumerableLength, maxStringLength);
            return o;
        }

        public static TEnumerable DumpFirst<TEnumerable>(this TEnumerable enumerable, string? header = null, int maxDepth = DumpQuotas.DefaultMaxDepth, int maxExpandedDepth = DumpQuotas.DefaultMaxExpandedDepth, int maxEnumerableLength = DumpQuotas.DefaultMaxEnumerableLength, int maxStringLength = DumpQuotas.DefaultMaxStringLength)
            where TEnumerable : IEnumerable

        {
            Dump(enumerable?.Cast<object>().FirstOrDefault(), header, maxDepth, maxExpandedDepth, maxEnumerableLength, maxStringLength);
            return enumerable;
        }

        public static TEnumerable DumpLast<TEnumerable>(this TEnumerable enumerable, string? header = null, int maxDepth = DumpQuotas.DefaultMaxDepth, int maxExpandedDepth = DumpQuotas.DefaultMaxExpandedDepth, int maxEnumerableLength = DumpQuotas.DefaultMaxEnumerableLength, int maxStringLength = DumpQuotas.DefaultMaxStringLength)
            where TEnumerable : IEnumerable
        {
            Dump(enumerable?.Cast<object>().LastOrDefault(), header, maxDepth, maxExpandedDepth, maxEnumerableLength, maxStringLength);
            return enumerable;
        }

        public static TEnumerable DumpElementAt<TEnumerable>(this TEnumerable enumerable, int index, string? header = null, int maxDepth = DumpQuotas.DefaultMaxDepth, int maxExpandedDepth = DumpQuotas.DefaultMaxExpandedDepth, int maxEnumerableLength = DumpQuotas.DefaultMaxEnumerableLength, int maxStringLength = DumpQuotas.DefaultMaxStringLength)
            where TEnumerable : IEnumerable
        {
            Dump(enumerable?.Cast<object>().ElementAtOrDefault(index), header, maxDepth, maxExpandedDepth, maxEnumerableLength, maxStringLength);
            return enumerable;
        }

        internal static event Action<DumpData>? Dumped;
    }

    internal struct DumpData
    {
        public object? Object { get; }
        public string? Header { get; }
        public DumpQuotas Quotas { get; }

        public DumpData(object? o, string? header, DumpQuotas quotas)
        {
            Object = o;
            Header = header;
            Quotas = quotas;
        }
    }

    internal struct DumpQuotas
    {
        internal const int DefaultMaxDepth = 4;
        internal const int DefaultMaxExpandedDepth = 1;
        internal const int DefaultMaxStringLength = 10000;
        internal const int DefaultMaxEnumerableLength = 10000;

        public int MaxDepth { get; }
        public int MaxExpandedDepth { get; }
        public int MaxEnumerableLength { get; }
        public int MaxStringLength { get; }

        public DumpQuotas(int maxDepth, int maxExpandedDepth, int maxEnumerableLength, int maxStringLength)
        {
            MaxDepth = maxDepth;
            MaxExpandedDepth = maxExpandedDepth;
            MaxEnumerableLength = maxEnumerableLength;
            MaxStringLength = maxStringLength;
        }

        public static DumpQuotas Default { get; } = new DumpQuotas(DefaultMaxDepth, DefaultMaxExpandedDepth, DefaultMaxEnumerableLength, DefaultMaxStringLength);

        [Pure]
        internal DumpQuotas StepDown() => 
            new DumpQuotas(MaxDepth - 1, MaxExpandedDepth - 1, MaxEnumerableLength, MaxStringLength);

        [Pure]
        internal DumpQuotas WithMaxDepth(int maxDepth) => 
            new DumpQuotas(maxDepth, MaxExpandedDepth, MaxEnumerableLength, MaxStringLength);
    }
}
