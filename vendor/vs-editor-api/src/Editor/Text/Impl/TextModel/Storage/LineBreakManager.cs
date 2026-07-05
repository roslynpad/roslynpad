using System;
using System.Threading;
using Microsoft.VisualStudio.Text.Utilities;

namespace Microsoft.VisualStudio.Text.Implementation
{
    internal static class LineBreakManager
    {
        public readonly static ILineBreaks Empty = new ShortLineBreaksEditor(Array.Empty<ushort>());

        /// <summary>
        /// Create a line break editor using the pooled line break lists (which should have excess capacity).
        /// </summary>
        /// <remarks>
        /// <para>ILineBreakEditor.ReleasePooledLineBreaks() should be called on the returne editors once all line breaks have been added.</para>
        /// <para>Note that this method is thread-safe. If multiple PooledLineBreakEditor are created simultaneously on different threads then
        /// only one will use the pooled line breaks (and the others will get freshly allocated line breaks).</para>
        /// </remarks>
        public static IPooledLineBreaksEditor CreatePooledLineBreakEditor(int maxLength)
        {
            return (maxLength <= short.MaxValue)
                   ? (IPooledLineBreaksEditor)(new ShortLineBreaksEditor(LineBreakListManager<ushort>.AcquireLineBreaks(ShortLineBreaksEditor.ExpectedNumberOfLines)))
                   : (IPooledLineBreaksEditor)(new IntLineBreaksEditor(LineBreakListManager<int>.AcquireLineBreaks(IntLineBreaksEditor.ExpectedNumberOfLines)));
        }

        // Create a line break editor using an allocated list (which should be sized to hold all the expected line breaks without reallocations),
        public static ILineBreaksEditor CreateLineBreakEditor(int maxLength, int initialCapacity)
        {
            return (maxLength < short.MaxValue)
                   ? (ILineBreaksEditor)(new ShortLineBreaksEditor(new ushort[initialCapacity]))
                   : (ILineBreaksEditor)(new IntLineBreaksEditor(new int[initialCapacity]));
        }

        public static ILineBreaks CreateLineBreaks(string source)
        {
            IPooledLineBreaksEditor lineBreaks = null;

            int index = 0;
            while (index < source.Length)
            {
                int breakLength = TextUtilities.LengthOfLineBreak(source, index, source.Length);
                if (breakLength == 0)
                {
                    ++index;
                }
                else
                {
                    if (lineBreaks == null)
                        lineBreaks = LineBreakManager.CreatePooledLineBreakEditor(source.Length);

                    lineBreaks.Add(index, breakLength);
                    index += breakLength;
                }
            }

            if (lineBreaks != null)
            {
                lineBreaks.ReleasePooledLineBreaks();
                return lineBreaks;
            }

            return Empty;
        }

        internal abstract class LineBreakListManager<T> : IPooledLineBreaksEditor
        {
            internal static T[] _pooledLineBreaks = null;

            internal protected T[] LineBreaks;

            private int _length;

            public int Length => _length;

            public LineBreakListManager(T[] lineBreaks)
            {
                this.LineBreaks = lineBreaks;
            }

            protected void Add(T value)
            {
                if (_length == this.LineBreaks.Length)
                {
                    // Simulate a List.Add()
                    var newLineBreaks = new T[_length * 2];
                    Array.Copy(this.LineBreaks, newLineBreaks, _length);

                    this.LineBreaks = newLineBreaks;
                }

                this.LineBreaks[_length++] = value;
            }

            // In single threaded operations, we'll always be getting/reusing the same list of line breaks. We, however, need to handle
            // the case of a file being simultaneously read on multiple threads (at which point one thread will get the pooled list,
            // the other threads will allocate, and the largest list will end up back in the shared pool).
            internal static T[] AcquireLineBreaks(int size)
            {
                T[] buffer = Volatile.Read(ref _pooledLineBreaks);
                if (buffer != null && buffer.Length >= size)
                {
                    if (buffer == Interlocked.CompareExchange(ref _pooledLineBreaks, null, buffer))
                    {
                        return buffer;
                    }
                }

                return new T[size];
            }

            public void ReleasePooledLineBreaks()
            {
                if (this.LineBreaks.Length != _length)
                {
                    // We have excess capacity, so make an accurately sized copy of this.LineBreaks and return it to the pool.
                    T[] newLineBreaks;
                    if (_length > 0)
                    {
                        newLineBreaks = new T[_length];
                        Array.Copy(this.LineBreaks, newLineBreaks, _length);
                    }
                    else
                    {
                        newLineBreaks = Array.Empty<T>();
                    }

                    T[] buffer = Volatile.Read(ref _pooledLineBreaks);
                    if ((buffer == null) || (buffer.Length < this.LineBreaks.Length))
                    {
                        // We're done with this.LineBreaks and either there is nothing in the pool or
                        // this.LineBreaks are larger than the array in the pool so replace it with
                        // this.LineBreaks.
                        Interlocked.CompareExchange(ref _pooledLineBreaks, this.LineBreaks, buffer);
                    }

                    this.LineBreaks = newLineBreaks;
                }
            }

            public abstract void Add(int start, int length);
            public abstract int StartOfLineBreak(int index);
            public abstract int EndOfLineBreak(int index);
        }

        private class ShortLineBreaksEditor : LineBreakListManager<ushort>
        {
            public const int ExpectedNumberOfLines = 500;      // Guestimate on how many lines will be in a typical 16k block.

            private const ushort MaskForPosition = 0x7fff;
            private const ushort MaskForLength = 0x8000;

            public ShortLineBreaksEditor(ushort[] lineBreaks)
                : base(lineBreaks)
            { }

            public override int StartOfLineBreak(int index)
            {
                return (int)(this.LineBreaks[index] & MaskForPosition);
            }

            public override int EndOfLineBreak(int index)
            {
                int lineBreak = this.LineBreaks[index];
                return (lineBreak & MaskForPosition) +
                       (((lineBreak & MaskForLength) != 0) ? 2 : 1);
            }

            public override void Add(int start, int length)
            {
                if ((start < 0) || (start > short.MaxValue))
                    throw new ArgumentOutOfRangeException(nameof(start));
                if ((length < 1) || (length > 2))
                    throw new ArgumentOutOfRangeException(nameof(length));

                this.Add((length == 1) ? (ushort)start : (ushort)(start | MaskForLength));
            }
        }

        private class IntLineBreaksEditor : LineBreakListManager<int>
        {
            public const int ExpectedNumberOfLines = 32000;      // Guestimate on how many lines will be in a typical 1MB block.

            private const int MaskForPosition = int.MaxValue;       //0x7fffffff
            private const int MaskForLength = int.MinValue;         //0x80000000 in an int-friendly way

            public IntLineBreaksEditor(int[] lineBreaks)
                : base(lineBreaks)
            { }

            public override int StartOfLineBreak(int index)
            {
                return (int)(this.LineBreaks[index] & MaskForPosition);
            }

            public override int EndOfLineBreak(int index)
            {
                int lineBreak = this.LineBreaks[index];
                return (lineBreak & MaskForPosition) +
                       (((lineBreak & MaskForLength) != 0) ? 2 : 1);
            }

            public override void Add(int start, int length)
            {
                if (start < 0)
                    throw new ArgumentOutOfRangeException(nameof(start));
                if ((length < 1) || (length > 2))
                    throw new ArgumentOutOfRangeException(nameof(length));

                this.Add((length == 1) ? start : (start | MaskForLength));
            }
        }
    }
}
